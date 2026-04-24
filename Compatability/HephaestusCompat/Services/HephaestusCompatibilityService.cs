using System.Threading.Tasks.Dataflow;
using Hephaestus;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using Path = System.IO.Path;

namespace SharedWeaponBuilds.HephaestusCompat.Services;

[Injectable(InjectionType.Singleton)]
public sealed class HephaestusCompatibilityService(
    WeaponBuildService weaponBuildService,
    DatabaseService databaseService,
    RagfairPriceService ragfairPriceService,
    SaveServer saveServer,
    ModHelper modHelper,
    ICloner cloner,
    JsonUtil jsonUtil,
    ISptLogger<HephaestusCompatibilityService> logger
)
{
    //Cant reference this from Hephaestus for some reason because it breaks the server for whatever reason, cba to figure it out
    internal sealed record HephaestusConfig
    {
        public required string currency { get; set; }

        public required double discount { get; set; }
    }

    public MongoId TraderId = MongoId.Empty();
    internal HephaestusConfig _config = default!;

    public static TraderAssort NewAssort()
    {
        return new()
        {
            Items = [],
            BarterScheme = [],
            LoyalLevelItems = [],
            NextResupply = 0,
        };
    }

    // Quick copypasta from Hephaestus because I'm lazy
    public async Task BuildAssort()
    {
        try
        {
            // ensure mod assets can be resolved
            var pathToMod = modHelper.GetAbsolutePathToModFolder(typeof(Hephaestus.Hephaestus).Assembly);

            if (pathToMod is null)
            {
                logger.Error("getUserBuild: pathToMod is null");
                return;
            }

            var traderBase = await jsonUtil.DeserializeFromFileAsync<TraderBase>(Path.Combine(pathToMod, "db", "base.json"));
            // config.json is an object, not a raw string — deserialize to JsonElement (or a typed config class)
            _config =
                await jsonUtil.DeserializeFromFileAsync<HephaestusConfig>(Path.Combine(pathToMod, "config.json"))
                ?? throw new InvalidOperationException("Could not load HephaestusConfig");

            if (traderBase is null)
            {
                logger.Error("getUserBuild: traderBase is null");
                return;
            }

            TraderId = traderBase.Id;

            var traders = databaseService.GetTables().Traders;
            var assort = NewAssort();
            var currency = Money.EUROS;
            if (_config?.currency is not null)
            {
                currency = _config.currency.ToString();
            }
            var discount = _config?.discount > 0 ? _config.discount : 0;
            List<WeaponBuild> allBuilds = [];
            var presetFiles = Directory.GetFiles(
                Path.Combine(pathToMod, "presets"),
                "*.json",
                new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }
            );
            foreach (var file in presetFiles)
            {
                var presets = await jsonUtil.DeserializeFromFileAsync<List<WeaponBuild>>(file)!;

                foreach (var preset in presets ?? [])
                {
                    if (weaponBuildService.WeaponBuilds.ContainsKey(preset.Id))
                    {
                        continue;
                    }

                    allBuilds.Add(preset);
                }
            }

            foreach (var (sessionId, profile) in saveServer.GetProfiles())
            {
                if (profile.UserBuildData is null || profile.UserBuildData.WeaponBuilds is null)
                {
                    continue;
                }

                foreach (var wb in profile.UserBuildData.WeaponBuilds)
                {
                    if (weaponBuildService.WeaponBuilds.ContainsKey(wb.Id))
                    {
                        continue;
                    }

                    allBuilds.Add(wb);
                }
            }

            foreach (var sharedBuild in weaponBuildService.WeaponBuilds.Values)
            {
                allBuilds.Add(sharedBuild);
            }

            logger.Success($"Hephaestus: Generated {allBuilds.Count} builds");
            foreach (var wb in allBuilds)
            {
                if (wb?.Items?[0]?.Id.IsValidMongoId() != true)
                {
                    continue;
                }
                var preItems = wb.Items;
                if (wb.Root is not null)
                {
                    var pi = cloner.Clone(wb.Items)!;
                    var id = pi[0].Id;
                    var tpl = pi[0].Template;
                    pi[0] = new()
                    {
                        Id = id,
                        Template = tpl,
                        ParentId = "hideout",
                        SlotId = "hideout",
                        Upd = new() { UnlimitedCount = true, StackObjectsCount = 999999 },
                    };
                    pi[0].AddToExtensionData("BuildId", wb.Id);
                    assort.Items.AddRange(pi);
                    var priceOfOfferItem = ragfairPriceService.GetDynamicOfferPriceForOffer(pi, currency, false);

                    if (discount > 0)
                    {
                        priceOfOfferItem = priceOfOfferItem - (priceOfOfferItem * (discount / 100));
                    }

                    var barter = new BarterScheme() { Count = priceOfOfferItem, Template = currency };

                    assort.BarterScheme.Add(
                        pi[0].Id,
                        [
                            [barter],
                        ]
                    );
                    assort.LoyalLevelItems.Add(pi[0].Id, 1);
                }
            }
            traders[TraderId].Assort = assort;
        }
        catch (Exception ex)
        {
            // Keep this simple — replace with server logger if desired.
            logger.Error($"getUserBuild exception: {ex}");
        }
    }

    public void AddBuild(WeaponBuild wb)
    {
        if (wb?.Items?[0]?.Id.IsValidMongoId() != true)
        {
            return;
        }

        RemoveBuild(wb.Id);

        var traders = databaseService.GetTables().Traders;
        var assort = traders[TraderId].Assort;

        if (wb.Root is not null)
        {
            var pi = cloner.Clone(wb.Items)!;
            var id = pi[0].Id;
            var tpl = pi[0].Template;

            pi[0] = new()
            {
                Id = id,
                Template = tpl,
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new() { UnlimitedCount = true, StackObjectsCount = 999999 },
            };

            pi[0].AddToExtensionData("BuildId", wb.Id);

            assort.Items.AddRange(pi);

            var priceOfOfferItem = ragfairPriceService.GetDynamicOfferPriceForOffer(pi, _config.currency, false);

            if (_config.discount > 0)
            {
                priceOfOfferItem = priceOfOfferItem - (priceOfOfferItem * (_config.discount / 100));
            }

            var barter = new BarterScheme() { Count = priceOfOfferItem, Template = _config.currency };

            assort.BarterScheme.Add(
                pi[0].Id,
                [
                    [barter],
                ]
            );

            assort.LoyalLevelItems.Add(pi[0].Id, 1);
        }
    }

    public void RemoveBuild(MongoId buildId)
    {
        var traders = databaseService.GetTables().Traders;
        var assort = traders[TraderId].Assort;

        var buildIdText = buildId.ToString();

        var childrenByParentId = new Dictionary<string, List<MongoId>>(StringComparer.Ordinal);

        foreach (var item in assort.Items)
        {
            if (item is null)
            {
                continue;
            }

            if (item.ParentId is null)
            {
                continue;
            }

            var parentId = item.ParentId.ToString();

            if (!childrenByParentId.TryGetValue(parentId, out var childIds))
            {
                childIds = [];
                childrenByParentId[parentId] = childIds;
            }

            childIds.Add(item.Id);
        }

        var itemIdsToRemove = assort.Items.Where(item => HasBuildId(item, buildIdText)).Select(item => item.Id).ToHashSet();

        if (itemIdsToRemove.Count == 0)
        {
            return;
        }

        var queue = new Queue<string>(itemIdsToRemove.Select(itemId => itemId.ToString()));

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();

            if (!childrenByParentId.TryGetValue(parentId, out var childIds))
            {
                continue;
            }

            foreach (var childId in childIds)
            {
                if (itemIdsToRemove.Add(childId))
                {
                    queue.Enqueue(childId.ToString());
                }
            }
        }

        assort.Items.RemoveAll(item =>
        {
            if (item is null)
            {
                return false;
            }

            return itemIdsToRemove.Contains(item.Id);
        });

        foreach (var itemId in itemIdsToRemove)
        {
            assort.BarterScheme.Remove(itemId);
            assort.LoyalLevelItems.Remove(itemId);
        }
    }

    private static bool HasBuildId(Item item, string buildIdText)
    {
        if (item is null)
        {
            return false;
        }

        if (item.ExtensionData is null)
        {
            return false;
        }

        if (!item.ExtensionData.TryGetValue("BuildId", out var value))
        {
            return false;
        }

        if (value is null)
        {
            return false;
        }

        return string.Equals(value.ToString(), buildIdText, StringComparison.Ordinal);
    }
}

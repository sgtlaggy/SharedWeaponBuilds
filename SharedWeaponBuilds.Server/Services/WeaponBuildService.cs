using System.Collections.Concurrent;
using System.Reflection;
using SharedWeaponBuilds.Server.WebSockets;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class WeaponBuildService(
    ModHelper modHelper,
    JsonUtil jsonUtil,
    WeaponBuildsWebSocket weaponBuildsWebSocket,
    ISptLogger<WeaponBuildService> logger
)
{
    /// <summary>
    /// Concurrent Dictionary keyed to build id, and then the build
    /// </summary>
    public ConcurrentDictionary<MongoId, WeaponBuild> WeaponBuilds { get; private set; } = [];

    public string ModPath { get; init; } = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    public string BuildsPath
    {
        get { return Path.Combine(ModPath, "WeaponBuilds"); }
    }

    public bool TryGetBuild(MongoId buildId, out WeaponBuild? weaponBuild)
    {
        if (!WeaponBuilds.TryGetValue(buildId, out var build))
        {
            weaponBuild = null;
            return false;
        }

        weaponBuild = build;
        return true;
    }

    public async Task LoadWeaponBuilds()
    {
        if (!Directory.Exists(BuildsPath))
        {
            Directory.CreateDirectory(BuildsPath);
        }

        WeaponBuilds.Clear();

        string[] files = Directory.GetFiles(BuildsPath, "*.json", SearchOption.TopDirectoryOnly);
        int loadedAmount = 0;

        foreach (string file in files)
        {
            try
            {
                WeaponBuild? build = await jsonUtil.DeserializeFromFileAsync<WeaponBuild>(file);

                if (build is null)
                {
                    logger.Error($"[Shared Weapon Builds] Failed to load weapon build from '{file}'");
                    continue;
                }

                if (!WeaponBuilds.TryAdd(build.Id, build))
                {
                    logger.Warning(
                        $"[Shared Weapon Builds] Failed to add weapon build from '{file}' because build id '{build.Id}' already exists in memory."
                    );
                    continue;
                }

                loadedAmount++;
            }
            catch (Exception ex)
            {
                logger.Error($"[Shared Weapon Builds] Failed to load weapon build from '{file}'", ex);
            }
        }

        if (loadedAmount > 0)
        {
            string buildLabel = loadedAmount == 1 ? "weapon build" : "weapon builds";
            logger.Success($"[Shared Weapon Builds] Loaded {loadedAmount} {buildLabel}");
        }
    }

    public List<WeaponBuild> GetWeaponBuilds()
    {
        return WeaponBuilds.Values.ToList();
    }

    public async Task SaveWeaponBuild(MongoId sessionId, PresetBuildActionRequestData request)
    {
        if (!Directory.Exists(BuildsPath))
        {
            Directory.CreateDirectory(BuildsPath);
        }

        if (request.Items is null)
        {
            logger.Error("[Shared Weapon Builds] Items in build request is null?");
            return;
        }

        WeaponBuild weaponBuild = new()
        {
            Id = request.Id,
            Name = request.Name,
            Root = request.Root,
            Items = request.Items.ToList(),
        };

        WeaponBuilds.AddOrUpdate(
            weaponBuild.Id,
            weaponBuild,
            (_, _) =>
            {
                return weaponBuild;
            }
        );

        string filePath = GetBuildFilePath(weaponBuild.Id);
        await File.WriteAllTextAsync(filePath, jsonUtil.Serialize(weaponBuild));

        await weaponBuildsWebSocket.BroadcastAsync(
            sessionId,
            new Models.UpdatedWeaponMessage
            {
                BuildId = weaponBuild.Id,
                UpdatedWeaponBuild = weaponBuild,
                IsDeleted = false,
            }
        );
    }

    public async Task RemoveWeaponBuild(MongoId sessionId, MongoId buildId)
    {
        WeaponBuilds.TryRemove(buildId, out _);

        string filePath = GetBuildFilePath(buildId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await weaponBuildsWebSocket.BroadcastAsync(sessionId, new Models.UpdatedWeaponMessage { BuildId = buildId, IsDeleted = true });
    }

    private string GetBuildFilePath(MongoId buildId)
    {
        return Path.Combine(BuildsPath, $"{buildId}.json");
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SharedWeaponBuilds.Server.WebSockets;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class WeaponBuildService(
    DatabaseService databaseService,
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
    private ConcurrentDictionary<MongoId, string> _fileMap { get; set; } = [];

    public string ModPath { get; init; } = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    public string BuildsPath
    {
        get { return Path.Combine(ModPath, "WeaponBuilds"); }
    }

    public List<WeaponBuild> GetWeaponBuilds()
    {
        return WeaponBuilds.Values.ToList();
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

        var itemDatabase = databaseService.GetTables().Templates.Items;

        WeaponBuilds.Clear();

        var files = Directory.GetFiles(BuildsPath, "*.json", SearchOption.TopDirectoryOnly);
        var loadedAmount = 0;

        foreach (var file in files)
        {
            try
            {
                var build = await jsonUtil.DeserializeFromFileAsync<WeaponBuild>(file);

                if (build is null || build.Items is null)
                {
                    logger.Error($"[Shared Weapon Builds] Failed to load weapon build from '{file}'");
                    continue;
                }

                var missingTemplatesCount = 0;

                foreach (var item in build.Items)
                {
                    if (!itemDatabase.ContainsKey(item.Template))
                    {
                        missingTemplatesCount++;
                    }
                }

                if (missingTemplatesCount > 0)
                {
                    var templateLabel = missingTemplatesCount == 1 ? "template" : "templates";

                    logger.Error(
                        $"[Shared Weapon Builds] Failed to load weapon build from '{file}' due to {missingTemplatesCount} missing {templateLabel}"
                    );
                    continue;
                }

                if (!_fileMap.TryAdd(build.Id, file))
                {
                    logger.Warning($"[Shared Weapon Builds] Failed to map {build.Id} to {file}");
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
            var buildLabel = loadedAmount == 1 ? "weapon build" : "weapon builds";
            logger.Success($"[Shared Weapon Builds] Loaded {loadedAmount} {buildLabel}");
        }
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

        if (_fileMap.TryGetValue(weaponBuild.Id, out var path))
        {
            await File.WriteAllTextAsync(path, jsonUtil.Serialize(weaponBuild));
        }
        else
        {
            var filePath = GetBuildFilePath(weaponBuild.Id, weaponBuild.Name!);

            if (!_fileMap.TryAdd(weaponBuild.Id, filePath))
            {
                logger.Warning($"[Shared Weapon Builds] Failed to map {weaponBuild.Id} to {filePath}");
                return;
            }

            await File.WriteAllTextAsync(filePath, jsonUtil.Serialize(weaponBuild));
        }

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

        if (_fileMap.TryGetValue(buildId, out var path))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _fileMap.TryRemove(buildId, out _);
        }

        await weaponBuildsWebSocket.BroadcastAsync(sessionId, new Models.UpdatedWeaponMessage { BuildId = buildId, IsDeleted = true });
    }

    private string GetBuildFilePath(MongoId buildId, string buildName)
    {
        var safeName = SanitizeFileName(buildName);

        return Path.Combine(BuildsPath, $"-{safeName}-{buildId}.json");
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "build";
        }

        var sanitized = value.Trim().Normalize(NormalizationForm.FormKC);

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '-');
        }

        sanitized = Regex.Replace(sanitized, @"\s+", " ");
        sanitized = sanitized.Trim(' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "build";
        }

        return sanitized;
    }
}

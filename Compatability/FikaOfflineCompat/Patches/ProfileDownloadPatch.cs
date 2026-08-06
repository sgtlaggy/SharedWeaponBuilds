using System.Reflection;
using System.Text.Json;
using FikaServer.Controllers;
using FikaServer.Models.Fika.Routes.Client;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;


namespace SharedWeaponBuilds.FikaOfflineCompat.Patches;

public class ProfileDownloadPatch : AbstractPatch
{
    protected static WeaponBuildService _weaponBuildService = ServiceLocator.ServiceProvider.GetService<WeaponBuildService>()!;
    protected static SaveServer _saveServer = ServiceLocator.ServiceProvider.GetService<SaveServer>()!;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientController).GetMethod(nameof(ClientController.HandleProfileDownload))!;
    }

    [PatchPostfix]
    protected static void AddBuildsToProfileData(MongoId sessionId, ref DownloadProfileResponse? __result)
    {
        if (__result is null)
        {
            return;
        }

        var playerBuildIds = _saveServer.GetProfile(sessionId)?.UserBuildData?.WeaponBuilds?.Select(build => build.Id).ToHashSet();
        var otherPlayerBuilds = _weaponBuildService.GetWeaponBuilds();
        if (playerBuildIds?.Count > 0)
        {
            otherPlayerBuilds = otherPlayerBuilds.Where(build => !(playerBuildIds).Contains(build.Id)).ToList();
        }

        __result.ModData ??= [];
        __result.ModData[Constants.ProfileDataKey] = JsonSerializer.Serialize(otherPlayerBuilds, Constants._jsonOptions);
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SharedWeaponBuilds.Server.WebSockets;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;


namespace SharedWeaponBuilds.FikaOfflineCompat.Patches;

public class ProfileUploadPatch : AbstractPatch
{
    protected static WeaponBuildService _weaponBuildService = ServiceLocator.ServiceProvider.GetService<WeaponBuildService>()!;
    protected static WeaponBuildsWebSocket _weaponBuildsWebSocket = ServiceLocator.ServiceProvider.GetService<WeaponBuildsWebSocket>()!;

    protected override MethodBase GetTargetMethod()
    {
        // Alternative to ‘FikaServer.API.UploadProfilesController.HandlePostRequest’
        // Using that, maybe ‘Request.Body’ would be used but that uses Stream and
        // may not be able to be read twice.
        return typeof(SaveServer).GetMethod(nameof(SaveServer.AddProfile))!;
    }

    [PatchPostfix]
    protected static void AddPlayerBuilds(SptProfile profileDetails)
    {
        if (profileDetails.UserBuildData?.WeaponBuilds is null)
        {
            return;
        }

        // Add all builds from profiles added/uploaded at runtime.
        foreach (var build in profileDetails.UserBuildData.WeaponBuilds)
        {
            // async method, just gonna fire and forget
            var task = _weaponBuildsWebSocket.BroadcastAsync(default, new() { BuildId = build.Id, UpdatedWeaponBuild = build, IsDeleted = false });
        }
    }
}

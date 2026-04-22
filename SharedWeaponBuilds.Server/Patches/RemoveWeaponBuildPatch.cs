using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.Patches;

public sealed class RemoveWeaponBuildPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BuildsCallbacks).GetMethod(nameof(BuildsCallbacks.DeleteBuild))
            ?? throw new InvalidOperationException("Could not BuildsCallbacks.DeleteBuild");
    }

    [PatchPrefix]
    public static bool Prefix(RemoveBuildRequestData request, MongoId sessionID, ref ValueTask<string> __result)
    {
        __result = ModifyResult(sessionID, request);
        return false;
    }

    public static async ValueTask<string> ModifyResult(MongoId sessionId, RemoveBuildRequestData request)
    {
        var httpResponseUtil =
            ServiceLocator.ServiceProvider.GetService<HttpResponseUtil>()
            ?? throw new NullReferenceException("Could not get HttpResponseUtil");

        var buildController =
            ServiceLocator.ServiceProvider.GetService<BuildController>()
            ?? throw new NullReferenceException("Could not get BuildController");

        var weaponBuildService =
            ServiceLocator.ServiceProvider.GetService<WeaponBuildService>()
            ?? throw new NullReferenceException("Could not get WeaponBuildService");

        var saveServer =
            ServiceLocator.ServiceProvider.GetService<SaveServer>() ?? throw new NullReferenceException("Could not get SaveServer");

        await weaponBuildService.RemoveWeaponBuild(sessionId, request.Id);

        var profile = saveServer.GetProfile(sessionId);

        if (profile?.UserBuildData?.WeaponBuilds is null)
        {
            return httpResponseUtil.NullResponse();
        }

        var weaponBuild = profile.UserBuildData.WeaponBuilds.FirstOrDefault(weaponBuild => weaponBuild.Id == request.Id);

        // Remove weapon build out of original profile if it is saved there
        if (weaponBuild is not null)
        {
            buildController.RemoveBuild(sessionId, request);
        }

        return httpResponseUtil.NullResponse();
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.HephaestusCompat.Services;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;

namespace SharedWeaponBuilds.HephaestusCompat.Patches;

public sealed class HephaestusRemoveBuildPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(WeaponBuildService).GetMethod(nameof(WeaponBuildService.RemoveWeaponBuild))
            ?? throw new InvalidOperationException("Could not find WeaponBuildService.RemoveWeaponBuild");
    }

    [PatchPostfix]
    public static async Task Postfix(Task __result, MongoId buildId)
    {
        var hephaestusCompatibilityService =
            ServiceLocator.ServiceProvider.GetService<HephaestusCompatibilityService>()
            ?? throw new NullReferenceException("Could not get HephaestusCompatibilityService");

        await __result;
        hephaestusCompatibilityService.RemoveBuild(buildId);
    }
}

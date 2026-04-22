using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace SharedWeaponBuilds.Server.Patches;

public sealed class LoadSharedWeaponBuildsPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BuildController).GetMethod(nameof(BuildController.GetUserBuilds))
            ?? throw new InvalidOperationException("Could not BuildController.GetUserBuilds");
    }

    [PatchPostfix]
    public static void Postfix(ref UserBuilds __result)
    {
        var weaponBuildService =
            ServiceLocator.ServiceProvider.GetService<WeaponBuildService>()
            ?? throw new NullReferenceException("Could not get WeaponBuildService");

        if (__result.WeaponBuilds is null)
        {
            return;
        }

        List<WeaponBuild> weaponBuilds = weaponBuildService.GetWeaponBuilds();

        foreach (var sharedBuild in weaponBuilds)
        {
            int existingIndex = -1;

            for (int i = 0; i < __result.WeaponBuilds.Count; i++)
            {
                if (__result.WeaponBuilds[i].Id == sharedBuild.Id)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                __result.WeaponBuilds[existingIndex] = sharedBuild;
            }
            else
            {
                __result.WeaponBuilds.Add(sharedBuild);
            }
        }
    }
}

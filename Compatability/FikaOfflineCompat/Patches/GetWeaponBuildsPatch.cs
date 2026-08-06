using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;


namespace SharedWeaponBuilds.FikaOfflineCompat.Patches;

public class GetWeaponBuildsPatch : AbstractPatch
{
    protected static ProfileDataService _profileDataService = ServiceLocator.ServiceProvider.GetService<ProfileDataService>()!;
    protected static SaveServer _saveServer = ServiceLocator.ServiceProvider.GetService<SaveServer>()!;

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(WeaponBuildService).GetMethod(nameof(WeaponBuildService.GetWeaponBuilds));
    }

    [PatchPostfix]
    protected static void AddBuildsFromProfileData(ref List<WeaponBuild> __result)
    {
        var buildIds = __result.Select(build => build.Id).ToHashSet();

        foreach (var profile in _saveServer.GetProfiles().Values)
        {
            if (profile?.ProfileInfo?.ProfileId is null)
            {
                continue;
            }

            // Add/overwrite from profile data on offline servers.
            var profileData = _profileDataService.GetProfileData<List<WeaponBuild>>(profile.ProfileInfo.ProfileId, Constants.ProfileDataKey);

            if (profileData is null)
            {
                continue;
            }

            foreach (var build in profileData)
            {
                if (buildIds.Add(build.Id))
                {
                    __result.Add(build);
                }
            }
        }
    }
}

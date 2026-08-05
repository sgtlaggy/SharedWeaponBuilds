using SharedWeaponBuilds.Server;
using SharedWeaponBuilds.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;

namespace SharedWeaponBuilds.FikaOfflineCompat;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset)]
public class ProfileBuildLoader(
    ProfileDataService _profileDataService,
    SaveServer _saveServer,
    WeaponBuildService _weaponBuildService
) : IOnLoad
{
    public Task OnLoad()
    {
        foreach (var profile in _saveServer.GetProfiles().Values)
        {
            if (profile?.ProfileInfo?.ProfileId is null)
            {
                continue;
            }

            // Add/overwrite from profile data on offline servers.
            var profileData = _profileDataService.GetProfileData<Dictionary<MongoId, WeaponBuild>>(profile.ProfileInfo.ProfileId, Constants.ProfileDataKey);
            if (profileData is not null)
            {
                foreach (var (buildId, build) in profileData)
                {
                    _weaponBuildService.WeaponBuilds[buildId] = build;
                }
            }

            if (profile?.UserBuildData?.WeaponBuilds is null)
            {
                continue;
            }

            // Add/overwrite with builds from profile in case profile was
            // added manually before running the server.
            foreach (var build in profile.UserBuildData.WeaponBuilds)
            {
                _weaponBuildService.WeaponBuilds[build.Id] = build;
            }
        }

        return Task.CompletedTask;
    }
}

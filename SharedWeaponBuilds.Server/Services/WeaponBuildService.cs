using SharedWeaponBuilds.Server.WebSockets;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;

namespace SharedWeaponBuilds.Server.Services;

[Injectable(InjectionType.Singleton)]
public sealed class WeaponBuildService(
    ProfileHelper profileHelper,
    WeaponBuildsWebSocket weaponBuildsWebSocket,
    ISptLogger<WeaponBuildService> logger
)
{
    public List<WeaponBuild> GetWeaponBuilds()
    {
        List<WeaponBuild> builds = [];
        foreach (var profile in profileHelper.GetProfiles().Values)
        {
            builds.AddRange(profile.UserBuildData?.WeaponBuilds ?? []);
        }
        return builds;
    }

    public async Task SaveWeaponBuild(MongoId sessionId, PresetBuildActionRequestData request)
    {
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
        await weaponBuildsWebSocket.BroadcastAsync(sessionId, new Models.UpdatedWeaponMessage { BuildId = buildId, IsDeleted = true });
    }
}

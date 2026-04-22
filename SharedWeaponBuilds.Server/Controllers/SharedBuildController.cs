using SharedWeaponBuilds.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;

namespace SharedWeaponBuilds.Server.Controllers;

[Injectable]
public sealed class SharedBuildController(WeaponBuildService weaponBuildService)
{
    public async Task SaveWeaponBuild(MongoId sessionId, PresetBuildActionRequestData data)
    {
        await weaponBuildService.SaveWeaponBuild(sessionId, data);
    }
}

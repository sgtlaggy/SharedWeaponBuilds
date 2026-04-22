using SharedWeaponBuilds.Server.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace SharedWeaponBuilds.Server.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset)]
public sealed class PostDBLoad(WeaponBuildService weaponBuildService) : IOnLoad
{
    public async Task OnLoad()
    {
        await weaponBuildService.LoadWeaponBuilds();
    }
}

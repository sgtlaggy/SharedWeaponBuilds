using SharedWeaponBuilds.Server.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace SharedWeaponBuilds.Server.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset)]
public sealed class PreSPTLoad(ISptLogger<PreSPTLoad> logger) : IOnLoad
{
    private bool _overridesInjected = false;
    private readonly List<AbstractPatch> _patches = [new LoadSharedWeaponBuildsPatch(), new RemoveWeaponBuildPatch()];

    private void InjectOverrides()
    {
        if (_overridesInjected)
        {
            return;
        }

        try
        {
            foreach (AbstractPatch patch in _patches)
            {
                logger.Debug($"[Shared Weapon Builds] Loading patch: {patch.GetType().Name}");
                patch.Enable();
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[Shared Weapon Builds] Error applying patch: {ex}");
            throw;
        }

        _overridesInjected = true;
    }

    public async Task OnLoad()
    {
        InjectOverrides();
    }
}

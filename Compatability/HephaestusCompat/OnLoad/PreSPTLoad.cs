using Hephaestus;
using SharedWeaponBuilds.HephaestusCompat.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace SharedWeaponBuilds.HephaestusCompat.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 2)]
public sealed class PreSPTLoad(ISptLogger<PreSPTLoad> logger) : IOnLoad
{
    private bool _overridesInjected = false;
    private readonly List<AbstractPatch> _patches = [new DisableOriginalGenerateAssortPatch(), new HephaestusRemoveBuildPatch()];

    private void InjectOverrides()
    {
        if (_overridesInjected)
        {
            return;
        }

        // Disable all Hephaestus Patches
        new GetAssortPatchWeaponBuild().Disable();
        new GetAssortPatchWeaponDelete().Disable();
        new GetAssortPatchUserLogin().Disable();

        try
        {
            foreach (var patch in _patches)
            {
                logger.Debug($"[Shared Weapon Builds - Hephaestus Compatibility] Loading patch: {patch.GetType().Name}");
                patch.Enable();
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[Shared Weapon Builds - Hephaestus Compatibility] Error applying patch: {ex}");
            throw;
        }

        _overridesInjected = true;
    }

    public async Task OnLoad()
    {
        InjectOverrides();
    }
}

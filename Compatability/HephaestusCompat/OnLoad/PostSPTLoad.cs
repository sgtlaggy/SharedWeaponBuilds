using Hephaestus;
using SharedWeaponBuilds.HephaestusCompat.Patches;
using SharedWeaponBuilds.HephaestusCompat.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace SharedWeaponBuilds.HephaestusCompat.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 2)]
public sealed class PostSPTLoad(HephaestusCompatibilityService hephaestusCompatibilityService) : IOnLoad
{
    public async Task OnLoad()
    {
        await hephaestusCompatibilityService.BuildAssort();
    }
}

using System.Reflection;
using Hephaestus;
using SPTarkov.Reflection.Patching;

namespace SharedWeaponBuilds.HephaestusCompat.Patches;

public sealed class DisableOriginalGenerateAssortPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GenerateAssort).GetMethod(nameof(GenerateAssort.buildAssort))
            ?? throw new InvalidOperationException("Could not find GenerateAssort");
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return false;
    }
}

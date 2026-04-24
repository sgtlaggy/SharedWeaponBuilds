using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SharedWeaponBuilds.Client.Patches;

public sealed class SharedWeaponBuildsWSInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Patches the initialisation for weapon builds
        return AccessTools.Method(typeof(TarkovApplication), nameof(TarkovApplication.method_26));
    }

    [PatchPostfix]
    public static async Task Postfix(Task __result)
    {
        // Allow the initial request to go through first, in async this happens in Postfix
        await __result;

        if (SharedWeaponBuildsPlugin.Instance.WeaponBuildWebSocket == null)
        {
            SharedWeaponBuildsPlugin.Instance.WeaponBuildWebSocket = new();
        }
    }
}

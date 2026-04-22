using BepInEx;
using SPT.Reflection.Patching;

namespace SharedWeaponBuilds.Client;

[BepInPlugin("wtf.archangel.sharedweaponbuilds", "Shared Weapon Builds", "1.0.0")]
public sealed class SharedWeaponBuildsPlugin : BaseUnityPlugin
{
    public static SharedWeaponBuildsPlugin Instance { get; private set; }
    public SharedWeaponBuildsWSManager WeaponBuildWebSocket { get; set; }
    private PatchManager _patchManager;

    public void Awake()
    {
        Instance = this;

        _patchManager = new(this, true);
        _patchManager.EnablePatches();
    }
}

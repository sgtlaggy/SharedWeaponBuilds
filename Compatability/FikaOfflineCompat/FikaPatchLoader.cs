using SharedWeaponBuilds.FikaOfflineCompat.Patches;
using SharedWeaponBuilds.Server;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;


namespace SharedWeaponBuilds.FikaOfflineCompat;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset)]
public class FikaPatchLoader : IOnLoad
{
    public Task OnLoad()
    {
        new ProfileDownloadPatch().Enable();
        new ProfileUploadPatch().Enable();
        return Task.CompletedTask;
    }
}

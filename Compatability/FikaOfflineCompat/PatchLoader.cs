using SharedWeaponBuilds.FikaOfflineCompat.Patches;
using SharedWeaponBuilds.Server;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;


namespace SharedWeaponBuilds.FikaOfflineCompat;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset)]
public class PatchLoader : IOnLoad
{
    public Task OnLoad()
    {
        new GetWeaponBuildsPatch().Enable();
        new ProfileDownloadPatch().Enable();
        new ProfileUploadPatch().Enable();
        return Task.CompletedTask;
    }
}

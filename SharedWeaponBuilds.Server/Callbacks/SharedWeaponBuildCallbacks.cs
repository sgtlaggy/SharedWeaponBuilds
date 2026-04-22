using SharedWeaponBuilds.Server.Controllers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.Callbacks;

[Injectable]
public sealed class SharedWeaponBuildCallbacks(SharedBuildController sharedBuildController, HttpResponseUtil httpResponseUtil)
{
    /// <summary>
    ///     Handle client/builds/weapon/save
    /// </summary>
    /// <returns></returns>
    public async ValueTask<string> SetWeapon(string url, PresetBuildActionRequestData request, MongoId sessionID)
    {
        await sharedBuildController.SaveWeaponBuild(sessionID, request);
        return httpResponseUtil.NullResponse();
    }
}

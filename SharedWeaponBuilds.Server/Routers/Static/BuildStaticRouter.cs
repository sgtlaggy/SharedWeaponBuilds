using SharedWeaponBuilds.Server.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.PresetBuild;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.Routers.Static;

//Todo in 4.1 TypePriority should be OnLoadOrder.Routers + SharedWeaponBuildsModMetadata.SharedWeaponBuildsPriorityOffset
[Injectable]
public class BuildStaticRouter(JsonUtil jsonUtil, SharedWeaponBuildCallbacks sharedWeaponBuildCallbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<PresetBuildActionRequestData>(
                "/client/builds/weapon/save",
                async (url, info, sessionID, output) => await sharedWeaponBuildCallbacks.SetWeapon(url, info, sessionID)
            ),
        ]
    ) { }

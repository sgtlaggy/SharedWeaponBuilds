using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.HandBook;
using EFT.UI;
using Newtonsoft.Json;
using SharedWeaponBuilds.Client.Models;
using SharedWeaponBuilds.Client.Patches;
using SPT.Common.Http;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Logger = BepInEx.Logging.Logger;

namespace SharedWeaponBuilds.Client;

/// <summary>
/// Most of this code I mostly copied from Fika's websocket implementation, credits to the Fika team.. Which is also myself I guess so xdx
/// </summary>
public sealed class SharedWeaponBuildsWSManager
{
    private static readonly ManualLogSource _logger = Logger.CreateLogSource("SharedWeaponBuildsWSManager");
    private static HandbookClass _handbookClass;
    private static CurrentScreenSingletonClass _currentScreenSingletonClass;
    public static ISession Session;
    public static ItemFactoryClass ItemFactoryClass;
    public static NotifierView notifierView;
    public static SharedWeaponBuildsWSManager Instance;
    public static bool Exists
    {
        get { return Instance != null; }
    }
    public string Host { get; set; }
    public string Url { get; set; }
    public string SessionId { get; set; }
    public bool Connected
    {
        get { return _webSocket.ReadyState == WebSocketState.Open; }
    }

    public bool reconnecting;

    private readonly WebSocket _webSocket;

    public SharedWeaponBuildsWSManager()
    {
        Instance = this;
        Host = RequestHandler.Host.Replace("http", "ws");
        SessionId = RequestHandler.SessionId;
        Url = $"{Host}/sharedweaponbuilds/websocket/";

        _webSocket = new WebSocket(Url) { WaitTime = TimeSpan.FromMinutes(1), EmitOnPing = true };

        _webSocket.SetCredentials(SessionId, "", true);

        _webSocket.OnError += WebSocket_OnError;
        _webSocket.OnMessage += WebSocket_OnMessage;
        _webSocket.OnClose += (sender, e) =>
        {
            if (reconnecting)
            {
                return;
            }

            Task.Run(ReconnectWebSocket);
        };

        ItemFactoryClass = Singleton<ItemFactoryClass>.Instance;
        Session = Singleton<ClientApplication<ISession>>.Instance.Session;
        notifierView = Singleton<PreloaderUI>.Instance.NotifierView;
        _handbookClass = Singleton<HandbookClass>.Instance;
        _currentScreenSingletonClass = CurrentScreenSingletonClass.Instance;

        Connect();
    }

    private void WebSocket_OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogInfo($"WS error: {e.Message}");
    }

    public void Connect()
    {
        _webSocket.Connect();
    }

    public void Close()
    {
        _webSocket.Close();
    }

    private void WebSocket_OnMessage(object sender, MessageEventArgs e)
    {
        if (e == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(e.Data))
        {
            return;
        }

        var weaponMessage = JsonConvert.DeserializeObject<UpdatedWeaponMessage>(e.Data);

        var buildId = weaponMessage.BuildId;
        var updatedBuild = weaponMessage.UpdatedWeaponBuild;

        // If we already have the weapon, remove it out of the handbook's caches so we can re-create it
        if (ItemFactoryClass.WeaponBuildsStorageClass.Dictionary_0.ContainsKey(buildId))
        {
            var entityNodeClass = _handbookClass.WeaponNodesWithParent[buildId];
            var entityNodeClass2 = _handbookClass.WeaponNodesWithParent[entityNodeClass.Data.ParentId];

            entityNodeClass2.RemoveChildren(entityNodeClass);
            ItemFactoryClass.RemoveCachedItemName(ItemFactoryClass.WeaponBuildsStorageClass.Dictionary_0[buildId].ItemIconName);
            _handbookClass.WeaponNodesWithParent.RemoveVirtual(_handbookClass.WeaponNodesWithParent[buildId].Data.Id);
        }

        // If the weapon is deleted, delete it out of the session & WeaponBuildsStorageClass and return early.
        if (weaponMessage.IsDeleted)
        {
            ItemFactoryClass.WeaponBuildsStorageClass.Dictionary_0.Remove(buildId);
            Session.WeaponBuildsStorage.Dictionary_0.Remove(buildId);
            return;
        }

        // Begin reconstructing updated build + handbook
        var newNode = CreateHandbookEntityNode(updatedBuild);

        _handbookClass.WeaponNodesWithParent.AddVirtual(newNode.Data.Id, newNode);

        var handbookClassNode = _handbookClass.WeaponNodesWithParent[newNode.Data.ParentId];

        if (handbookClassNode == null)
        {
            return;
        }

        newNode.Parent = handbookClassNode;
        newNode.New = false;
        handbookClassNode.ReplaceChildren(newNode);

        ItemFactoryClass.WeaponBuildsStorageClass.Dictionary_0[buildId] = updatedBuild;
        Session.WeaponBuildsStorage.Dictionary_0[buildId] = updatedBuild;

        if (_currentScreenSingletonClass.CurrentScreenController.ScreenType == EFT.UI.Screens.EEftScreenType.EditBuild)
        {
            var buildScreen = (EditBuildScreen)_currentScreenSingletonClass.Dictionary_0[EFT.UI.Screens.EEftScreenType.EditBuild];

            var value = (WeaponBuildClass)EditBuildScreenPatch.WeaponBuildClassField.GetValue(buildScreen);

            if (value != null)
            {
                if (value.Id == updatedBuild.Id)
                {
                    AsyncWorker.RunInMainTread(() =>
                    {
                        buildScreen.method_31(updatedBuild);
                    });
                }
            }
        }
    }

    public EntityNodeClass CreateHandbookEntityNode(WeaponBuildClass build)
    {
        EntityNodeClass handbookEntityNode = _handbookClass[build.Item.TemplateId];

        return new EntityNodeClass
        {
            Data = new HandbookData
            {
                Name = build.HandbookName,
                Item = build.Item,
                Id = build.Id,
                Type = ENodeType.Item,
                ParentId = handbookEntityNode.Data.Id,
                FromBuild = true,
            },
            Count = 1,
            Depth = handbookEntityNode.Depth + 1,
        };
    }

    private async Task ReconnectWebSocket()
    {
        reconnecting = true;

        while (reconnecting)
        {
            if (_webSocket.ReadyState == WebSocketState.Open)
            {
                break;
            }

            // Don't attempt to reconnect if we're still attempting to connect.
            if (_webSocket.ReadyState != WebSocketState.Connecting)
            {
                _webSocket.Connect();
            }

            await Task.Delay(10 * 1000);

            if (_webSocket.ReadyState == WebSocketState.Open)
            {
                break;
            }
        }

        reconnecting = false;
    }
}

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using SharedWeaponBuilds.Server.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Utils;

namespace SharedWeaponBuilds.Server.WebSockets;

/// <summary>
/// Most of this code I mostly copied from Fika's websocket implementation, credits to the Fika team.. Which is also myself I guess so xdx
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class WeaponBuildsWebSocket(ISptLogger<WeaponBuildsWebSocket> logger, JsonUtil jsonUtil) : IWebSocketConnectionHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _clientWebSockets = [];

    public string GetHookUrl()
    {
        return "/sharedweaponbuilds/websocket/";
    }

    public string GetSocketId()
    {
        return "Shared Weapon Builds WebSocket";
    }

    public Task OnClose(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        var client = _clientWebSockets.FirstOrDefault(x => x.Value == ws);

        if (client.Key != null)
        {
            logger.Debug($"[{GetSocketId()}] Deleting client ${client.Key}");

            _clientWebSockets.TryRemove(client.Key, out _);
        }

        return Task.CompletedTask;
    }

    public async Task OnConnection(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "", CancellationToken.None);
            return;
        }

        var base64EncodedString = authHeader.Split(' ')[1];
        var decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedString));
        var authorization = decodedString.Split(':');
        var userSessionID = authorization[0];

        logger.Debug($"[{GetSocketId()}] User is {userSessionID}");

        if (!_clientWebSockets.TryAdd(userSessionID, ws))
        {
            logger.Warning($"[{GetSocketId()}] Could not add {userSessionID} as it already exists?");
            return;
        }
    }

    public Task OnMessage(byte[] rawData, WebSocketMessageType messageType, WebSocket ws, HttpContext context)
    {
        return Task.CompletedTask;
    }

    public async Task SendAsync(MongoId sessionID, UpdatedWeaponMessage message)
    {
        // Client is not online or not currently connected to the websocket.
        if (!_clientWebSockets.TryGetValue(sessionID, out WebSocket? ws))
        {
            return;
        }

        // Client was formerly connected to the websocket, but may have connection issues as it didn't run onClose
        if (ws is null || ws.State == WebSocketState.Closed)
        {
            return;
        }

        await ws.SendAsync(Encoding.UTF8.GetBytes(jsonUtil.Serialize(message)), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task BroadcastAsync(MongoId broadcaster, UpdatedWeaponMessage message)
    {
        foreach (var websocket in _clientWebSockets)
        {
            if (websocket.Key == broadcaster)
            {
                continue;
            }

            await SendAsync(websocket.Key, message);
        }
    }
}

using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    private FacepunchTransport transport;
    private ConnectionManager connectionManager;

    private void Awake()
    {
        transport = GetComponent<FacepunchTransport>();
        connectionManager = GetComponent<ConnectionManager>();
    }

    public void OpenSteamOverlay()
    {
        SteamFriends.OpenOverlay("friends");
    }

    private void OnEnable()
    {
        SteamFriends.OnGameLobbyJoinRequested += HandleGameLobbyJoinRequest;
    }

    private void OnDisable()
    {
        SteamFriends.OnGameLobbyJoinRequested -= HandleGameLobbyJoinRequest;
    }

    private void HandleGameLobbyJoinRequest(Lobby lobby, SteamId id)
    {
        transport.targetSteamId = id;
        connectionManager.StartGameMultiplayerOnlineClient();
    }
}

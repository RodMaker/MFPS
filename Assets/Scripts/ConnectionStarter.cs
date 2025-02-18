using System.Collections;
using PurrLobby;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Steam;
using PurrNet.Transports;
using Steamworks;
using UnityEngine;

public class ConnectionStarter : MonoBehaviour
{
    private SteamTransport _steamTransport;
    private UDPTransport _udpTransport;
    private NetworkManager _networkManager;
    private LobbyDataHolder _lobbyDataHolder;

    private bool _isFromLobby;

    private void Awake()
    {
        if (!TryGetComponent(out _steamTransport))
            PurrLogger.LogError($"Failed to get {nameof(SteamTransport)} component.", this);

        if (!TryGetComponent(out _udpTransport))
            PurrLogger.LogError($"Failed to get {nameof(UDPTransport)} component.", this);

        if (!TryGetComponent(out _networkManager))
            PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);

        _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (_lobbyDataHolder)
            _isFromLobby = true;
    }

    private void Start()
    {
        if (!_networkManager)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
            return;
        }

        if (!_steamTransport)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(SteamTransport)} is null!", this);
            return;
        }

        if (_isFromLobby)
        {
            StartFromLobby();
        }
        else
        {
            StartNormal();
        }
    }

    private void StartNormal()
    {
        _networkManager.transport = _udpTransport;

        if (!ParrelSync.ClonesManager.IsClone())
            _networkManager.StartServer();
        _networkManager.StartClient();
    }

    private void StartFromLobby()
    {
        _networkManager.transport = _steamTransport;

        if (!_lobbyDataHolder)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
            return;
        }

        if (!_lobbyDataHolder.CurrentLobby.IsValid)
        {
            PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
            return;
        }

        if (!ulong.TryParse(_lobbyDataHolder.CurrentLobby.lobbyId, out ulong ulongId))
        {
            Debug.LogError($"Failed to parse lobbyid into ulong!", this);
        }

        var lobbyOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(ulongId));

        if (!lobbyOwner.IsValid())
        {
            Debug.LogError($"Failed to get lobby owner from parsed lobby ID", this);
        }

        _steamTransport.address = lobbyOwner.ToString();

        if (_lobbyDataHolder.CurrentLobby.IsOwner)
            _networkManager.StartServer();
        StartCoroutine(StartClient());
    }

    private IEnumerator StartClient()
    {
        yield return new WaitForSeconds(1f);
        _networkManager.StartClient();
    }
}

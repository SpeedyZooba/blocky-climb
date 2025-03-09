using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner _networkRunnerPrefab;
    private NetworkRunner _networkRunner;
    private LobbyBrowser _lobbyBrowser;

    private void Awake()
    {
        if (_networkRunner == null)
        {
            _networkRunner = Instantiate(_networkRunnerPrefab);
            DontDestroyOnLoad(_networkRunner);
            _networkRunner.ProvideInput = true;
            _networkRunner.AddCallbacks(this);
        }
        else
        {
            Destroy(gameObject);
        }
        _lobbyBrowser = LobbyBrowser.Instance;
    }

    public void JoinLobby()
    {
        _networkRunner.JoinSessionLobby(SessionLobby.Shared);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (sessionList.Count == 0)
        {
            Debug.Log("Found no lobbies.");
            _lobbyBrowser.DisplayNotFoundMessage();
        }
        else
        {
            _lobbyBrowser.PurgeList();
            foreach (SessionInfo sessionInfo in sessionList)
            {
                _lobbyBrowser.AddToList(sessionInfo);
            }
        }
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason){ }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}

using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GameState
{
    Going,
    Ended,
}

public struct Base
{
    public Vector3 SpawnPoint;
    public Quaternion SpawnRotation;
}

public class GameManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    public GameState GameState { get { return _currentState; } }
    public float TimeRemaining => _matchTimer.RemainingTime(Runner) ?? 0f;

    [Networked] private TickTimer _matchTimer { get; set; }
    [Networked] private TickTimer _countdownTimer { get; set; }
    [SerializeField] private GameObject _courseBlocks;
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private NetworkPrefabRef _powerUpPrefab;
    [SerializeField] private Transform _lobbySpawnPivot;
    [SerializeField] private Transform _lobbySpawnPoint;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _powerUpSpawnPoint;
    [SerializeField] private Transform _spawnPointPivot;
    [SerializeField] private int _matchDuration;
    [SerializeField] private int _maxHealth;
    private float _lobbyRadius;
    private float _powerUpRadius;
    private Vector3 _spawnCenter;
    private Vector3 _powerUpCenter;
    private float _powerUpHeightOffset = 10;
    private float _powerUpMaxHeight = 70;
    private float _spawnInterval;
    private int _freeSlots => _players.Capacity / 2 - _players.Count;

    // The Networked attribute tells Fusion to copy the property from the server (host client) to other clients (e.g players)
    [Networked, Capacity(8)] private NetworkDictionary<PlayerRef, Player> _players => default;
    [Networked, Capacity(8)] private NetworkDictionary<PlayerRef, Player> _readyPlayers => default;
    // OnChangedRender causes Fusion to check whether a change was made to the property in the Render callback pipeline
    [Networked, OnChangedRender(nameof(CheckGameState))] private GameState _currentState { get; set; }
    [Networked] private Player _winner { get; set; }
    [Networked] private bool _isLobbyReady { get; set; }

    public override void FixedUpdateNetwork()
    {

        if (_players.Count < 1)
        {
            return;
        }

        if (Runner.IsServer && _currentState == GameState.Ended)
        {
            UIManager.Instance.UpdatePlayerTable(_players.ToArray());

            if (_isLobbyReady)
            {
                CollectReadyPlayers();
                StartGame();
            }
        }

        if (_currentState == GameState.Going && !Runner.IsResimulation)
        {
            _spawnInterval += Runner.DeltaTime;
            // Note that this allocates a new array each time so it is inefficient
            // However it is infrequent in this case for its use to be justified, proceed with care in other situations
            UIManager.Instance.UpdateLeaderboard(_readyPlayers.OrderByDescending(player => player.Value.Score).ToArray());
            UIManager.Instance.UpdateTimer(Mathf.RoundToInt(TimeRemaining));
            if (_spawnInterval >= 10f)
            {
                SpawnTimeItem();
                _spawnInterval = 0;
            }
            if (_matchTimer.IsRunning)
            {
                CheckTime();
            }
        }
    }

    public override void Spawned()
    {
        _isLobbyReady = false;
        _matchTimer = TickTimer.None;
        _winner = null;
       _currentState = GameState.Ended;
        UIManager.Instance.SetLobbyUI(_currentState, _winner);
        UIManager.Instance.UpdatePlayerTable(_players.ToArray());
        Runner.SetIsSimulated(Object, true);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // HasStateAuthority ensures that only the host client can perform the action
        if (HasStateAuthority)
        {
            GenerateLobbySpawnPoint(_spawnCenter, _lobbyRadius, out Vector3 spawnPosition);
            CenterRotation(spawnPosition, _spawnCenter, out Quaternion lookDirection);
            NetworkObject playerObject = Runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
            Player newPlayer = playerObject.GetComponent<Player>();
            newPlayer.Teleport(spawnPosition, lookDirection);
            _players.Add(player, newPlayer);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        // The context for the usage of out here is returning multiple values (in this case, the Player found and the bool indicating whether the look-up was a success)
        if (_players.TryGet(player, out Player playerBehaviour))
        {
            _players.Remove(player);
            _readyPlayers.Remove(player);
            Runner.Despawn(playerBehaviour.Object);
            UIManager.Instance.UpdatePlayerTable(_players.ToArray());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Runner.IsServer && _currentState == GameState.Going && _winner == null && other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out Player player))
        {
            _matchTimer = TickTimer.None;
            _winner = player;
            _currentState = GameState.Ended;
            RPC_StopBattleMusic();
            RPC_PlayEndGameSFX();
            ResetGame();
        }
    }

    private void CheckTime()
    {
        if (Runner.IsServer && _matchTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_SetTimeOut();
            _winner = _readyPlayers.OrderByDescending(player => player.Value.Score).FirstOrDefault().Value;
            _currentState = GameState.Ended;
            RPC_StopBattleMusic();
            RPC_PlayEndGameSFX();
            ResetGame();
        }
    }

    private void CheckGameState()
    {
        UIManager.Instance.SetLobbyUI(_currentState, _winner);
    }

    private void ResetGame()
    {
        _isLobbyReady = false;
        _matchTimer = TickTimer.None;
        foreach (KeyValuePair<PlayerRef, Player> player in _players)
        {
            player.Value.IsReady = false;
            player.Value.HealthPoints = _maxHealth;
            
        }
        _readyPlayers.Clear();
        UIManager.Instance.UpdatePlayerTable(_players.ToArray());
        PrepareCourse();
    }

    private void PrepareCourse()
    {
        if (_courseBlocks != null)
        {
            for (int i = 0; i < _courseBlocks.transform.childCount; ++i)
            {
                _courseBlocks.transform.GetChild(i).gameObject.SetActive(true);    
            }
        }
        else
        {
            Debug.Log("Course object missing.");
        }
    }

    private void PreparePlayers()
    {
        float angle = 360f / _players.Count;
        _spawnPointPivot.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0f);
        foreach (KeyValuePair<PlayerRef, Player> player in _readyPlayers)
        {
            Player newPlayer = player.Value;
            GenerateNextSpawnPoint(angle, out Vector3 position, out Quaternion rotation);
            newPlayer.HealthPoints = _maxHealth;
            newPlayer.Teleport(position, rotation);
            newPlayer.ResetCooldowns();
        }
    }

    private void CollectReadyPlayers()
    {
        foreach (KeyValuePair<PlayerRef, Player> player in _players)
        {
            if (player.Value.IsReady)
            {
                _readyPlayers.Add(player.Key, player.Value);
            }
        }
    }

    private void RespawnPlayer(NetworkObject networkObject)
    {
        Player player = networkObject.GetComponent<Player>();
        player.HealthPoints = _maxHealth;
        player.Teleport(player.PlayerBase.SpawnPoint, player.PlayerBase.SpawnRotation);
    }

    private void SetReady(NetworkObject networkObject)
    {
        Player player = networkObject.GetComponent<Player>();
        UIManager.Instance.UpdatePlayerTable(_players.ToArray());
        if (Runner.IsServer && _currentState == GameState.Ended && HasStateAuthority && player.Object.InputAuthority == Runner.LocalPlayer)
        {
            RPC_StartCountdown();
        }
    }

    private void GenerateLobbySpawnPoint(Vector3 centerPoint, float radius, out Vector3 spawnPosition)
    {
        float angle = Random.Range(0, Mathf.PI * 2);
        float distance = Random.Range(0, radius);
        float spawnPosX = centerPoint.x + distance * Mathf.Cos(angle);
        float spawnPosZ = centerPoint.z + distance * Mathf.Sin(angle);
        spawnPosition = new Vector3(spawnPosX, centerPoint.y, spawnPosZ);
    }

    private void GeneratePowerUpSpawnPoint(Vector3 centerPoint, float radius, out Vector3 spawnPosition)
    {
        float angle = Random.Range(0, Mathf.PI * 2);
        float distance = Random.Range(0, radius);
        float spawnPosX = centerPoint.x + distance * Mathf.Cos(angle);
        float spawnPosY = Random.Range(centerPoint.y + _powerUpHeightOffset, _powerUpMaxHeight);
        float spawnPosZ = centerPoint.z + distance * Mathf.Sin(angle);
        spawnPosition = new Vector3(spawnPosX, spawnPosY, spawnPosZ);
    }

    private void CenterRotation(Vector3 playerPos, Vector3 centerPoint, out Quaternion lookDirection)
    {
        Vector3 direction = (centerPoint - playerPos).normalized;
        Quaternion qDirection = Quaternion.LookRotation(direction);
        lookDirection = Quaternion.Euler(0, qDirection.eulerAngles.y, 0);
    }

    // Generate positions for players to spawn in a circle with varied spaces in between determined by angle.
    private void GenerateNextSpawnPoint(float angle, out Vector3 position, out Quaternion rotation)
    {
        position = _spawnPoint.position;
        rotation = _spawnPoint.rotation;
        _spawnPointPivot.Rotate(0f, angle, 0f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartCountdown()
    {
        StartCoroutine(BeginCountdown());
    }

    private IEnumerator BeginCountdown()
    {
        int countdown = 3;
        while (countdown > 0)
        {
            UIManager.Instance.SetCountdownUI(countdown);
            yield return new WaitForSeconds(1f);
            countdown--;
        }
        _isLobbyReady = true;
    }

    private void ChangeTime(int time, float chance)
    {
        int roll = Random.Range(1, 11);
        if (roll <= chance * 10)
        {
            _matchTimer = TickTimer.CreateFromSeconds(Runner, TimeRemaining - time);
        }
        else
        {
            _matchTimer = TickTimer.CreateFromSeconds(Runner, TimeRemaining + time);
        }
    }

    private void SpawnTimeItem()
    {
        if (HasStateAuthority)
        {
            GeneratePowerUpSpawnPoint(_powerUpCenter, _powerUpRadius, out Vector3 spawnPosition);
            NetworkObject newPowerUp = Runner.Spawn(_powerUpPrefab, Vector3.zero, Quaternion.identity);
            RPC_SetItemPosition(newPowerUp, spawnPosition);
        }
    }

    private void StartGame()
    {
        RPC_PlayBeginGameSFX();
        _winner = null;
        _currentState = GameState.Going;
        PrepareCourse();
        PreparePlayers();
        _matchTimer = TickTimer.CreateFromSeconds(Runner, _matchDuration);
        RPC_PlayBattleMusic();
        NetworkObject newPowerUp = Runner.Spawn(_powerUpPrefab, Vector3.zero, Quaternion.identity);
        RPC_SetItemPosition(newPowerUp, new Vector3(2, 3, 0));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetItemPosition(NetworkObject item, Vector3 position)
    {
        item.transform.position = position;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBeginGameSFX()
    {
        AudioManager.Instance.PlayStartFX();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetTimeOut()
    {
        UIManager.Instance.SetTimeOut();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBattleMusic()
    {
        AudioManager.Instance.PlayBattleMusic();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEndGameSFX()
    {
        AudioManager.Instance.PlayWinSFX();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopBattleMusic()
    {
        AudioManager.Instance.StopBattleMusic();
    }
    private void Awake()
    {
        _spawnInterval = 0;
        _lobbyRadius = Vector3.Distance(_lobbySpawnPivot.position, _lobbySpawnPoint.position);
        _powerUpRadius = Vector3.Distance(_spawnPointPivot.position, _powerUpSpawnPoint.position);
        _spawnCenter = _lobbySpawnPivot.position;
        _powerUpCenter = _spawnPointPivot.position;
    }

    private void OnEnable()
    {
        Player.OnDeath += RespawnPlayer;
        Player.OnReady += SetReady;
        TimeItem.OnTaken += ChangeTime;
    }

    private void OnDisable()
    {
        Player.OnDeath -= RespawnPlayer;
        Player.OnReady -= SetReady;
        TimeItem.OnTaken -= ChangeTime;
    }
}
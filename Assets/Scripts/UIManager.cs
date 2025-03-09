using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Serializable]
    private struct LeaderboardEntry
    {
        public TextMeshProUGUI PlayerName;
        public Slider HealthPoints;
        public TextMeshProUGUI Score;
    }

    [Serializable]
    private struct PlayerInfo
    {
        public TextMeshProUGUI PlayerName;
        public TextMeshProUGUI Status;
    }

    public Player LocalPlayer;
    public static UIManager Instance;
    [SerializeField] private LeaderboardEntry[] _leaderboard;
    [SerializeField] private PlayerInfo[] _playerTable;
    [SerializeField] private GameObject _adminPanel;
    #region Timers
    [SerializeField] private TextMeshProUGUI _matchTimer;
    [SerializeField] private Slider _doubleJumpTimer;
    [SerializeField] private Slider _grapplingHookTimer;
    [SerializeField] private Slider _glideTimer;
    [SerializeField] private Slider _breakTimer;
    [SerializeField] private Slider _laserTimer;
    [SerializeField] private Image _glideActivity;
    #endregion
    #region Game UI
    [SerializeField] private TextMeshProUGUI _gameState;
    [SerializeField] private TextMeshProUGUI _instruction;
    [SerializeField] private AudioSource _timeAlertSFX;
    private Coroutine _blinkingRoutine;
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Extra instance of Camera found.");
        }
        _doubleJumpTimer.value = 0;
        _grapplingHookTimer.value = 0;
        _glideTimer.value = 0;
        _breakTimer.value = 0;
        _laserTimer.value = 0;
    }

    private void Update()
    {
        if (LocalPlayer != null)
        {
            _doubleJumpTimer.value = LocalPlayer.DoubleJumpRemaining;
            _grapplingHookTimer.value = LocalPlayer.GrapplingHookRemaining;
            _glideActivity.enabled = LocalPlayer.IsGliding;
            _glideTimer.value = LocalPlayer.IsGliding ? LocalPlayer.RemainingGlide : LocalPlayer.GlidingRemaining;
            _breakTimer.value = LocalPlayer.BreakRemaining;
            _laserTimer.value = LocalPlayer.LaserRemaining;
        }
    }

    public void PlayerIsReady()
    {
        _instruction.text = "Waiting for other players to get ready...";
    }

    public void SetLobbyUI(GameState currentState, Player winner)
    {
        if (currentState == GameState.Ended)
        {
            if (winner == null)
            {
                _gameState.text = "Getting Ready...";
                _instruction.text = "Press R when you're ready to begin.";
            }
            else
            {
                _gameState.text = $"{winner.Nickname} Wins!";
                _instruction.text = "Press R when you're ready to play again!";
            }
        }
        _gameState.enabled = currentState == GameState.Ended;
        _instruction.enabled = currentState == GameState.Ended;
    }

    public void SetCountdownUI(int seconds)
    {
        _gameState.text = "";
        _instruction.text = $"The game will begin in {seconds}...";
    }

    public void SetAdminPanel()
    {
        if (!LocalPlayer.HasStateAuthority)
        {
            _adminPanel.gameObject.SetActive(false);
        }
    }

    public void UpdateLeaderboard(KeyValuePair<PlayerRef, Player>[] players)
    {
        for (int i = 0; i < _leaderboard.Length; i++)
        {
            LeaderboardEntry item = _leaderboard[i];
            if (i < players.Length)
            {
                item.PlayerName.text = players[i].Value.Nickname;
                item.HealthPoints.gameObject.SetActive(true);
                item.HealthPoints.value = players[i].Value.HealthPoints;
                item.Score.text = $"{players[i].Value.Score}m";
            }
            else
            {
                item.PlayerName.text = "";
                item.HealthPoints.gameObject.SetActive(false);
                item.Score.text = "";
            }
        }
    }

    public void UpdatePlayerTable(KeyValuePair<PlayerRef, Player>[] players)
    {
        for (int i = 0; i < _playerTable.Length; i++)
        {
            PlayerInfo item = _playerTable[i];
            if (i < players.Length)
            {
                item.PlayerName.text = players[i].Value.Nickname;
                item.Status.text = players[i].Value.IsReady ? "Ready" : "Not Ready";
            }
            else
            {
                item.PlayerName.text = "";
                item.Status.text = "";
            }
        }
    }

    public void UpdateTimer(int time)
    {
        TimeSpan timeLeft = TimeSpan.FromSeconds(time);
        if (time <= 10) 
        {
            if (_blinkingRoutine == null)
            {
                _blinkingRoutine = StartCoroutine(Blink(time));
            }
            
            if (time % 2 != 0)
            {
                _matchTimer.color = Color.white;
            }
        }
        _matchTimer.text = timeLeft.ToString(@"mm\:ss");
    }

    public void SetTimeOut()
    {
        _matchTimer.color = Color.white;
        _matchTimer.text = "Time out!";
    }

    private IEnumerator Blink(int time)
    {
        while (time >= 0)
        {
            _timeAlertSFX.Play();
            _matchTimer.color = Color.red;
            yield return new WaitForSeconds(2f);
            time -= 2;
        }
    }
}
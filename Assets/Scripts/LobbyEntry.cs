using Fusion;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyEntry : MonoBehaviour
{
    public event Action<SessionInfo> OnJoinLobby;

    private SessionInfo _lobbyInfo;
    [SerializeField] private TextMeshProUGUI _sessionName;
    [SerializeField] private TextMeshProUGUI _playerCount;
    [SerializeField] private Button _joinButton;

    public void PopulateEntry(SessionInfo sessionInfo)
    {
        _lobbyInfo = sessionInfo;
        _sessionName.text = sessionInfo.Name;
        _playerCount.text = $"{sessionInfo.PlayerCount.ToString()}/{sessionInfo.MaxPlayers.ToString()}";

        if (sessionInfo.PlayerCount >= sessionInfo.MaxPlayers)
        {
            _joinButton.gameObject.SetActive(false);
        }
        else
        {
            _joinButton.gameObject.SetActive(true);
        }
    }

    public void OnClick()
    {
        OnJoinLobby?.Invoke(_lobbyInfo);
    }
}
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class LobbyBrowser : MonoBehaviour
{
    public static LobbyBrowser Instance;

    [SerializeField] private TextMeshProUGUI _status;
    [SerializeField] private LobbyEntry _lobby;
    [SerializeField] private VerticalLayoutGroup _layout;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Extra instance of LobbyBrowser found.");
        }
        PurgeList();
    }

    // Clears the list to display newest search results
    public void PurgeList()
    {
        foreach (Transform entry in _layout.transform)
        {
            Destroy(entry.gameObject);
        }
        _status.gameObject.SetActive(false);
    }

    public void AddToList(SessionInfo sessionInfo)
    {
        LobbyEntry newEntry = Instantiate(_lobby, _layout.transform).GetComponent<LobbyEntry>();
        newEntry.PopulateEntry(sessionInfo);

        newEntry.OnJoinLobby += NewEntry_OnJoinLobby;
    }

    public void OpenLobbyBrowser()
    {
        gameObject.SetActive(true);
        DisplayOngoingSearchMessage();
    }

    public void ExitLobbyBrowser()
    {
        gameObject.SetActive(false);
    }

    public void DisplayNotFoundMessage()
    {
        _status.text = "No active lobbies found.";
        _status.gameObject.SetActive(true);
    }

    public void DisplayOngoingSearchMessage()
    {
        _status.text = "Loading...";
        _status.gameObject.SetActive(true);
    }

    private void NewEntry_OnJoinLobby(SessionInfo obj)
    {
        
    }
}
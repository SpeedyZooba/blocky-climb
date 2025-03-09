using Fusion;
using TMPro;
using UnityEngine;

public class LoadManager : MonoBehaviour
{
    private GameObject _loadingScreen;
    private NetworkRunner _runner;

    private void Awake()
    {
        _runner = GetComponent<NetworkRunner>();
        _loadingScreen = GameObject.Find("FusionMenuViewLoading");
    }

    private void Update()
    {
        if (_loadingScreen.activeInHierarchy && !_runner.IsRunning)
        {
            _loadingScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Connecting to host...";
        }
        else
        {
            _loadingScreen.GetComponentInChildren<TextMeshProUGUI>().text = "Loading game lobby...";
        }
    }
}
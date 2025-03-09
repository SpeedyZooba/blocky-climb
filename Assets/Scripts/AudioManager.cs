using UnityEngine;
using Fusion;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioSource _battleMusic;
    [SerializeField] private AudioSource _startFX;
    [SerializeField] private AudioSource _winFX;

    public void PlayBattleMusic()
    {
        _battleMusic.Play();
    }

    public void PlayWinSFX()
    {
        _winFX.Play();
    }

    public void PlayStartFX()
    {
        _startFX.Play();
    }

    public void StopBattleMusic()
    {
        _battleMusic.Stop();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.Log("Extra instance of AudioManager found.");
            Destroy(gameObject);
        }
    }
}
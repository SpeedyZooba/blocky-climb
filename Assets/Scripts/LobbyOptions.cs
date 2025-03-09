using UnityEngine;

public class LobbyOptions : MonoBehaviour
{
    public void ShowOptions()
    {
        gameObject.SetActive(true);
    }

    public void HideOptions()
    {
        gameObject.SetActive(false); 
    }
}

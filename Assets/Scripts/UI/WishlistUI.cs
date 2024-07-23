using UnityEngine;
using UnityEngine.SceneManagement;

public class WishlistUI : MonoBehaviour
{
    [SerializeField] private string discordLink;
    [SerializeField] private string wishlistLink;
    
    private const int MainSceneIndex = 1;

    public void ShowMainMenu()
    {
        SceneManager.LoadScene(MainSceneIndex, LoadSceneMode.Single);
    }

    public void WishlistGame()
    {
        if (string.IsNullOrEmpty(wishlistLink)) return;
        Application.OpenURL(wishlistLink);
    }

    public void InviteToDiscord()
    {
        if (string.IsNullOrEmpty(discordLink)) return;
        Application.OpenURL(discordLink);
    }
}

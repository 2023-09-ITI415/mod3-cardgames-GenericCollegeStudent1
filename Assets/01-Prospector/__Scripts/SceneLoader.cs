using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene_prospector(string __Prospector)
    {
        SceneManager.LoadScene(__Prospector);
    }
    public void LoadScene_poker(string Poker)
    {
        SceneManager.LoadScene(Poker);
    }
        
}
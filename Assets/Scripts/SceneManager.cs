using UnityEngine;
using manager=UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    public void NextScene()
    {
        int currentSceneIndex = manager.GetActiveScene().buildIndex;
        manager.LoadScene(currentSceneIndex + 1);
    }
    public void StartScene()
    {
        manager.LoadScene(0);
        Debug.Log("BackToTheMiddle");
    }
}

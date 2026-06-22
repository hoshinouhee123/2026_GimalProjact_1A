using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }
}

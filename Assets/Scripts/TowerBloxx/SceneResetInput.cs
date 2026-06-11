using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneResetInput : MonoBehaviour
{
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    private void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            ResetScene();
        }
    }

    private void ResetScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
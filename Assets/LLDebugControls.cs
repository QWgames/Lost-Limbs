using UnityEngine;
using UnityEngine.SceneManagement;
public class LLDebugControls : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private KeyCode resetKey = KeyCode.F5;
    private void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            ResetScene();
        }
    }

    public void ResetScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }
}

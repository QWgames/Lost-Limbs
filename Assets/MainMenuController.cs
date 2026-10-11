
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject creditsDimOverlay;

    [Header("Transition")]
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;

    [Header("Game")]
    [SerializeField] private string gameSceneName = "MainScene";

    private bool isTransitioning;

    private void Start()
    {
        // Set the initial screen.
        mainMenuPanel.SetActive(true);
        creditsPanel.SetActive(false);

        if (creditsDimOverlay != null)
            creditsDimOverlay.SetActive(false);

        // Fade into the menu from black.
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);

            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;

            StartCoroutine(Fade(1f, 0f));
        }
    }

    public void PlayGame()
    {
        if (!isTransitioning)
            StartCoroutine(LoadGame());
    }

    public void OpenCredits()
    {
        if (!isTransitioning)
            StartCoroutine(SwitchScreen(false));
    }

    public void BackToMenu()
    {
        if (!isTransitioning)
            StartCoroutine(SwitchScreen(true));
    }

    private IEnumerator SwitchScreen(bool showMenu)
    {
        isTransitioning = true;

        // Fade to black before switching screens.
        yield return StartCoroutine(Fade(0f, 1f));

        mainMenuPanel.SetActive(showMenu);
        creditsPanel.SetActive(!showMenu);

        // Dim the background while Credits is open.
        if (creditsDimOverlay != null)
            creditsDimOverlay.SetActive(!showMenu);

        // Fade back into the selected screen.
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
    }

    private IEnumerator LoadGame()
    {
        isTransitioning = true;

        // Fade out before loading gameplay.
        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = startAlpha;
        fadeImage.color = color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        // Hide the fade image when the screen is fully visible.
        if (endAlpha <= 0f)
            fadeImage.gameObject.SetActive(false);
    }
}

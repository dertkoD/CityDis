using System.Collections;
using UnityEngine;

// A simple, persistent full-screen fader. Put one in the 3D map scene; it
// survives scene loads (DontDestroyOnLoad) so it can fade the transition both
// into and out of the 2D mini game.
//
// Setup (in the editor, no prefab needed):
//   - Create a UI Canvas (Render Mode = Screen Space - Overlay, high Sort Order).
//   - Add a full-screen black Image to it.
//   - Add a CanvasGroup to the Canvas (or the Image) and a ScreenFader component.
//   - Assign the CanvasGroup to "Fade Group". Start with its Alpha at 0.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(1f);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(0f);
    }

    private IEnumerator Fade(float target)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        // Block clicks while the screen is (partly) covered so they don't leak
        // through to whichever scene is underneath.
        fadeGroup.blocksRaycasts = true;

        float start = fadeGroup.alpha;

        for (float time = 0f; fadeDuration > 0f && time < fadeDuration; time += Time.unscaledDeltaTime)
        {
            fadeGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = target;
        fadeGroup.blocksRaycasts = target > 0.5f;
    }
}

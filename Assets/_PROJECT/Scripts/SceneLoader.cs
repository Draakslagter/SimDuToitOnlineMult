using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;
    public static SceneLoader Instance => _instance;
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private float transitionDuration = 0.5f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private static void SwitchScene(int sceneIndex)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
    }
    public void SwitchWithTransition(int sceneIndex)
    {
        Time.timeScale = 1.0f;
        StartCoroutine(TransitionLerp(sceneIndex));
    }
    private IEnumerator TransitionLerp(int sceneIndex)
    {
        var timeElapsed = 0.0f;
        while (timeElapsed < transitionDuration)
        {
            transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timeElapsed / transitionDuration);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        transitionCanvasGroup.alpha = 1f;
        SwitchScene(sceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
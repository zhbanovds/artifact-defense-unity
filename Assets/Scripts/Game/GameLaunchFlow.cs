using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameLaunchFlow
{
    private const string MainMenuSceneName = "Main Menu";
    private const string GameSceneName = "SampleScene";

    private static bool gameStartedFromMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RedirectEditorPlayModeToMainMenu()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name != GameSceneName || gameStartedFromMenu)
        {
            return;
        }

        SceneManager.LoadScene(MainMenuSceneName);
    }

    public static void StartGame(string sceneName)
    {
        gameStartedFromMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public static void ReturnToMainMenu(string sceneName)
    {
        gameStartedFromMenu = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Следит за концом игры (чей трон разрушен — тот проиграл) и пауза по Esc.
public class GameManager : MonoBehaviour
{
    public const string MenuSceneName = "MainMenu";

    public static GameManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public Team Winner { get; private set; }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (!IsGameOver && !MatchSetup.InLobby && kb != null && kb.escapeKey.wasPressedThisFrame)
            SetPaused(!IsPaused);
    }

    void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused && !Net.Online ? 0f : 1f;   // по сети время не останавливаем
    }

    public void OnThroneDestroyed(Team loser)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Winner = loser == Team.Kurchatov ? Team.Lobnya : Team.Kurchatov;
        Time.timeScale = 0f;
        NetWorld.GameOverEvent(Winner);
        Debug.Log($"Трон города {TeamUtil.GetName(loser)} разрушен! Победил город {TeamUtil.GetName(Winner)}.");
    }

    void OnGUI()
    {
        GUI.depth = -30; // пауза и победа — самый верх
        // Кнопка меню/паузы (нужна на телефоне, где нет Esc)
        if (!IsGameOver && !MatchSetup.InLobby)
        {
            float sc = Screen.height / 1080f;
            var pr = new Rect(Screen.width - 90 * sc, 12 * sc, 76 * sc, 56 * sc);
            HudArea.Add(pr);
            var ps = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(26 * sc), fontStyle = FontStyle.Bold };
            if (GUI.Button(pr, IsPaused ? ">" : "II", ps)) SetPaused(!IsPaused);
        }

        if (!IsGameOver && !IsPaused) return;

        float w = 420, h = IsGameOver ? 220 : 260;
        var rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GUI.Box(rect, "");

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        float bx = rect.x + 110, by = rect.y + 85;

        if (IsGameOver)
        {
            style.normal.textColor = TeamUtil.GetColor(Winner);
            GUI.Label(new Rect(rect.x, rect.y + 15, w, 60), $"Победил город {TeamUtil.GetName(Winner)}!", style);

            if (!Net.Online && GUI.Button(new Rect(bx, by, 200, 40), "Играть ещё раз"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (GUI.Button(new Rect(bx, by + 55, 200, 40), "В главное меню")) GoToMenu();
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x, rect.y + 15, w, 60), "Пауза", style);

            if (GUI.Button(new Rect(bx, by, 200, 40), "Продолжить")) SetPaused(false);
            if (GUI.Button(new Rect(bx, by + 55, 200, 40), "В главное меню")) GoToMenu();
            if (GUI.Button(new Rect(bx, by + 110, 200, 40), "Выход")) MainMenu.Quit();
        }
    }

    // Клиент сети: хост сообщил, что трон пал
    public void NetGameOver(Team winner)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Winner = winner;
        Time.timeScale = 0f;
    }

    void GoToMenu()
    {
        if (Net.Online) { Net.Leave(); return; }
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuSceneName);
    }
}

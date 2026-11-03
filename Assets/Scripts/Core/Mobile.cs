using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Телефон: облегчённая графика, ввод текста через окно браузера, кнопки вместо клавиш.
public static class Mobile
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern string MpkaPrompt(string title, string def);
    [DllImport("__Internal")] static extern int MpkaIsMobile();
    [DllImport("__Internal")] static extern void MpkaShare(string text, bool share);
#endif

    // Скопировать (или «Поделиться» на телефоне) — в браузере обычный буфер Unity не доходит до системы
    public static void Copy(string text, bool share = false)
    {
        GUIUtility.systemCopyBuffer = text;
#if UNITY_WEBGL && !UNITY_EDITOR
        try { MpkaShare(text, share); } catch { }
#endif
    }

    static int cached = -1;
    public static bool IsPhone
    {
        get
        {
            if (cached < 0)
            {
                bool m = Application.isMobilePlatform || (Application.absoluteURL ?? "").Contains("mobile=1");
#if UNITY_WEBGL && !UNITY_EDITOR
                try { m |= MpkaIsMobile() == 1; } catch { }
#endif
                cached = m ? 1 : 0;
            }
            return cached == 1;
        }
    }

    // Ввести текст: на телефоне — окно браузера, иначе null (рисуем обычное поле)
    public static string Prompt(string title, string def)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return MpkaPrompt(title, def ?? ""); } catch { return def; }
#else
        return def;
#endif
    }

    // Масштаб интерфейса: на ПК экран 1080 — 1, на телефоне всё крупнее (экран маленький и близко)
    public static float UiScale => IsPhone ? Mathf.Min(Screen.height / 680f, Screen.width / 1650f) : Screen.height / 1080f;
    // Меню широкое (рассчитано на 1920): на телефоне ограничиваем и по ширине
    public static float MenuScale => IsPhone ? Mathf.Min(Screen.height / 680f, Screen.width / 1920f) : Screen.height / 1080f;

    // Камера сама держит героя в центре (на телефоне), пока не потянешь её пальцем
    public static bool Follow = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Apply();
        Apply();
    }

    static void Apply()
    {
        if (!IsPhone) return;
        Application.targetFrameRate = 45;
        QualitySettings.lodBias = 0.6f;
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = 0.8f;
            urp.shadowDistance = 30f;
            urp.msaaSampleCount = 1;
        }
        // постэффекты и мелкая трава — самое тяжёлое для телефона
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) v.enabled = false;
        var soft = GameObject.Find("DecorSoft");
        if (soft != null) soft.SetActive(false);
        if (Object.FindFirstObjectByType<MobileHud>() == null && SceneManager.GetActiveScene().name != Net.MenuScene)
            new GameObject("MobileHud").AddComponent<MobileHud>();
    }
}

// Кнопки для телефона: камера к герою (и слежение), таблица счёта
public class MobileHud : MonoBehaviour
{
    public static bool ScoreOpen;
    RTSCamera rig;

    void Start() => rig = FindFirstObjectByType<RTSCamera>();

    void LateUpdate()
    {
        var hero = HeroController.Player;
        if (!Mobile.Follow || hero == null || rig == null || MatchSetup.InLobby) return;
        Vector3 p = hero.transform.position, c = rig.transform.position;
        rig.JumpTo(Vector3.Lerp(c, p, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime)));
    }

    void OnGUI()
    {
        if (MatchSetup.InLobby || HeroController.Player == null) return;
        var gm = GameManager.Instance;
        if (gm != null && gm.IsGameOver) return;
        GUI.depth = -20;
        float s = Mobile.UiScale;
        var st = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(24 * s), fontStyle = FontStyle.Bold };
        float w = 200 * s, h = 70 * s, x = Screen.width - w - 14 * s, y = 150 * s;
        var r1 = new Rect(x, y, w, h);
        var r2 = new Rect(x, y + h + 12 * s, w, h);
        HudArea.Add(r1); HudArea.Add(r2);
        GUI.color = Mobile.Follow ? new Color(0.6f, 1f, 0.6f) : Color.white;
        if (GUI.Button(r1, Mobile.Follow ? "ЗА ГЕРОЕМ" : "К ГЕРОЮ", st)) Mobile.Follow = true;
        GUI.color = ScoreOpen ? new Color(1f, 0.85f, 0.4f) : Color.white;
        if (GUI.Button(r2, "СЧЁТ", st)) ScoreOpen = !ScoreOpen;
        GUI.color = Color.white;
    }
}

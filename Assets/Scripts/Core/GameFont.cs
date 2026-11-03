using UnityEngine;

// Шрифт с кириллицей для всего интерфейса. В браузере (WebGL) нет системных шрифтов,
// и встроенный шрифт Unity показывает только латиницу — поэтому везде Inter из Resources/Fonts.
[DefaultExecutionOrder(-1000)]
public class GameFont : MonoBehaviour
{
    static Font main, bold;

    public static Font Get() { if (main == null) main = Resources.Load<Font>("Fonts/Inter-Regular"); return main != null ? main : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
    public static Font Bold() { if (bold == null) bold = Resources.Load<Font>("Fonts/Inter-SemiBold"); return bold != null ? bold : Get(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindFirstObjectByType<GameFont>() != null) return;
        var go = new GameObject("GameFont");
        DontDestroyOnLoad(go);
        go.AddComponent<GameFont>().useGUILayout = false;
    }

    void Awake() { useGUILayout = false; }

    // раньше всех остальных OnGUI: стили, созданные из GUI.skin, уже получат нужный шрифт
    void OnGUI()
    {
        var f = Get();
        if (GUI.skin.font != f) GUI.skin.font = f;
    }
}

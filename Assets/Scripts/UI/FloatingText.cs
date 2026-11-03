using UnityEngine;

// Всплывающий текст над юнитом («x2!», «Не хватает маны» и т.п.).
public class FloatingText : MonoBehaviour
{
    const float Life = 1.3f;

    string text;
    Color color;
    int size;
    Vector3 worldPos;
    float start;

    public static bool NoNet;   // не пересылать по сети (цифры урона — у каждого свои)

    public static void Show(Vector3 worldPos, string text, Color color, int size = 24)
    {
        if (!NoNet) NetWorld.TextEvent(worldPos, text, color, size);
        var go = new GameObject("FloatingText");
        var f = go.AddComponent<FloatingText>();
        f.text = text;
        f.color = color;
        f.size = size;
        f.worldPos = worldPos;
        f.start = Time.time;
    }

    void Update()
    {
        if (Time.time - start > Life) Destroy(gameObject);
    }

    void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float t = Mathf.Clamp01((Time.time - start) / Life);
        Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * t * 1.5f);
        if (sp.z < 0f) return;

        float s = Screen.height / 1080f;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * s * (1f + 0.3f * (1f - t))),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        var r = new Rect(sp.x - 200, Screen.height - sp.y - 30, 400, 60);

        style.normal.textColor = new Color(0, 0, 0, 1f - t);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = new Color(color.r, color.g, color.b, 1f - t);
        GUI.Label(r, text, style);
    }
}

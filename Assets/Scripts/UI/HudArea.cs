using System.Collections.Generic;
using UnityEngine;

// Запоминает, где на экране интерфейс, чтобы клики по нему не отправляли героя бегать.
public static class HudArea
{
    static readonly List<Rect> rects = new List<Rect>();
    static int frame = -1;

    // Вызывается из OnGUI (координаты GUI: y сверху вниз).
    public static void Add(Rect r)
    {
        if (Time.frameCount != frame) { rects.Clear(); frame = Time.frameCount; }
        rects.Add(r);
    }

    // Позиция мыши из Input System (y снизу вверх).
    public static bool IsOver(Vector2 inputPos)
    {
        var p = new Vector2(inputPos.x, Screen.height - inputPos.y);
        foreach (var r in rects) if (r.Contains(p)) return true;
        return false;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Картинки предметов: Assets/Resources/Items/<id>.png
public static class ItemIcons
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static Texture2D Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!cache.TryGetValue(id, out var t))
        {
            t = Resources.Load<Texture2D>("Items/" + id);
            cache[id] = t;
        }
        return t;
    }
}

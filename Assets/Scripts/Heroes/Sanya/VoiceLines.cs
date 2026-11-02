using System.Collections.Generic;
using UnityEngine;

// Голос героя: записи лежат в Resources/Voice/<id героя>/line1..N.
// Фраза смерти (deathIndex) в случайные реплики не попадает.
public static class VoiceLines
{
    static readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
    static AudioSource source;
    static float busyUntil;
    static int last = -1;

    public const string PrefKey = "mpka_voice";
    public static bool Enabled => PlayerPrefs.GetInt(PrefKey, 1) == 1;

    public static AudioClip[] Clips(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return null;
        if (cache.TryGetValue(heroId, out var c)) return c;
        var list = new List<AudioClip>(Resources.LoadAll<AudioClip>("Voice/" + heroId));
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        c = list.ToArray();
        cache[heroId] = c;
        return c;
    }

    static AudioSource Source()
    {
        if (source == null)
        {
            var go = new GameObject("HeroVoice");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }
        return source;
    }

    // Насколько далеко герой от центра экрана (чтобы бота слышно только рядом)
    static float DistanceToView(Vector3 p)
    {
        var cam = Camera.main;
        if (cam == null) return 0f;
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!plane.Raycast(ray, out float d)) return 0f;
        Vector3 c = ray.GetPoint(d); c.y = 0; p.y = 0;
        return Vector3.Distance(c, p);
    }

    public static void Speak(string heroId, Vector3 where, float chance = 1f, bool force = false, int index = -1, int excludeIndex = -1, bool ignoreDistance = false)
    {
        if (!Enabled) return;
        var clips = Clips(heroId);
        if (clips == null || clips.Length == 0) return;
        if (!force && (Time.unscaledTime < busyUntil || Random.value > chance)) return;
        float dist = ignoreDistance ? 0f : DistanceToView(where);
        if (dist > 24f) return;

        int i = index;
        if (i < 0 || i >= clips.Length)
        {
            var pool = new List<int>();
            for (int k = 0; k < clips.Length; k++)
                if (k != excludeIndex && (k != last || clips.Length < 3)) pool.Add(k);
            if (pool.Count == 0) return;
            i = pool[Random.Range(0, pool.Count)];
        }
        last = i;
        var src = Source();
        src.Stop();
        src.clip = clips[i];
        src.volume = Mathf.Clamp(1f - dist / 26f, 0.3f, 1f);
        src.Play();
        busyUntil = Time.unscaledTime + clips[i].length + 0.3f;
    }
}

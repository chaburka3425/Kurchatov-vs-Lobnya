using UnityEngine;

// Вода течёт: текстура реки медленно сдвигается и чуть «дышит».
public class WaterFx : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.02f, 0.06f);
    Material mat;
    Vector2 baseScale;

    void Start()
    {
        mat = GetComponent<Renderer>().material;   // своя копия, чтобы не менять ассет
        baseScale = mat.HasProperty("_BaseMap") ? mat.GetTextureScale("_BaseMap") : mat.mainTextureScale;
    }

    void Update()
    {
        if (mat == null) return;
        Vector2 off = speed * Time.time + new Vector2(Mathf.Sin(Time.time * 0.7f), Mathf.Cos(Time.time * 0.5f)) * 0.01f;
        if (mat.HasProperty("_BaseMap")) mat.SetTextureOffset("_BaseMap", off); else mat.mainTextureOffset = off;
        float k = 1f + Mathf.Sin(Time.time * 0.4f) * 0.02f;
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", baseScale * k);
    }
}

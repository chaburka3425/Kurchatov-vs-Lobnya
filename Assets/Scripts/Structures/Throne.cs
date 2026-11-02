using UnityEngine;

// Трон города. Если его разрушить — эта сторона проигрывает.
[RequireComponent(typeof(Unit), typeof(Health))]
public class Throne : MonoBehaviour
{
    [Tooltip("Объекты, которые уходят под землю вместе с троном (портрет, подпись)")]
    public Transform[] sinkWithThrone;

    Unit unit;

    void Awake()
    {
        unit = GetComponent<Unit>();
        GetComponent<Health>().OnDeath += OnDestroyed;
    }

    void OnDestroyed(Unit killer)
    {
        // Трон "рушится" — опускаем его в землю вместе с портретом (на 60% его высоты).
        float h = transform.localScale.y;
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length > 1)
        {
            float top = transform.position.y;
            foreach (var r in rs) top = Mathf.Max(top, r.bounds.max.y);
            h = top - transform.position.y;
        }
        Vector3 down = Vector3.down * (h * 0.6f);
        transform.position += down;
        if (sinkWithThrone != null)
            foreach (var t in sinkWithThrone)
                if (t != null) t.position += down * 2f;

        if (GameManager.Instance != null) GameManager.Instance.OnThroneDestroyed(unit.team);
    }
}

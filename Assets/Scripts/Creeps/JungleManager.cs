using UnityEngine;

// Лагеря гопников: первые появляются на 0:30, дальше — раз в минуту, если лагерь пуст и рядом нет героев.
public class JungleManager : MonoBehaviour
{
    public float firstSpawn = 30f;
    public float respawn = 60f;

    float[] next;
    float startTime;

    void Start()
    {
        startTime = Time.time;
        next = new float[LaneData.Camps.Length];
        for (int i = 0; i < next.Length; i++) next[i] = startTime + firstSpawn;
    }

    void Update()
    {
        for (int i = 0; i < LaneData.Camps.Length; i++)
        {
            if (Time.time < next[i]) continue;
            next[i] = Time.time + respawn;
            var c = LaneData.Camps[i];
            if (CampAlive(i) || HeroNear(c.pos, 8f)) continue;
            int n = c.big ? 3 : 2;
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2f / n;
                var pos = c.pos + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.6f;
                CreepFactory.CreateNeutral(i, pos, c.big && k == 0);
            }
        }
    }

    static bool CampAlive(int camp)
    {
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.team != Team.Neutral) continue;
            var n = u.GetComponent<NeutralAI>();
            if (n != null && n.camp == camp) return true;
        }
        return false;
    }

    static bool HeroNear(Vector3 p, float r)
    {
        foreach (var u in Unit.All)
            if (u.kind == UnitKind.Hero && u.IsAlive && Vector3.Distance(u.transform.position, p) < r) return true;
        return false;
    }
}

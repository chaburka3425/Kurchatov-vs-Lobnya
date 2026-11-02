using UnityEngine;

// Выпускает волны крипов на одну линию: первая через 5 сек., потом каждые 30 сек.
// Со временем крипы становятся немного сильнее.
public class CreepSpawner : MonoBehaviour
{
    public Team team = Team.Kurchatov;
    public LaneData.Lane lane = LaneData.Lane.Mid;
    public Transform enemyThrone;
    public float firstWave = 5f;
    public float interval = 30f;
    public int meleeCount = 3;
    public int rangedCount = 1;
    [Tooltip("+% к здоровью и урону за каждую волну")]
    public float powerPerWave = 0.03f;

    public static float NextWaveTime { get; private set; }
    public int Wave { get; private set; }
    float next;

    void Start()
    {
        next = Time.time + firstWave;
        NextWaveTime = next;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsGameOver) return;
        if (Time.time < next) return;

        next += interval;
        NextWaveTime = next;
        SpawnWave();
    }

    void SpawnWave()
    {
        Wave++;
        float power = 1f + powerPerWave * (Wave - 1);
        Vector3[] path = LaneData.Path(team, lane);
        Vector3 toEnemy = path[0] - transform.position; toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.01f) toEnemy = Vector3.forward;
        toEnemy.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, toEnemy);

        int total = meleeCount + rangedCount;
        for (int i = 0; i < total; i++)
        {
            bool ranged = i >= meleeCount;
            float offset = (i - (total - 1) / 2f) * 1.2f;
            Vector3 pos = transform.position + side * offset - (ranged ? toEnemy * 1.5f : Vector3.zero);
            var go = CreepFactory.Create(team, ranged, pos, path, power);
            var ai = go.GetComponent<CreepAI>();
            if (ai != null) ai.lane = lane;
        }
    }
}

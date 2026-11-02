using UnityEngine;

// День и ночь по 5 минут (как часы на верхней панели). Ночью солнце садится и синеет,
// окна и фонари светят ярче на фоне темноты, а обзор героев и крипов меньше (как в Доте).
public class DayNight : MonoBehaviour
{
    public const float Half = 300f;       // 5 минут день, 5 минут ночь
    public const float Blend = 20f;       // сколько секунд длится закат/рассвет
    public const float NightVision = 0.7f;

    public Light sun;
    public static float NightAmount { get; private set; }   // 0 — день, 1 — ночь
    public static bool IsNight => NightAmount > 0.5f;
    public static float VisionMult => Mathf.Lerp(1f, NightVision, NightAmount);

    Color daySky = new Color(0.62f, 0.7f, 0.82f), dayEq = new Color(0.5f, 0.55f, 0.48f), dayGround = new Color(0.28f, 0.26f, 0.2f);
    Color nightSky = new Color(0.16f, 0.2f, 0.36f), nightEq = new Color(0.12f, 0.15f, 0.26f), nightGround = new Color(0.07f, 0.07f, 0.1f);
    Color dayFog = new Color(0.7f, 0.76f, 0.82f), nightFog = new Color(0.1f, 0.13f, 0.22f);
    bool wasNight;

    void OnDestroy() => NightAmount = 0f;

    void Update()
    {
        float t = MatchSetup.InLobby ? 0f : MatchSetup.MatchTime;
        // фаза: внутри 5-минутки считаем близость к смене
        float inCycle = Mathf.Repeat(t, Half * 2f);
        float target;
        if (inCycle < Half) target = inCycle > Half - Blend ? Mathf.InverseLerp(Half - Blend, Half, inCycle) : 0f;                 // день → закат
        else target = inCycle > Half * 2f - Blend ? 1f - Mathf.InverseLerp(Half * 2f - Blend, Half * 2f, inCycle) : 1f;              // ночь → рассвет
        NightAmount = Mathf.SmoothStep(0f, 1f, target);

        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(1.25f, 0.35f, NightAmount);
            sun.color = Color.Lerp(new Color(1f, 0.95f, 0.84f), new Color(0.55f, 0.65f, 1f), NightAmount);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(52f, 30f, NightAmount), Mathf.Lerp(-35f, 140f, NightAmount), 0f);
        }
        RenderSettings.ambientSkyColor = Color.Lerp(daySky, nightSky, NightAmount);
        RenderSettings.ambientEquatorColor = Color.Lerp(dayEq, nightEq, NightAmount);
        RenderSettings.ambientGroundColor = Color.Lerp(dayGround, nightGround, NightAmount);
        RenderSettings.fogColor = Color.Lerp(dayFog, nightFog, NightAmount);

        if (IsNight != wasNight && !MatchSetup.InLobby && MatchSetup.MatchTime > 1f)
        {
            wasNight = IsNight;
            var me = HeroController.Player;
            if (me != null) FloatingText.Show(me.transform.position + Vector3.up * 5f, IsNight ? "Наступила ночь — обзор меньше" : "Наступил день",
                IsNight ? new Color(0.6f, 0.7f, 1f) : new Color(1f, 0.9f, 0.5f), 26);
        }
    }
}

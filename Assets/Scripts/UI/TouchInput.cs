using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Управление с телефона:
//  • тап по земле — идти, тап по врагу — атаковать;
//  • нажал способность на панели — тапни по цели;
//  • один палец тянуть — двигать камеру, двумя пальцами — зум;
//  • мини-карта: тап — камера туда, двойной тап — герой туда.
public class TouchInput : MonoBehaviour
{
    // Как только было касание экрана — мышиное управление отключается, чтобы не было двойных команд.
    public static bool UsingTouch { get; private set; }

    HeroController hero => HeroController.Player;
    AbilityCaster caster => hero != null ? hero.GetComponent<AbilityCaster>() : null;
    RTSCamera rig;

    int primaryId = -1;
    Vector2 startPos, lastPos;
    float startTime;
    bool dragging, startedOverHud;
    float lastPinch = -1f;

    void Start()
    {
        rig = FindFirstObjectByType<RTSCamera>();
    }

    void Update()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        var active = new List<TouchControl>();
        foreach (var t in ts.touches)
            if (t.press.isPressed) active.Add(t);

        if (active.Count > 0) UsingTouch = true;

        var gm = GameManager.Instance;
        if (gm != null && (gm.IsPaused || gm.IsGameOver)) { primaryId = -1; return; }

        // Два пальца — зум
        if (active.Count >= 2)
        {
            float d = Vector2.Distance(active[0].position.ReadValue(), active[1].position.ReadValue());
            if (lastPinch > 0f && rig != null) rig.ZoomBy((d - lastPinch) * 0.04f);
            lastPinch = d;
            dragging = true; // это не тап
            return;
        }
        lastPinch = -1f;

        if (active.Count == 1)
        {
            var t = active[0];
            Vector2 p = t.position.ReadValue();
            int id = t.touchId.ReadValue();
            if (id != primaryId)
            {
                primaryId = id;
                startPos = lastPos = p;
                startTime = Time.unscaledTime;
                dragging = false;
                startedOverHud = HudArea.IsOver(p);
            }
            else
            {
                float threshold = Mathf.Max(18f, Screen.height * 0.025f);
                if (!startedOverHud && (p - startPos).magnitude > threshold) dragging = true;
                if (dragging && !startedOverHud && rig != null) rig.PanScreen(p - lastPos);
                lastPos = p;
            }
            return;
        }

        // Палец отпущен
        if (primaryId != -1)
        {
            if (!dragging && !startedOverHud && Time.unscaledTime - startTime < 0.6f) OnTap(lastPos);
            primaryId = -1;
        }
    }

    void OnTap(Vector2 screenPos)
    {
        if (hero == null || MatchSetup.InLobby) return;
        var fx = hero.GetComponent<ItemEffects>();
        if (fx != null && fx.TpTargeting) { fx.TicketAtScreen(screenPos); return; }
        if (caster != null && caster.IsTargeting) caster.TargetAt(screenPos);
        else hero.CommandAt(screenPos);
    }
}

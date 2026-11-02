using UnityEngine;

// Красивые троны: у Курчатова — «Атом Курчатова» (мраморный постамент, стеклянный столп
// и светящийся атом с орбитами-электронами), у Москвы — кремлёвская башня с шатром и красной звездой.
// По углам — чаши с огнём цвета команды, флаги. Всё — дочерние детали корня трона.
public static class ThroneBuilder
{
    static readonly Color Marble = new Color(0.9f, 0.9f, 0.88f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.25f);

    // Постамент-ступени (восьмиугольник) + огненные чаши и флаги по углам
    static void Pedestal(Transform t, string k, Color stone, Color trim, Color team)
    {
        BuildParts.Lathe(t, "Steps", "Throne_Steps", new[] { new Vector2(0f, 0f), new Vector2(3.4f, 0f), new Vector2(3.4f, 0.3f), new Vector2(3.0f, 0.3f), new Vector2(3.0f, 0.6f),
            new Vector2(2.6f, 0.6f), new Vector2(2.6f, 0.9f), new Vector2(0f, 0.9f) }, Vector3.zero, Vector3.one, k + "_Stone", stone, 0f, 0.45f, 8, Quaternion.Euler(0, 22.5f, 0));
        foreach (var (R, y) in new[] { (3.35f, 0.3f), (2.95f, 0.6f), (2.55f, 0.9f) })
            BuildParts.Torus(t, "StepTrim", $"Throne_StepTrim{R:0.00}", R, 0.04f, new Vector3(0, y, 0), Quaternion.Euler(0, 22.5f, 0), Vector3.one, k + "_Trim", trim, 0.85f, 0.7f);
        BuildParts.GlowMesh(t, "TeamRing", BuildParts.Save("Throne_TeamRing", ProcMesh.Torus(3.6f, 0.08f, 56, 6)), new Vector3(0, 0.04f, 0), Quaternion.identity, Vector3.one, "Throne_Ring_" + k, team);

        var fire = Color.Lerp(new Color(1f, 0.55f, 0.12f), team, 0.3f);   // настоящий огонь с оттенком команды
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
            {
                var p = new Vector3(2.55f * x, 0f, 2.55f * z);
                // чаша на ножке с огнём
                BuildParts.Lathe(t, "Brazier", "Throne_Brazier", new[] { new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.18f, 0.2f), new Vector2(0.12f, 1.1f), new Vector2(0.2f, 1.2f),
                    new Vector2(0.5f, 1.45f), new Vector2(0.55f, 1.6f), new Vector2(0.45f, 1.6f), new Vector2(0f, 1.4f) }, p, Vector3.one, k + "_Trim", trim, 0.85f, 0.65f, 18);
                BuildParts.Glow(t, "Flame", PrimitiveType.Sphere, p + Vector3.up * 1.72f, new Vector3(0.5f, 0.45f, 0.5f), "Throne_Flame_" + k, fire);
                foreach (var (o, h) in new[] { (new Vector3(0, 0, 0), 0.75f), (new Vector3(0.14f, 0, 0.05f), 0.45f), (new Vector3(-0.12f, 0, -0.06f), 0.5f) })
                    BuildParts.GlowMesh(t, "FlameTongue", BuildParts.Save("Throne_FlameTongue" + h, ProcMesh.Tube(ProcMesh.Bezier(Vector3.zero, new Vector3(0.05f, h * 0.35f, 0), new Vector3(-0.05f, h * 0.7f, 0.03f), new Vector3(0.02f, h, 0), 8),
                        ProcMesh.Taper(8, 0.2f, 0f, 0.8f), 8)), p + Vector3.up * 1.65f + o, Quaternion.identity, Vector3.one, "Throne_Flame_" + k, fire);
                BuildParts.Glow(t, "FlameCore", PrimitiveType.Sphere, p + Vector3.up * 1.72f, new Vector3(0.35f, 0.45f, 0.35f), "Throne_FlameCore", new Color(1f, 0.95f, 0.75f));
                BuildParts.Ambient(t, "Fire", p + Vector3.up * 1.9f, AmbientFx.Kind.Fire, fire, 0.15f, 0.25f, 2);
            }
        // флаги команды по бокам
        foreach (float x in new[] { -1f, 1f })
        {
            var pole = new Vector3(3.2f * x, 0f, 0f);
            BuildParts.Lathe(t, "FlagPole", "Throne_FlagPole", new[] { new Vector2(0f, 0f), new Vector2(0.08f, 0f), new Vector2(0.06f, 3.4f), new Vector2(0f, 3.5f) }, pole, Vector3.one,
                k + "_Trim", trim, 0.85f, 0.65f, 10);
            BuildParts.Prim(t, "FlagTop", PrimitiveType.Sphere, pole + Vector3.up * 3.55f, Vector3.one * 0.22f, k + "_Trim", trim, 0.85f, 0.65f);
            // полотнище из трёх слегка изогнутых полос
            for (int i = 0; i < 3; i++)
                BuildParts.Prim(t, "Flag", PrimitiveType.Cube, pole + new Vector3(0.45f * x + 0.35f * x * i, 2.9f - i * 0.04f, 0.04f * Mathf.Sin(i * 1.6f)), new Vector3(0.62f, 0.95f - i * 0.08f, 0.03f),
                    k + "_Flag", team, 0f, 0.45f, Quaternion.Euler(0, 8f * Mathf.Sin(i * 1.3f) * x, 0));
        }
    }

    // ---------- Курчатов: Атом ----------
    public static void BuildAtom(Transform t, Color team)
    {
        Pedestal(t, "Kur", Marble, Gold, team);
        // колонна с каннелюрами-кольцами и стеклянный столп
        BuildParts.Lathe(t, "Column", "Throne_KurColumn", new[] { new Vector2(0f, 0.9f), new Vector2(1.5f, 0.9f), new Vector2(1.5f, 1.15f), new Vector2(1.2f, 1.3f), new Vector2(1.0f, 1.5f),
            new Vector2(0.85f, 2.6f), new Vector2(1.15f, 2.75f), new Vector2(1.2f, 2.9f), new Vector2(0f, 2.9f) }, Vector3.zero, Vector3.one, "Kur_Stone", Marble, 0f, 0.5f, 32);
        foreach (float y in new[] { 1.55f, 2.0f, 2.4f })
            BuildParts.Torus(t, "ColumnRing", "Throne_KurColRing" + y, 0.95f - (y - 1.6f) * 0.04f, 0.05f, new Vector3(0, y, 0), Quaternion.identity, Vector3.one, "Kur_Trim", Gold, 0.85f, 0.7f);
        BuildParts.Lathe(t, "Glass", "Throne_KurGlass", new[] { new Vector2(0f, 2.9f), new Vector2(0.5f, 2.95f), new Vector2(0.6f, 3.3f), new Vector2(0.32f, 3.8f), new Vector2(0f, 3.95f) },
            Vector3.zero, Vector3.one, "Kur_Glass", new Color(0.55f, 0.75f, 1f), 0.2f, 0.95f, 24);

        // атом: ядро и три орбиты с электронами, всё вращается
        var atom = new GameObject("Atom").transform;
        atom.SetParent(t, false);
        atom.localPosition = new Vector3(0, 4.7f, 0);
        atom.localScale = Vector3.one * 0.8f;
        var spin = atom.gameObject.AddComponent<SpinFx>(); spin.speed = 25f; spin.bob = 0.15f; spin.bobSpeed = 1.1f;
        var core = Color.Lerp(team, Color.white, 0.35f);
        BuildParts.Glow(atom, "Nucleus", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.9f, "Throne_KurCore", core);
        BuildParts.Glow(atom, "NucleusHot", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.55f, "Throne_FlameCore", new Color(1f, 0.95f, 0.75f));
        var orbitMesh = BuildParts.Save("Throne_Orbit", ProcMesh.Torus(1.6f, 0.05f, 64, 6));
        for (int i = 0; i < 3; i++)
        {
            var ring = new GameObject("Orbit" + i).transform;
            ring.SetParent(atom, false);
            ring.localRotation = Quaternion.Euler(70f, i * 60f, 0f);
            ring.localScale = new Vector3(1f, 1f, 0.45f);
            BuildParts.GlowMesh(ring, "OrbitRing", orbitMesh, Vector3.zero, Quaternion.identity, Vector3.one, "Throne_Ring_Kur", team);
            var e = new GameObject("ElectronSpin").transform;
            e.SetParent(ring, false);
            var es = e.gameObject.AddComponent<SpinFx>(); es.speed = 140f + i * 40f; es.bob = 0f;
            BuildParts.Glow(e, "Electron", PrimitiveType.Sphere, new Vector3(1.6f, 0, 0), Vector3.one * 0.28f, "Throne_KurCore", core);
        }
        BuildParts.Ambient(atom, "Sparkle", Vector3.zero, AmbientFx.Kind.Sparkle, core, 0.25f, 1.2f, 2);
        BuildParts.Text(t, "КУРЧАТОВ", new Vector3(0, 1.25f, -1.6f), Gold, 0.06f, Quaternion.Euler(10, 0, 0));
    }

    // ---------- Москва: кремлёвская башня ----------
    public static void BuildKremlin(Transform t, Color team)
    {
        var brick = new Color(0.66f, 0.17f, 0.13f);
        var white = new Color(0.95f, 0.93f, 0.88f);
        var roof = new Color(0.16f, 0.42f, 0.28f);
        Pedestal(t, "Mos", new Color(0.82f, 0.8f, 0.76f), Gold, team);

        // ярусы башни: широкий кирпичный низ, белые карнизы, ярусы поуже
        BuildParts.Prim(t, "Tier1", PrimitiveType.Cube, new Vector3(0, 1.8f, 0), new Vector3(2.8f, 1.8f, 2.8f), "Mos_Brick", brick, 0f, 0.3f);
        BuildParts.Prim(t, "Cornice1", PrimitiveType.Cube, new Vector3(0, 2.75f, 0), new Vector3(3.05f, 0.18f, 3.05f), "Mos_White", white, 0f, 0.4f);
        for (int i = 0; i < 5; i++)   // зубцы-«ласточкины хвосты»
            foreach (float side in new[] { -1f, 1f })
            {
                float o = Mathf.Lerp(-1.2f, 1.2f, i / 4f);
                BuildParts.Prim(t, "Merlon", PrimitiveType.Cube, new Vector3(o, 3.0f, 1.4f * side), new Vector3(0.3f, 0.35f, 0.2f), "Mos_Brick", brick, 0f, 0.3f);
                BuildParts.Prim(t, "Merlon", PrimitiveType.Cube, new Vector3(1.4f * side, 3.0f, o), new Vector3(0.2f, 0.35f, 0.3f), "Mos_Brick", brick, 0f, 0.3f);
            }
        BuildParts.Prim(t, "Tier2", PrimitiveType.Cube, new Vector3(0, 3.35f, 0), new Vector3(1.8f, 1.0f, 1.8f), "Mos_Brick", brick, 0f, 0.3f);
        BuildParts.Prim(t, "Cornice2", PrimitiveType.Cube, new Vector3(0, 3.9f, 0), new Vector3(2.0f, 0.15f, 2.0f), "Mos_White", white, 0f, 0.4f);
        // часы на втором ярусе
        BuildParts.Lathe(t, "ClockFace", "Throne_Clock", new[] { new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.4f, 0.06f), new Vector2(0f, 0.06f) },
            new Vector3(0, 3.35f, -0.91f), Vector3.one, "Mos_White", white, 0f, 0.4f, 28, Quaternion.Euler(90, 0, 0));
        BuildParts.Torus(t, "ClockRim", "Throne_ClockRim", 0.4f, 0.04f, new Vector3(0, 3.35f, -0.96f), Quaternion.Euler(90, 0, 0), Vector3.one, "Mos_Trim", Gold, 0.85f, 0.7f);
        BuildParts.Prim(t, "HourHand", PrimitiveType.Cube, new Vector3(0.08f, 3.42f, -0.98f), new Vector3(0.04f, 0.2f, 0.02f), "Mos_Dark", new Color(0.1f, 0.1f, 0.1f), 0f, 0.4f, Quaternion.Euler(0, 0, -40));
        BuildParts.Prim(t, "MinuteHand", PrimitiveType.Cube, new Vector3(0, 3.5f, -0.98f), new Vector3(0.03f, 0.3f, 0.02f), "Mos_Dark", new Color(0.1f, 0.1f, 0.1f), 0f, 0.4f);
        // окна-бойницы с тёплым светом
        foreach (float x in new[] { -0.75f, 0.75f })
            BuildParts.Glow(t, "Loophole", PrimitiveType.Cube, new Vector3(x, 1.9f, -1.41f), new Vector3(0.3f, 0.6f, 0.04f), "Tower_Window", new Color(1f, 0.82f, 0.45f));
        // зелёный шатёр (четырёхгранник) с белыми рёбрами
        BuildParts.Lathe(t, "Spire", "Throne_Spire", new[] { new Vector2(0f, 3.95f), new Vector2(1.35f, 3.95f), new Vector2(0.9f, 4.5f), new Vector2(0.12f, 5.7f), new Vector2(0f, 5.8f) },
            Vector3.zero, Vector3.one, "Mos_Roof", roof, 0.2f, 0.65f, 4, Quaternion.Euler(0, 45f, 0));
        // светящаяся рубиновая звезда, медленно вращается
        var star = new GameObject("Star").transform;
        star.SetParent(t, false);
        star.localPosition = new Vector3(0, 6.25f, 0);
        star.localScale = Vector3.one * 0.85f;
        var spin = star.gameObject.AddComponent<SpinFx>(); spin.speed = 30f; spin.bob = 0.08f;
        var ruby = Color.Lerp(team, new Color(1f, 0.1f, 0.1f), 0.5f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f;
            BuildParts.Glow(star, "Ray", PrimitiveType.Capsule, Quaternion.Euler(0, 0, a) * new Vector3(0, 0.3f, 0), new Vector3(0.22f, 0.32f, 0.1f), "Throne_Star", ruby, Quaternion.Euler(0, 0, a));
        }
        BuildParts.Glow(star, "StarCore", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.38f, 0.38f, 0.14f), "Throne_FlameCore", new Color(1f, 0.85f, 0.75f));
        BuildParts.Ambient(star, "Sparkle", Vector3.zero, AmbientFx.Kind.Sparkle, ruby, 0.3f, 0.6f, 1);
        BuildParts.Text(t, "МОСКВА", new Vector3(0, 1.25f, -1.45f), Gold, 0.06f, Quaternion.Euler(10, 0, 0));
    }
}

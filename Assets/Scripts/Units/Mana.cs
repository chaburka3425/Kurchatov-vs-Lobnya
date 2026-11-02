using UnityEngine;

// Мана для способностей.
public class Mana : MonoBehaviour
{
    public float maxMana = 300f;
    public float regenPerSecond = 1f;

    public float Current { get; private set; }

    void Awake() => Current = maxMana;

    void Update()
    {
        if (Current < maxMana)
            Current = Mathf.Min(maxMana, Current + regenPerSecond * Time.deltaTime);
    }

    public void SetMax(float max, bool refill)
    {
        maxMana = max;
        Current = refill ? max : Mathf.Min(Current, max);
    }

    public bool TrySpend(float amount)
    {
        if (Current < amount) return false;
        Current -= amount;
        return true;
    }

    public void Refill() => Current = maxMana;

    public void SetMaxKeepRatio(float max)
    {
        float ratio = maxMana > 0f ? Current / maxMana : 1f;
        maxMana = max;
        Current = max * ratio;
    }

    public void Restore(float amount) => Current = Mathf.Min(maxMana, Current + amount);
}

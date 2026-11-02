using UnityEngine;

// Крутит объект вокруг своей оси и чуть покачивает (души-сосиски вокруг Сани).
public class SpinFx : MonoBehaviour
{
    public float speed = 60f;     // градусов в секунду
    public float bob = 0.12f;     // покачивание вверх-вниз
    public float bobSpeed = 1.6f;

    Vector3 basePos;

    void Start() => basePos = transform.localPosition;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bob;
    }
}

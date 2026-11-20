using UnityEngine;

// Keeps the 1080 px wide room fully visible on any phone, and adds screen shake.
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public float worldWidth = 10.8f;   // room width in world units
    public float minHeight = 19.2f;    // never show less than this height

    Camera cam;
    Vector3 basePosition;
    float shakeTime;
    float shakeStrength;

    void Awake()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.position;
        Fit();
    }

    void Fit()
    {
        float sizeForWidth = worldWidth / 2f / cam.aspect;
        cam.orthographicSize = Mathf.Max(sizeForWidth, minHeight / 2f);
    }

    public void Shake(float duration, float strength)
    {
        shakeTime = duration;
        shakeStrength = strength;
    }

    void LateUpdate()
    {
        Fit();
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeStrength;
            transform.position = basePosition + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            transform.position = basePosition;
        }
    }
}

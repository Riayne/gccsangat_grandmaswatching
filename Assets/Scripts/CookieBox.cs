using UnityEngine;

// Wobbles the lid while the bacha eats and bounces the box for each cookie.
public class CookieBox : MonoBehaviour
{
    public Transform box;
    public Transform lid;

    public bool IsEating { get; set; }

    Vector3 boxScale;
    float bounce;

    void Awake()
    {
        boxScale = box.localScale;
    }

    public void Bounce()
    {
        bounce = 1f;
    }

    void Update()
    {
        float angle = IsEating ? Mathf.Sin(Time.time * 28f) * 4f : 0f;
        lid.localRotation = Quaternion.Euler(0f, 0f, angle);

        bounce = Mathf.MoveTowards(bounce, 0f, Time.deltaTime * 8f);
        box.localScale = boxScale * (1f + bounce * 0.04f);
    }
}

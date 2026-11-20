using UnityEngine;

// Wobbles the lid while the bacha eats, bounces the box for each cookie,
// and shows the golden cookie for a short time.
public class CookieBox : MonoBehaviour
{
    public Transform box;
    public Transform lid;
    public Transform golden;

    public bool IsEating { get; set; }
    public bool GoldenActive => golden.gameObject.activeSelf;

    Vector3 boxScale;
    Vector3 goldenScale;
    float bounce;
    float goldenTimeLeft;

    void Awake()
    {
        boxScale = box.localScale;
        goldenScale = golden.localScale;
        golden.gameObject.SetActive(false);
    }

    public void Bounce()
    {
        bounce = 1f;
    }

    public void ShowGolden(float seconds)
    {
        goldenTimeLeft = seconds;
        golden.gameObject.SetActive(true);
    }

    public void HideGolden()
    {
        goldenTimeLeft = 0f;
        golden.gameObject.SetActive(false);
    }

    void Update()
    {
        float angle = IsEating ? Mathf.Sin(Time.time * 28f) * 4f : 0f;
        lid.localRotation = Quaternion.Euler(0f, 0f, angle);

        bounce = Mathf.MoveTowards(bounce, 0f, Time.deltaTime * 8f);
        box.localScale = boxScale * (1f + bounce * 0.04f);

        if (GoldenActive)
        {
            goldenTimeLeft -= Time.deltaTime;
            float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.08f;
            golden.localScale = goldenScale * pulse;
            golden.Rotate(0f, 0f, 60f * Time.deltaTime);
            if (goldenTimeLeft <= 0f) HideGolden();
        }
    }
}

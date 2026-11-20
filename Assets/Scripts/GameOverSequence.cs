using System.Collections;
using UnityEngine;

// Dadi throws her chappal: it spins toward the camera until it fills the screen,
// the "glass" cracks, the phone vibrates and the camera shakes.
public class GameOverSequence : MonoBehaviour
{
    public Camera cam;
    public CameraFit cameraFit;
    public AudioManager audioManager;
    public Transform chappal;
    public Transform crack;
    public float flightTime = 0.7f;

    public IEnumerator Play(Vector3 from)
    {
        yield return new WaitForSeconds(0.5f);   // let the player read Dadi's line

        audioManager.Play(audioManager.whoosh);
        chappal.gameObject.SetActive(true);
        Vector3 target = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        float coverScale = cam.orthographicSize * 2f / 3.2f * 1.3f;   // chappal sprite is 3.2 units tall
        from.y += 2f;

        float t = 0f;
        while (t < flightTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flightTime);
            float ease = k * k;
            chappal.position = Vector3.Lerp(from, target, ease);
            chappal.localScale = Vector3.one * Mathf.Lerp(0.6f, coverScale, ease);
            chappal.Rotate(0f, 0f, 1000f * Time.deltaTime);
            yield return null;
        }
        chappal.rotation = Quaternion.Euler(0f, 0f, 12f);

        // Smash!
        audioManager.Play(audioManager.glassBreak);
        crack.gameObject.SetActive(true);
        crack.position = target;
        float crackScale = Mathf.Max(cam.orthographicSize * 2f / 19.2f, cam.orthographicSize * 2f * cam.aspect / 10.8f);
        crack.localScale = Vector3.one * crackScale;
        cameraFit.Shake(0.45f, 0.35f);
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        yield return new WaitForSeconds(0.9f);
    }
}

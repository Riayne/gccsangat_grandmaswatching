using UnityEngine;

// One place Dadi can come from (a door, the window, or behind the sofa).
public class SpawnPoint : MonoBehaviour
{
    public enum Kind { Door, Window, Sofa }

    public Kind kind = Kind.Door;
    [Tooltip("Stereo pan for the footsteps: -1 = left speaker, 1 = right speaker.")]
    public float pan = 0f;
    [Tooltip("First night Dadi can use this spawn.")]
    public int unlockNight = 1;
    [Tooltip("Area the player taps (world units) to close this door / jump out of the window.")]
    public Rect tapArea;

    public GameObject dadi;           // Dadi's sprite for this spawn (hidden until she watches)
    public GameObject warningVisual;  // light / shadow strip shown during the warning
    public GameObject doorPanel;      // closed-door sprite (doors only)
    public GameObject chotu;          // little brother peeking in (doors only)

    public bool IsDoorClosed => doorPanel != null && doorPanel.activeSelf;

    public bool Contains(Vector2 worldPoint)
    {
        return tapArea.Contains(worldPoint);
    }

    // Point above a character's head, used to place a speech bubble.
    public Vector3 BubblePoint(GameObject who = null)
    {
        var sr = (who != null ? who : dadi).GetComponent<SpriteRenderer>();
        Bounds b = sr.bounds;
        return new Vector3(b.center.x, b.max.y + 0.2f, 0f);
    }

    public void SetDoorClosed(bool closed)
    {
        if (doorPanel != null) doorPanel.SetActive(closed);
    }

    public void HideAll()
    {
        dadi.SetActive(false);
        warningVisual.SetActive(false);
        if (chotu != null) chotu.SetActive(false);
        SetDoorClosed(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(tapArea.center, tapArea.size);
    }
}

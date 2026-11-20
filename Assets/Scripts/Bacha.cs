using UnityEngine;

// The boy. Shows the right sprite for his state, chews while eating and fidgets while hiding.
public class Bacha : MonoBehaviour
{
    public enum State { Eating, Hiding, Away }

    public GameObject eatingSprite;
    public GameObject hidingSprite;

    public State Current { get; private set; } = State.Hiding;

    Vector3 eatingScale;
    Vector3 hidingPosition;

    void Awake()
    {
        eatingScale = eatingSprite.transform.localScale;
        hidingPosition = hidingSprite.transform.localPosition;
        SetState(State.Hiding);
    }

    public void SetState(State state)
    {
        Current = state;
        eatingSprite.SetActive(state == State.Eating);
        hidingSprite.SetActive(state == State.Hiding);
    }

    void Update()
    {
        if (Current == State.Eating)
        {
            // Chewing: a quick little squash and stretch.
            float chew = Mathf.Sin(Time.time * 20f) * 0.02f;
            eatingSprite.transform.localScale = new Vector3(eatingScale.x * (1f - chew), eatingScale.y * (1f + chew), 1f);
        }
        else if (Current == State.Hiding)
        {
            // Hair tuft bobs nervously.
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 0.06f;
            hidingSprite.transform.localPosition = hidingPosition + new Vector3(0f, bob, 0f);
        }
    }
}

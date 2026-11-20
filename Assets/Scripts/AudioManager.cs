using UnityEngine;

// Plays every sound in the game. Footsteps get their own source so they can be panned left/right.
public class AudioManager : MonoBehaviour
{
    public AudioSource ambientSource;
    public AudioSource sfxSource;
    public AudioSource stepSource;

    public AudioClip ambient;
    public AudioClip footstep;
    public AudioClip crunch;
    public AudioClip knock;
    public AudioClip doorClose;
    public AudioClip sting;
    public AudioClip whoosh;
    public AudioClip glassBreak;
    public AudioClip jump;
    public AudioClip praise;
    public AudioClip cough;
    public AudioClip giggle;
    public AudioClip upsBeep;
    public AudioClip golden;
    public AudioClip nightStart;
    public AudioClip powerOff;

    const string SoundKey = "SoundOn";

    public bool SoundOn => AudioListener.volume > 0f;

    void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetInt(SoundKey, 1) == 1 ? 1f : 0f;
    }

    void Start()
    {
        ambientSource.clip = ambient;
        ambientSource.loop = true;
        ambientSource.volume = 0.5f;
        ambientSource.Play();
    }

    public void ToggleSound()
    {
        AudioListener.volume = SoundOn ? 0f : 1f;
        PlayerPrefs.SetInt(SoundKey, SoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Quieter ambience during load-shedding (the fan stops!).
    public void SetFanOn(bool on)
    {
        ambientSource.volume = on ? 0.5f : 0.12f;
    }

    public void Footstep(float pan, float volume)
    {
        stepSource.panStereo = pan;
        stepSource.pitch = Random.Range(0.9f, 1.1f);
        stepSource.PlayOneShot(footstep, volume);
        Haptics.Pulse(15);
    }

    public void Crunch(float pitch = 1f)
    {
        sfxSource.pitch = Random.Range(0.85f, 1.2f) * pitch;
        sfxSource.PlayOneShot(crunch, 0.6f);
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayPanned(AudioClip clip, float pan, float volume = 1f)
    {
        stepSource.panStereo = pan;
        stepSource.pitch = 1f;
        stepSource.PlayOneShot(clip, volume);
    }
}

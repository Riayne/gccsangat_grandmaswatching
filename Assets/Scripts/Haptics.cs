using UnityEngine;

// Short phone vibrations (Android). Handheld.Vibrate() is too long for footsteps, so this calls Android directly.
public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
#endif

    public static void Pulse(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (vibrator == null)
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            vibrator?.Call("vibrate", milliseconds);
        }
        catch (System.Exception)
        {
            // No vibrator on this device: ignore.
        }
#endif
    }

    public static void Long()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}

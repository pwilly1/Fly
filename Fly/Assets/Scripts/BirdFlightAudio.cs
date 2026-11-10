using UnityEngine;

[DisallowMultipleComponent]
public class BirdFlightAudio : MonoBehaviour
{
    private const string FlapPath = "BirdSounds/WingFlap";
    private const string WindPath = "BirdSounds/FlightWind";

    private AudioSource flapSource;
    private AudioSource windSource;
    private AudioClip flapClip;

    private void Awake()
    {
        flapClip = Resources.Load<AudioClip>(FlapPath);
        AudioClip windClip = Resources.Load<AudioClip>(WindPath);

        flapSource = gameObject.AddComponent<AudioSource>();
        flapSource.playOnAwake = false;
        flapSource.spatialBlend = 0f;

        windSource = gameObject.AddComponent<AudioSource>();
        windSource.playOnAwake = false;
        windSource.spatialBlend = 0f;
        windSource.loop = true;
        windSource.clip = windClip;
        windSource.volume = 0f;
        if (windClip != null) windSource.Play();

        if (flapClip == null || windClip == null)
            Debug.LogWarning("Bird flight audio clips are missing from Resources/BirdSounds.", this);
    }

    public void PlayFlap()
    {
        if (flapClip != null) flapSource.PlayOneShot(flapClip, 0.48f);
    }

    public void UpdateWind(float speed, float cruiseSpeed, float diveSpeedBonus)
    {
        if (windSource.clip == null) return;
        float speedAmount = Mathf.InverseLerp(cruiseSpeed - 3f,
            cruiseSpeed + diveSpeedBonus, speed);
        float response = 1f - Mathf.Exp(-3f * Time.deltaTime);
        windSource.volume = Mathf.Lerp(windSource.volume,
            Mathf.Lerp(0.04f, 0.18f, speedAmount), response);
        windSource.pitch = Mathf.Lerp(windSource.pitch,
            Mathf.Lerp(0.82f, 1.16f, speedAmount), response);
    }

    public void ResumeWind()
    {
        if (windSource.clip != null && !windSource.isPlaying) windSource.Play();
    }

    public void StopWind()
    {
        if (windSource != null) windSource.Stop();
    }
}

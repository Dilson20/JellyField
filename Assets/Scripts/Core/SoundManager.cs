using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Clips — drag AudioClips here")]
    public AudioClip clipPop;       // played when a quadrant disappears
    public AudioClip clipPlace;     // tile placed on grid
    public AudioClip clipPickup;    // tile picked up
    public AudioClip clipMerge;     // full merge / promote to full tile

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource _source;

    void Awake()
    {
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    // colorIndex 0-4 → slight pitch variation so each color sounds distinct
    public void PlayPop(int colorIndex = -1)
    {
        Play(clipPop, pitchVariation: true, colorIndex);
    }

    public void PlayPlace() => Play(clipPlace);
    public void PlayPickup() => Play(clipPickup);
    public void PlayMerge() => Play(clipMerge, pitchVariation: true);

    void Play(AudioClip clip, bool pitchVariation = false, int colorIndex = -1)
    {
        if (clip == null || _source == null) return;

        float pitch = 1f;
        if (pitchVariation)
        {
            // Each color gets a slightly different pitch (+0 to +0.4 semitones per index)
            float colorOffset = colorIndex >= 0 ? colorIndex * 0.08f : 0f;
            pitch = Random.Range(0.92f, 1.08f) + colorOffset;
        }

        _source.pitch = pitch;
        _source.PlayOneShot(clip, masterVolume * sfxVolume);
    }
}
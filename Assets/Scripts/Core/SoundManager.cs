using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Clips — drag AudioClips here")]
    public AudioClip clipPop;
    public AudioClip clipPlace;
    public AudioClip clipPickup;
    public AudioClip clipMerge;

    [Header("Enable / Disable per action")]
    public bool enablePop = true;
    public bool enablePlace = true;
    public bool enablePickup = true;
    public bool enableMerge = true;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource _source;

    void Awake()
    {
        Instance = this;
        _source = GetComponent<AudioSource>();  // ← uses existing AudioSource on same GO
        if (_source == null)
            Debug.LogWarning("SoundManager: No AudioSource found on this GameObject. Please add one.");
        else
            _source.playOnAwake = false;
    }

    public void PlayPop(int colorIndex = -1)
    {
        if (!enablePop) return;
        Play(clipPop, pitchVariation: true, colorIndex);
    }

    public void PlayPlace()
    {
        if (!enablePlace) return;
        Play(clipPlace);
    }

    public void PlayPickup()
    {
        if (!enablePickup) return;
        Play(clipPickup);
    }

    public void PlayMerge()
    {
        if (!enableMerge) return;
        Play(clipMerge, pitchVariation: true);
    }

    void Play(AudioClip clip, bool pitchVariation = false, int colorIndex = -1)
    {
        if (clip == null || _source == null) return;

        float pitch = 1f;
        if (pitchVariation)
        {
            float colorOffset = colorIndex >= 0 ? colorIndex * 0.08f : 0f;
            pitch = Random.Range(0.92f, 1.08f) + colorOffset;
        }

        _source.pitch = pitch;
        _source.PlayOneShot(clip, masterVolume * sfxVolume);
    }
}
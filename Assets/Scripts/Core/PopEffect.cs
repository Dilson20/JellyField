using System.Collections;
using UnityEngine;

public class PopEffect : MonoBehaviour
{
    // Call this from anywhere to spawn a pop at world position with a given color
    public static void Spawn(Vector3 worldPos, Color color, Sprite sprite = null)
    {
        var go = new GameObject("PopEffect");
        go.transform.position = worldPos;
        go.AddComponent<PopEffect>().Play(color, sprite);
    }

    void Play(Color color, Sprite sprite)
    {
        StartCoroutine(DoEffect(color, sprite));
    }

    IEnumerator DoEffect(Color color, Sprite sprite)
    {
        int count = 6;
        float duration = 0.35f;
        float spread = 0.28f;

        var particles = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            var dot = new GameObject($"dot_{i}");
            dot.transform.SetParent(transform);
            dot.transform.localPosition = Vector3.zero;

            var sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;
            sr.color = color;
            dot.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);
            particles[i] = dot.transform;
        }

        float elapsed = 0f;
        float[] angles = new float[count];
        float baseAngle = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
            angles[i] = baseAngle + (360f / count) * i + Random.Range(-15f, 15f);

        float[] speeds = new float[count];
        for (int i = 0; i < count; i++)
            speeds[i] = Random.Range(0.7f, 1.3f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 2f);
            float fade = 1f - Mathf.Pow(t, 1.5f);

            for (int i = 0; i < count; i++)
            {
                if (particles[i] == null) continue;
                float rad = angles[i] * Mathf.Deg2Rad;
                float dist = easeOut * spread * speeds[i];
                particles[i].localPosition = new Vector3(
                    Mathf.Cos(rad) * dist,
                    Mathf.Sin(rad) * dist, 0);

                float s = Mathf.Lerp(0.14f, 0.03f, t) * speeds[i];
                particles[i].localScale = Vector3.one * s;

                var sr = particles[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = new Color(color.r, color.g, color.b, fade);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
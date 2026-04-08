using System.Collections;
using UnityEngine;

public class JiggleEffect : MonoBehaviour
{
    private Vector3 baseScale;
    private Coroutine jiggleCoroutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
    }

    public void PlayPickup()
    {
        StopJiggle();
        jiggleCoroutine = StartCoroutine(PickupJiggle());
    }

    public void PlayDrop()
    {
        StopJiggle();
        jiggleCoroutine = StartCoroutine(DropJiggle());
    }

    public void PlaySwap()
    {
        StopJiggle();
        jiggleCoroutine = StartCoroutine(SwapJiggle());
    }

    public void StopJiggle()
    {
        if (jiggleCoroutine != null)
            StopCoroutine(jiggleCoroutine);
        transform.localScale = baseScale;
    }

    public void PlayDrag()
    {
        StopJiggle();
        jiggleCoroutine = StartCoroutine(DragJiggle());
    }

    public void PlayIdle()
    {
        StopJiggle();
        jiggleCoroutine = StartCoroutine(IdleShake());
    }

    // Continuous jiggle while being dragged
    IEnumerator DragJiggle()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 8f;
            float wobbleX = Mathf.Sin(t) * 0.06f;
            float wobbleY = Mathf.Cos(t * 1.3f) * 0.06f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + wobbleX),
                baseScale.y * (1f + wobbleY),
                1f);
            yield return null;
        }
    }

    // Gentle idle shake when not moving
    IEnumerator IdleShake()
    {
        float t = Random.Range(0f, 10f); // random offset so tiles don't sync
        while (true)
        {
            t += Time.deltaTime * 2.5f;
            float wobble = Mathf.Sin(t) * 0.03f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + wobble),
                baseScale.y * (1f - wobble),
                1f);
            yield return null;
        }
    }

    // Squash down then spring back up
    IEnumerator PickupJiggle()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.12f;
            float squash = 1f + Mathf.Sin(t * Mathf.PI) * 0.07f;
            float stretch = 1f - Mathf.Sin(t * Mathf.PI) * 0.05f;
            transform.localScale = new Vector3(
                baseScale.x * squash,
                baseScale.y * stretch,
                1f);
            yield return null;
        }
        transform.localScale = baseScale;
    }

    // Wobble on landing
    IEnumerator DropJiggle()
    {
        float duration = 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float wobble = Mathf.Sin(progress * Mathf.PI * 4f)
                           * (1f - progress) * 0.06f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + wobble),
                baseScale.y * (1f - wobble),
                1f);
            yield return null;
        }
        transform.localScale = baseScale;
    }

    // Quick squeeze on swap
    IEnumerator SwapJiggle()
    {
        float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float squeeze = Mathf.Sin(progress * Mathf.PI * 3f)
                            * (1f - progress) * 0.06f;
            transform.localScale = new Vector3(
                baseScale.x * (1f - squeeze),
                baseScale.y * (1f + squeeze),
                1f);
            yield return null;
        }
        transform.localScale = baseScale;
    }
}
using NonStopHallsGauntlet;
using UnityEngine;

public class Fader : MonoBehaviour
{
    public float lifetime = 10f;
    public float fadeDuration = 2f;

    SpriteRenderer[] renderers;
    float timer;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }
    void Start()
    {
        timer = lifetime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= fadeDuration)
        {
            float t = Mathf.Clamp01(timer / fadeDuration);
            SetAlpha(t);
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void SetAlpha(float alpha)
    {
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
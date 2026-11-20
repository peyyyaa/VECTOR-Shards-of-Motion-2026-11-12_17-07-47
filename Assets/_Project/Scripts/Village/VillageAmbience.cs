using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Village life for one screen (purely visual, no physics): small flocks of birds cross the sky now and then,
/// lamp lights flicker softly, grass and flowers sway in the breeze, pale pollen drifts through the air,
/// chimneys smoke, the forge hearth throws embers, and the restored shard crystal sheds motes.
/// Smoke / embers / motes come from marker transforms; a marker only emits while it is active in the hierarchy
/// (so motes stop while the shrine is still broken).
/// Added by Tools > Shards of Motion > Game Shell > Build Village (on "Ambience_Visual" in each village room).
/// The bird sprite is drawn in code (two flap frames), so it needs no asset.
/// </summary>
public class VillageAmbience : MonoBehaviour
{
    [SerializeField] private FxLibrary fx;
    [SerializeField] private Light2D[] lamps = new Light2D[0];
    [SerializeField] private SpriteRenderer[] sway = new SpriteRenderer[0];
    [Tooltip("Seconds between flocks (random in this range).")]
    [SerializeField] private Vector2 birdInterval = new Vector2(6f, 14f);
    [SerializeField] private float pollenPerSecond = 1.2f;
    [SerializeField] private Transform[] smoke = new Transform[0];
    [SerializeField] private Transform[] embers = new Transform[0];
    [SerializeField] private Transform[] motes = new Transform[0];

    private const float HalfWidth = 10f;
    private static Sprite[] birdFrames;

    private float[] lampBase;
    private Vector3[] swayHome, swayFoot;
    private float birdTimer, pollenTimer, smokeTimer, emberTimer, moteTimer;

    private void Start()
    {
        lampBase = new float[lamps.Length];
        for (int i = 0; i < lamps.Length; i++) if (lamps[i] != null) lampBase[i] = lamps[i].intensity;
        swayHome = new Vector3[sway.Length];
        swayFoot = new Vector3[sway.Length];
        for (int i = 0; i < sway.Length; i++)
        {
            if (sway[i] == null) continue;
            swayHome[i] = sway[i].transform.position;
            Bounds b = sway[i].bounds;
            swayFoot[i] = new Vector3(b.center.x, b.min.y, 0f); // sway around the stem's base
        }
        birdTimer = Random.Range(1f, birdInterval.x);
    }

    private void Update()
    {
        float t = Time.time;
        for (int i = 0; i < lamps.Length; i++)
            if (lamps[i] != null)
                lamps[i].intensity = lampBase[i] * (0.92f + 0.08f * Mathf.PerlinNoise(t * 1.5f, i * 7.3f) + 0.03f * Mathf.Sin(t * 9f + i));

        for (int i = 0; i < sway.Length; i++)
        {
            if (sway[i] == null) continue;
            // A gust that travels across the screen: neighbours sway slightly out of step.
            float angle = 3f * Mathf.Sin(t * 1.3f - swayHome[i].x * 0.4f) + 1.5f * Mathf.Sin(t * 2.9f + i);
            Quaternion q = Quaternion.Euler(0f, 0f, angle);
            sway[i].transform.SetPositionAndRotation(swayFoot[i] + q * (swayHome[i] - swayFoot[i]), q);
        }

        birdTimer -= Time.deltaTime;
        if (birdTimer <= 0f)
        {
            birdTimer = Random.Range(birdInterval.x, birdInterval.y);
            Flock();
        }

        if (fx == null || fx.dot == null) return;
        Emitters();
        pollenTimer -= Time.deltaTime;
        if (pollenTimer > 0f) return;
        pollenTimer = 1f / pollenPerSecond;
        Vector3 at = transform.position + new Vector3(Random.Range(-HalfWidth, HalfWidth), Random.Range(-4f, 1.5f), 0f);
        Fx.Sprite(fx.dot, at, new Color(1f, 0.95f, 0.75f, 0.55f), Random.Range(4f, 6f), 2).FadeInOut()
          .Move(new Vector2(Random.Range(0.15f, 0.45f), Random.Range(-0.05f, 0.15f)));
    }

    private void Emitters()
    {
        float dt = Time.deltaTime;
        if ((smokeTimer -= dt) <= 0f)
        {
            smokeTimer = 0.2f;
            foreach (Transform s in smoke)
                if (s != null && s.gameObject.activeInHierarchy)
                    Fx.Sprite(fx.dot, s.position + new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f), new Color(0.85f, 0.83f, 0.8f, 0.7f), Random.Range(2.5f, 3.5f), -1)
                      .FadeInOut().Grow(2f, 5f).Move(new Vector2(Random.Range(0.1f, 0.3f), Random.Range(0.45f, 0.65f)));
        }
        if ((emberTimer -= dt) <= 0f)
        {
            emberTimer = 0.25f;
            foreach (Transform e in embers)
                if (e != null && e.gameObject.activeInHierarchy && fx.spark != null)
                    Fx.Sprite(fx.spark, e.position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f), new Color(1f, 0.6f, 0.25f), Random.Range(0.6f, 1.1f), 2)
                      .Move(new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1.1f)));
        }
        if ((moteTimer -= dt) <= 0f)
        {
            moteTimer = 0.3f;
            foreach (Transform m in motes)
                if (m != null && m.gameObject.activeInHierarchy)
                    Fx.Sprite(fx.dot, m.position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.6f, 0.4f), 0f), new Color(0.55f, 0.95f, 1f, 0.9f), Random.Range(1.5f, 2.5f), 2)
                      .FadeInOut().Move(new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.3f, 0.6f)));
        }
    }

    // 1 to 3 birds crossing high up, in a loose V, left-to-right or right-to-left.
    private void Flock()
    {
        int count = Random.Range(1, 4);
        bool toRight = Random.value < 0.5f;
        float speed = Random.Range(2.5f, 4f);
        float y = Random.Range(2f, 4.6f);
        for (int i = 0; i < count; i++)
        {
            Vector3 start = transform.position + new Vector3(toRight ? -HalfWidth - 1f - i * 0.7f : HalfWidth + 1f + i * 0.7f, y - i * 0.35f, 0f);
            GameObject b = new GameObject("Bird");
            b.transform.position = start;
            b.AddComponent<Bird>().Setup(Frames(), toRight ? speed : -speed, (2f * HalfWidth + 4f + i * 0.7f) / speed);
        }
    }

    private static Sprite[] Frames()
    {
        if (birdFrames != null && birdFrames[0] != null) return birdFrames;
        string[][] art =
        {
            new[] { "#...#", ".#.#.", "..#.." }, // wings up
            new[] { ".....", "#####", "..#.." }, // wings level
        };
        birdFrames = new Sprite[art.Length];
        Color ink = new Color(0.17f, 0.12f, 0.11f, 1f);
        for (int f = 0; f < art.Length; f++)
        {
            Texture2D tex = new Texture2D(5, 3) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 5; x++)
                    tex.SetPixel(x, 2 - y, art[f][y][x] == '#' ? ink : Color.clear);
            tex.Apply();
            birdFrames[f] = Sprite.Create(tex, new Rect(0, 0, 5, 3), new Vector2(0.5f, 0.5f), 32f);
        }
        return birdFrames;
    }

    private class Bird : MonoBehaviour
    {
        private Sprite[] frames;
        private SpriteRenderer sr;
        private float vx, life, phase;
        private Vector3 pos;

        public void Setup(Sprite[] f, float speed, float lifetime)
        {
            frames = f; vx = speed; life = lifetime; phase = Random.value * 3f;
            pos = transform.position;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingLayerName = "BackDecor";
            sr.sortingOrder = 20;
            sr.color = new Color(1f, 1f, 1f, 0.85f);
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            pos.x += vx * Time.deltaTime;
            float t = Time.time + phase;
            sr.sprite = frames[(int)(t * 6f) % frames.Length];
            transform.position = Fx.Snap(pos + Vector3.up * (Mathf.Sin(t * 2f) * 0.15f));
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(FxLibrary library, Light2D[] lampLights, SpriteRenderer[] swaying, Transform[] smokePoints, Transform[] emberPoints, Transform[] motePoints)
    {
        fx = library; lamps = lampLights; sway = swaying; smoke = smokePoints; embers = emberPoints; motes = motePoints;
    }
#endif
}

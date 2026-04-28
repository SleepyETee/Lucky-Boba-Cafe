// ============================================================
// FILE: DayNightCycle.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Visual day/night cycle for 2D cafe game.
//              Smoothly transitions ambient color, overlay tint,
//              window glow, and shadow opacity across day periods.
//              Works without URP — uses a fullscreen sprite overlay
//              and camera background color for lighting effects.
// ============================================================
using UnityEngine;
using System;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    // ==================== PERIOD DEFINITION ====================

    public enum TimePeriod
    {
        EarlyMorning,   // 6:00 - 8:00   dawn, warm gold
        Morning,        // 8:00 - 11:00  bright white
        LunchRush,      // 11:00 - 14:00 warm midday
        Afternoon,      // 14:00 - 17:00 golden hour starts
        Evening,        // 17:00 - 18:30 orange sunset
        Closing          // 18:30 - 19:00 deep blue twilight
    }

    [Serializable]
    public class PeriodLighting
    {
        public TimePeriod period;
        [Tooltip("Game hour this period starts (6-19)")]
        public float startHour;
        [Tooltip("Ambient tint applied to camera overlay")]
        public Color ambientColor = Color.white;
        [Tooltip("Overlay opacity (0 = clear, 0.3 = noticeable tint)")]
        [Range(0f, 0.6f)] public float overlayAlpha = 0f;
        [Tooltip("Intensity of window glow sprites (0 = off, 1 = full)")]
        [Range(0f, 1f)] public float windowGlowIntensity = 0f;
        [Tooltip("Shadow sprite opacity")]
        [Range(0f, 1f)] public float shadowAlpha = 0.3f;
        [Tooltip("Shadow X offset (simulates sun angle)")]
        public float shadowOffsetX = 0.5f;
    }

    [Header("Period Lighting Presets")]
    [SerializeField] private PeriodLighting[] periodPresets = new PeriodLighting[]
    {
        new PeriodLighting {
            period = TimePeriod.EarlyMorning, startHour = 6f,
            ambientColor = new Color(1f, 0.85f, 0.65f),    // warm dawn gold
            overlayAlpha = 0.12f, windowGlowIntensity = 0.3f,
            shadowAlpha = 0.15f, shadowOffsetX = -1.2f
        },
        new PeriodLighting {
            period = TimePeriod.Morning, startHour = 8f,
            ambientColor = new Color(1f, 0.98f, 0.95f),    // bright daylight
            overlayAlpha = 0f, windowGlowIntensity = 0f,
            shadowAlpha = 0.25f, shadowOffsetX = -0.6f
        },
        new PeriodLighting {
            period = TimePeriod.LunchRush, startHour = 11f,
            ambientColor = new Color(1f, 1f, 0.95f),       // bright warm noon
            overlayAlpha = 0f, windowGlowIntensity = 0f,
            shadowAlpha = 0.35f, shadowOffsetX = 0f
        },
        new PeriodLighting {
            period = TimePeriod.Afternoon, startHour = 14f,
            ambientColor = new Color(1f, 0.93f, 0.8f),     // golden hour
            overlayAlpha = 0.05f, windowGlowIntensity = 0.1f,
            shadowAlpha = 0.3f, shadowOffsetX = 0.6f
        },
        new PeriodLighting {
            period = TimePeriod.Evening, startHour = 17f,
            ambientColor = new Color(1f, 0.7f, 0.45f),     // orange sunset
            overlayAlpha = 0.15f, windowGlowIntensity = 0.7f,
            shadowAlpha = 0.2f, shadowOffsetX = 1.2f
        },
        new PeriodLighting {
            period = TimePeriod.Closing, startHour = 18.5f,
            ambientColor = new Color(0.55f, 0.55f, 0.85f), // blue twilight
            overlayAlpha = 0.25f, windowGlowIntensity = 1f,
            shadowAlpha = 0.1f, shadowOffsetX = 1.5f
        },
    };

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 1.5f;

    [Header("References (auto-created if not assigned)")]
    [SerializeField] private SpriteRenderer overlayRenderer;
    [SerializeField] private SpriteRenderer[] windowGlowSprites;
    [SerializeField] private SpriteRenderer[] shadowSprites;

    [Header("Overlay Settings")]
    [SerializeField] private int overlaySortingOrder = 999;
    #pragma warning disable CS0414
    [SerializeField] private string overlaySortingLayer = "Default";
    #pragma warning restore CS0414

    // Runtime state
    private float currentGameHour = 6f;
    private PeriodLighting currentTarget;
    private PeriodLighting previousTarget;
    private float transitionProgress = 1f; // 1 = fully at target

    private Camera cachedCam;

    // Cached current values (for smooth lerp)
    private Color currentAmbientColor;
    private float currentOverlayAlpha;
    private float currentWindowGlow;
    private float currentShadowAlpha;
    private float currentShadowOffsetX;

    // Public read-only
    public TimePeriod CurrentPeriod => currentTarget != null ? currentTarget.period : TimePeriod.Morning;
    public float GameHour => currentGameHour;

    /// <summary>Fired when the visual period changes. Passes (oldPeriod, newPeriod).</summary>
    public event Action<TimePeriod, TimePeriod> OnPeriodChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        EnsureOverlay();

        // Sort presets by start hour
        System.Array.Sort(periodPresets, (a, b) => a.startHour.CompareTo(b.startHour));

        // Initialize to first period
        currentTarget = GetPresetForHour(currentGameHour);
        previousTarget = currentTarget;
        transitionProgress = 1f;
        ApplyLightingImmediate(currentTarget);
    }

    void Update()
    {
        // Smoothly transition lighting values
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime * transitionSpeed;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            float t = SmoothStep(transitionProgress);
            LerpLighting(previousTarget, currentTarget, t);
        }
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Call this every frame from DaySummaryUI or DayManager to drive the cycle.
    /// gameHour: 6.0 = 6:00 AM, 12.5 = 12:30 PM, 19.0 = 7:00 PM
    /// </summary>
    public void SetGameHour(float gameHour)
    {
        currentGameHour = Mathf.Clamp(gameHour, 0f, 24f);

        PeriodLighting newTarget = GetPresetForHour(currentGameHour);
        if (newTarget != currentTarget)
        {
            TimePeriod oldPeriod = currentTarget != null ? currentTarget.period : TimePeriod.Morning;
            previousTarget = currentTarget;
            currentTarget = newTarget;
            transitionProgress = 0f;

            OnPeriodChanged?.Invoke(oldPeriod, newTarget.period);
            Debug.Log($"[DayNight] Period changed: {oldPeriod} → {newTarget.period} (hour {gameHour:F1})");
        }
    }

    /// <summary>
    /// Convert DaySummaryUI's dayTimer (0..dayLengthSeconds) into game hours (6..19).
    /// </summary>
    public void SetTimeFromDayProgress(float dayTimer, float dayLengthSeconds)
    {
        float t = Mathf.Clamp01(dayTimer / Mathf.Max(1f, dayLengthSeconds));
        float gameHour = Mathf.Lerp(6f, 19f, t); // 6 AM to 7 PM
        SetGameHour(gameHour);
    }

    /// <summary>
    /// Snap to a period instantly (no transition). Useful for scene load.
    /// </summary>
    public void SnapToHour(float gameHour)
    {
        currentGameHour = gameHour;
        currentTarget = GetPresetForHour(gameHour);
        previousTarget = currentTarget;
        transitionProgress = 1f;
        ApplyLightingImmediate(currentTarget);
    }

    /// <summary>
    /// Register window glow sprites at runtime (e.g. for dynamically placed buildings).
    /// </summary>
    public void RegisterWindowGlow(SpriteRenderer sr)
    {
        var list = new System.Collections.Generic.List<SpriteRenderer>(windowGlowSprites ?? new SpriteRenderer[0]);
        if (!list.Contains(sr)) list.Add(sr);
        windowGlowSprites = list.ToArray();
    }

    /// <summary>
    /// Register shadow sprites at runtime.
    /// </summary>
    public void RegisterShadow(SpriteRenderer sr)
    {
        var list = new System.Collections.Generic.List<SpriteRenderer>(shadowSprites ?? new SpriteRenderer[0]);
        if (!list.Contains(sr)) list.Add(sr);
        shadowSprites = list.ToArray();
    }

    // ==================== INTERNAL ====================

    PeriodLighting GetPresetForHour(float hour)
    {
        if (periodPresets == null || periodPresets.Length == 0)
            return new PeriodLighting { ambientColor = Color.white };

        // Find the latest preset whose startHour <= hour
        PeriodLighting result = periodPresets[0];
        for (int i = periodPresets.Length - 1; i >= 0; i--)
        {
            if (hour >= periodPresets[i].startHour)
            {
                result = periodPresets[i];
                break;
            }
        }
        return result;
    }

    void LerpLighting(PeriodLighting from, PeriodLighting to, float t)
    {
        if (from == null || to == null) return;

        currentAmbientColor = Color.Lerp(from.ambientColor, to.ambientColor, t);
        currentOverlayAlpha = Mathf.Lerp(from.overlayAlpha, to.overlayAlpha, t);
        currentWindowGlow = Mathf.Lerp(from.windowGlowIntensity, to.windowGlowIntensity, t);
        currentShadowAlpha = Mathf.Lerp(from.shadowAlpha, to.shadowAlpha, t);
        currentShadowOffsetX = Mathf.Lerp(from.shadowOffsetX, to.shadowOffsetX, t);

        ApplyCurrentValues();
    }

    void ApplyLightingImmediate(PeriodLighting preset)
    {
        if (preset == null) return;

        currentAmbientColor = preset.ambientColor;
        currentOverlayAlpha = preset.overlayAlpha;
        currentWindowGlow = preset.windowGlowIntensity;
        currentShadowAlpha = preset.shadowAlpha;
        currentShadowOffsetX = preset.shadowOffsetX;

        ApplyCurrentValues();
    }

    void ApplyCurrentValues()
    {
        // 1. Fullscreen overlay tint
        if (overlayRenderer != null)
        {
            Color c = currentAmbientColor;
            c.a = currentOverlayAlpha;
            overlayRenderer.color = c;
        }

        // 2. Window glow sprites (fade in at evening)
        if (windowGlowSprites != null)
        {
            foreach (var sr in windowGlowSprites)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = currentWindowGlow;
                sr.color = c;
            }
        }

        // 3. Shadow sprites (opacity + sun-angle offset)
        if (shadowSprites != null)
        {
            bool snap = transitionProgress >= 1f;
            foreach (var sr in shadowSprites)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = currentShadowAlpha;
                sr.color = c;

                Vector3 pos = sr.transform.localPosition;
                pos.x = snap ? currentShadowOffsetX : Mathf.Lerp(pos.x, currentShadowOffsetX, Time.deltaTime * 2f);
                sr.transform.localPosition = pos;
            }
        }

        // 4. Camera background tint (subtle ambient shift)
        if (cachedCam == null) cachedCam = Camera.main;
        Camera cam = cachedCam;
        if (cam != null)
        {
            Color target = currentAmbientColor * 0.3f;
            target.a = 1f;
            cam.backgroundColor = Color.Lerp(cam.backgroundColor, target, Time.deltaTime * transitionSpeed);
        }

        // 5. Global ambient light color (built-in render pipeline)
        RenderSettings.ambientLight = currentAmbientColor;
    }

    // ==================== AUTO-SETUP ====================

    void EnsureOverlay()
    {
        if (overlayRenderer != null) return;

        if (cachedCam == null) cachedCam = Camera.main;
        Camera cam = cachedCam;
        if (cam == null) return;

        GameObject overlayObj = new GameObject("DayNightOverlay");
        overlayObj.transform.SetParent(cam.transform);
        overlayObj.transform.localPosition = new Vector3(0f, 0f, 5f);
        overlayObj.transform.localRotation = Quaternion.identity;

        overlayRenderer = overlayObj.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = GetSharedPixelSprite();
        overlayRenderer.color = new Color(1f, 1f, 1f, 0f); // start invisible
        overlayRenderer.sortingOrder = overlaySortingOrder;
        // Simple mode: the 1x1 sprite stretches via localScale.
        // Sliced mode on a 1x1 sprite can cause visual artifacts.
        overlayRenderer.drawMode = SpriteDrawMode.Simple;

        // Scale to fill screen with generous margin
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        overlayObj.transform.localScale = new Vector3(camWidth + 4f, camHeight + 4f, 1f);

        Debug.Log("[DayNight] Auto-created overlay on camera.");
    }

    // Shared 1x1 white pixel sprite — created once, reused everywhere
    private static Sprite s_sharedPixelSprite;

    static Sprite GetSharedPixelSprite()
    {
        if (s_sharedPixelSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            s_sharedPixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            s_sharedPixelSprite.hideFlags = HideFlags.HideAndDontSave;
        }
        return s_sharedPixelSprite;
    }

    float SmoothStep(float t)
    {
        // Hermite interpolation for smooth transitions
        return t * t * (3f - 2f * t);
    }
}

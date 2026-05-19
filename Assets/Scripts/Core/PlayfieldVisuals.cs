using UnityEngine;

/// <summary>
/// Creates all static playfield decorations at initialization time.
/// Dark Catppuccin theme with lane dividers, hitline glow, and lane receptors.
/// Designed for iPhone 6 landscape (16:9). Zero runtime allocation.
/// </summary>
public class PlayfieldVisuals : MonoBehaviour
{
    // Lane layout constants — shared by all systems via static access
    public const float LaneSpacing = 2.0f;
    public const float LaneOffset = -1.5f; // (0 - 1.5) * spacing = leftmost lane
    public const int LaneCount = 4;

    /// <summary>
    /// Returns the world X position for a given lane index (0-3).
    /// Lane 0 = -3, Lane 1 = -1, Lane 2 = 1, Lane 3 = 3.
    /// </summary>
    public static float GetLaneX(int laneIndex)
    {
        return (laneIndex + LaneOffset) * LaneSpacing;
    }

    // Catppuccin Mocha palette
    private static readonly Color Crust    = HexColor(0x11, 0x11, 0x1b);
    private static readonly Color Mantle   = HexColor(0x18, 0x18, 0x25);
    private static readonly Color Base     = HexColor(0x1e, 0x1e, 0x2e);
    private static readonly Color Surface0 = HexColor(0x31, 0x32, 0x44);
    private static readonly Color Surface1 = HexColor(0x45, 0x47, 0x5a);
    private static readonly Color Subtext0 = HexColor(0xa6, 0xad, 0xc8);

    public static readonly Color[] LaneColors = new Color[4]
    {
        HexColor(0xf3, 0x8b, 0xa8), // Pink
        HexColor(0xcb, 0xa6, 0xf7), // Mauve
        HexColor(0x89, 0xb4, 0xfa), // Blue
        HexColor(0xa6, 0xe3, 0xa1)  // Green
    };

    // Pre-allocated lane materials for notes and sliders
    public Material[] LaneMaterials { get; private set; }
    public Material[] LaneSliderMaterials { get; private set; }

    private SpriteRenderer _hitlineGlow;
    private SpriteRenderer _hitlineCore;
    private readonly SpriteRenderer[] _receptors = new SpriteRenderer[4];

    public void Initialize()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = Crust;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        float totalWidth = (LaneCount + 1) * LaneSpacing; // extra padding on sides

        CreateBackground(totalWidth);
        CreateLaneBackgrounds();
        CreateLaneDividers();
        CreateHitline(totalWidth);
        CreateLaneReceptors();
        CreateLaneMaterials();
    }

    private void CreateBackground(float totalWidth)
    {
        GameObject bg = CreateSprite("Background", 0f, 3.5f, totalWidth + 4f, 14f, Base, -10);
        bg.transform.SetParent(transform, false);

        // Subtle side vignette strips
        float edgeX = totalWidth * 0.5f + 1.5f;
        Color vignette = Crust;
        vignette.a = 0.6f;
        GameObject vigL = CreateSprite("VignetteL", -edgeX, 3.5f, 3f, 14f, vignette, -7);
        vigL.transform.SetParent(transform, false);
        GameObject vigR = CreateSprite("VignetteR", edgeX, 3.5f, 3f, 14f, vignette, -7);
        vigR.transform.SetParent(transform, false);
    }

    private void CreateLaneBackgrounds()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            float laneX = GetLaneX(i);

            // Alternating subtle shade for depth
            Color bgColor = (i % 2 == 0) ? Mantle : Base;
            GameObject laneBg = CreateSprite("LaneBg_" + i, laneX, 3.5f, LaneSpacing * 0.95f, 14f, bgColor, -9);
            laneBg.transform.SetParent(transform, false);

            // Subtle lane color tint at the bottom (approach zone)
            Color tint = LaneColors[i];
            tint.a = 0.05f;
            GameObject laneGlow = CreateSprite("LaneGlow_" + i, laneX, 1f, LaneSpacing * 0.95f, 3f, tint, -8);
            laneGlow.transform.SetParent(transform, false);
        }
    }

    private void CreateLaneDividers()
    {
        // Divider lines at lane edges
        for (int i = 0; i <= LaneCount; i++)
        {
            float x = GetLaneX(0) - LaneSpacing * 0.5f + i * LaneSpacing;
            Color divColor = (i == 0 || i == LaneCount) ? Surface1 : Surface0;
            float width = (i == 0 || i == LaneCount) ? 0.04f : 0.02f;
            GameObject div = CreateSprite("Divider_" + i, x, 3.5f, width, 14f, divColor, -5);
            div.transform.SetParent(transform, false);
        }
    }

    private void CreateHitline(float totalWidth)
    {
        // Outer glow (wide, subtle)
        GameObject glowObj = CreateSprite("HitlineGlow", 0f, 0f, totalWidth + 1f, 0.4f, WithAlpha(Subtext0, 0.10f), 5);
        _hitlineGlow = glowObj.GetComponent<SpriteRenderer>();
        glowObj.transform.SetParent(transform, false);

        // Core line (crisp)
        float coreWidth = (LaneCount) * LaneSpacing + 0.5f;
        GameObject coreObj = CreateSprite("HitlineCore", 0f, 0f, coreWidth, 0.035f, WithAlpha(Subtext0, 0.85f), 6);
        _hitlineCore = coreObj.GetComponent<SpriteRenderer>();
        coreObj.transform.SetParent(transform, false);
    }

    private void CreateLaneReceptors()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            float laneX = GetLaneX(i);
            Color receptorColor = LaneColors[i];
            receptorColor.a = 0.5f;

            // Outer ring (larger, dimmer)
            GameObject ringObj = CreateSprite("ReceptorRing_" + i, laneX, 0f, 0.5f, 0.5f, WithAlpha(LaneColors[i], 0.15f), 6);
            ringObj.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ringObj.transform.SetParent(transform, false);

            // Inner diamond
            GameObject receptorObj = CreateSprite("Receptor_" + i, laneX, 0f, 0.3f, 0.3f, receptorColor, 7);
            receptorObj.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _receptors[i] = receptorObj.GetComponent<SpriteRenderer>();
            receptorObj.transform.SetParent(transform, false);
        }
    }

    private void CreateLaneMaterials()
    {
        Shader unlitShader = Shader.Find("Sprites/Default");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        LaneMaterials = new Material[4];
        LaneSliderMaterials = new Material[4];

        for (int i = 0; i < 4; i++)
        {
            // Tap note material: vibrant lane color
            Material noteMat = new Material(unlitShader);
            noteMat.color = LaneColors[i];
            LaneMaterials[i] = noteMat;

            // Slider material: softer, slightly transparent
            Material sliderMat = new Material(unlitShader);
            Color sliderColor = LaneColors[i];
            sliderColor.a = 0.65f;
            sliderMat.color = sliderColor;
            LaneSliderMaterials[i] = sliderMat;
        }
    }

    private void Update()
    {
        // Hitline pulse (value-type only, zero allocation)
        if (_hitlineGlow != null)
        {
            float pulse = 0.06f + 0.05f * Mathf.Sin(Time.time * 3f);
            Color c = _hitlineGlow.color;
            c.a = pulse;
            _hitlineGlow.color = c;
        }

        // Receptor pulse
        float receptorPulse = 0.35f + 0.2f * Mathf.Sin(Time.time * 2f);
        for (int i = 0; i < 4; i++)
        {
            if (_receptors[i] != null)
            {
                Color rc = LaneColors[i];
                rc.a = receptorPulse;
                _receptors[i].color = rc;
            }
        }
    }

    // --- Helpers ---

    private static Sprite _sharedWhiteSprite;

    private static GameObject CreateSprite(string name, float x, float y, float w, float h, Color color, int sortOrder)
    {
        GameObject obj = new GameObject(name);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = sortOrder;
        obj.transform.localPosition = new Vector3(x, y, 0f);
        obj.transform.localScale = new Vector3(w, h, 1f);
        return obj;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_sharedWhiteSprite != null) return _sharedWhiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _sharedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _sharedWhiteSprite;
    }

    private static Color HexColor(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}

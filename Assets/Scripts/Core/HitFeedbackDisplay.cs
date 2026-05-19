using UnityEngine;

/// <summary>
/// Pre-instantiated visual feedback for lane presses, hits, and misses.
/// 4 lane overlays (glow on hold) + 4 hit flash quads + 4 judgement text labels.
/// Zero runtime allocation.
/// </summary>
public class HitFeedbackDisplay : MonoBehaviour
{
    private readonly SpriteRenderer[] _laneOverlays = new SpriteRenderer[4];
    private readonly SpriteRenderer[] _hitFlashes = new SpriteRenderer[4];
    private readonly float[] _hitFlashTimers = new float[4];

    // Tick hit/miss flash (smaller, quicker burst at hitline)
    private readonly SpriteRenderer[] _tickFlashes = new SpriteRenderer[4];
    private readonly float[] _tickFlashTimers = new float[4];
    private readonly bool[] _tickFlashIsHit = new bool[4];

    // Judgement text (HIT / MISS)
    private readonly TextMesh[] _judgementTexts = new TextMesh[4];
    private readonly float[] _judgementTimers = new float[4];
    private readonly int[] _judgementTier = new int[4];
    private readonly Vector3[] _judgementBasePos = new Vector3[4];

    private const float HitFlashDuration = 0.12f;
    private const float OverlayAlpha = 0.18f;
    private const float HitFlashAlpha = 0.6f;
    private const float JudgementDuration = 0.5f;
    private const float JudgementRiseSpeed = 1.5f;
    private const float TickFlashDuration = 0.08f;
    private const float TickFlashAlpha = 0.45f;

    // Catppuccin-inspired lane colors
    private static readonly Color[] LaneColors = new Color[4]
    {
        new Color(0.953f, 0.545f, 0.659f, 1f), // Pink #f38ba8
        new Color(0.796f, 0.651f, 0.969f, 1f), // Mauve #cba6f7
        new Color(0.537f, 0.706f, 0.980f, 1f), // Blue #89b4fa
        new Color(0.651f, 0.890f, 0.631f, 1f)  // Green #a6e3a1
    };

    private JudgementEngine _judgementEngine;

    public void Initialize(JudgementEngine judgementEngine)
    {
        _judgementEngine = judgementEngine;

        for (int i = 0; i < 4; i++)
        {
            float laneX = PlayfieldVisuals.GetLaneX(i);

            // Lane overlay: tall strip behind the notes, subtle glow on hold
            GameObject overlayObj = CreateQuad("LaneOverlay_" + i, laneX, 3.5f, PlayfieldVisuals.LaneSpacing * 0.95f, 10f);
            _laneOverlays[i] = overlayObj.GetComponent<SpriteRenderer>();
            _laneOverlays[i].color = WithAlpha(LaneColors[i], 0f);
            _laneOverlays[i].sortingOrder = -1;
            overlayObj.transform.SetParent(transform, false);

            // Hit flash: bright burst at the hitline on tap
            GameObject flashObj = CreateQuad("HitFlash_" + i, laneX, 0f, PlayfieldVisuals.LaneSpacing * 0.95f, 0.6f);
            _hitFlashes[i] = flashObj.GetComponent<SpriteRenderer>();
            _hitFlashes[i].color = WithAlpha(Color.white, 0f);
            _hitFlashes[i].sortingOrder = 10;
            flashObj.transform.SetParent(transform, false);

            _hitFlashTimers[i] = 0f;

            // Judgement text: floats up from hitline
            GameObject textObj = new GameObject("JudgementText_" + i);
            textObj.transform.SetParent(transform, false);
            Vector3 basePos = new Vector3(laneX, 0.5f, -0.5f);
            textObj.transform.localPosition = basePos;
            _judgementBasePos[i] = basePos;

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = "";
            tm.characterSize = 0.15f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = WithAlpha(Color.white, 0f);
            _judgementTexts[i] = tm;
            _judgementTimers[i] = 0f;
            _judgementTier[i] = 3;

            // Tick flash: compact burst centered on hitline for tick feedback
            GameObject tickFlashObj = CreateQuad("TickFlash_" + i, laneX, 0f, PlayfieldVisuals.LaneSpacing * 0.65f, 0.3f);
            _tickFlashes[i] = tickFlashObj.GetComponent<SpriteRenderer>();
            _tickFlashes[i].color = WithAlpha(Color.white, 0f);
            _tickFlashes[i].sortingOrder = 11;
            tickFlashObj.transform.SetParent(transform, false);
            _tickFlashTimers[i] = 0f;
            _tickFlashIsHit[i] = true;
        }
    }

    public void ShowHit(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex > 3) return;
        _hitFlashTimers[laneIndex] = HitFlashDuration;
        ShowJudgement(laneIndex, 0);
    }

    public void ShowMiss(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex > 3) return;
        ShowJudgement(laneIndex, 3);
    }

    /// <summary>
    /// Subtle quick flash for a successfully held slider tick. Adds to combo feel.
    /// </summary>
    public void ShowTickHit(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex > 3) return;
        _tickFlashTimers[laneIndex] = TickFlashDuration;
        _tickFlashIsHit[laneIndex] = true;
    }

    /// <summary>
    /// Dim red flash for a missed slider tick (released during hold).
    /// </summary>
    public void ShowTickMiss(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex > 3) return;
        _tickFlashTimers[laneIndex] = TickFlashDuration;
        _tickFlashIsHit[laneIndex] = false;
    }

    // Tier: 0=PERFECT, 1=GREAT, 2=GOOD, 3=MISS
    private static readonly string[] TierLabels = { "PERFECT", "GREAT", "GOOD", "MISS" };
    private static readonly Color[] TierColors =
    {
        new Color(0.980f, 0.843f, 0.373f, 1f), // Yellow  #fad65a (PERFECT)
        new Color(0.651f, 0.890f, 0.631f, 1f), // Green   #a6e3a1 (GREAT)
        new Color(0.537f, 0.706f, 0.980f, 1f), // Blue    #89b4fa (GOOD)
        new Color(0.953f, 0.545f, 0.659f, 1f)  // Pink    #f38ba8 (MISS)
    };
    private static readonly float[] TierScales = { 1.4f, 1.2f, 1.0f, 0.9f };

    /// <summary>
    /// Show a judgement indicator. Tier: 0=PERFECT, 1=GREAT, 2=GOOD, 3=MISS.
    /// </summary>
    public void ShowJudgement(int laneIndex, int tier)
    {
        if (laneIndex < 0 || laneIndex > 3) return;
        if (tier < 0) tier = 0;
        if (tier > 3) tier = 3;

        // Flash on any successful hit (not miss)
        if (tier < 3) _hitFlashTimers[laneIndex] = HitFlashDuration;

        _judgementTimers[laneIndex] = JudgementDuration;
        _judgementTier[laneIndex] = tier;

        TextMesh tm = _judgementTexts[laneIndex];
        if (tm == null) return;
        tm.text = TierLabels[tier];
        tm.color = TierColors[tier];
        tm.transform.localPosition = _judgementBasePos[laneIndex];
        float s = TierScales[tier];
        tm.transform.localScale = new Vector3(s, s, 1f);
    }

    private void Update()
    {
        if (_judgementEngine == null) return;
        float dt = Time.deltaTime;

        for (int i = 0; i < 4; i++)
        {
            // Lane overlay: visible while held
            bool isHeld = _judgementEngine.LaneIsHeld[i];
            if (_laneOverlays[i] != null)
            {
                float targetAlpha = isHeld ? OverlayAlpha : 0f;
                _laneOverlays[i].color = WithAlpha(LaneColors[i], targetAlpha);
            }

            // Hit flash: decaying burst
            if (_hitFlashTimers[i] > 0f)
            {
                _hitFlashTimers[i] -= dt;
                float t = Mathf.Clamp01(_hitFlashTimers[i] / HitFlashDuration);
                if (_hitFlashes[i] != null)
                {
                    _hitFlashes[i].color = WithAlpha(LaneColors[i], t * HitFlashAlpha);
                }
            }
            else if (_hitFlashes[i] != null)
            {
                _hitFlashes[i].color = WithAlpha(LaneColors[i], 0f);
            }

            // Tick flash: quick compact burst
            if (_tickFlashTimers[i] > 0f)
            {
                _tickFlashTimers[i] -= dt;
                float tt = Mathf.Clamp01(_tickFlashTimers[i] / TickFlashDuration);
                if (_tickFlashes[i] != null)
                {
                    Color flashColor = _tickFlashIsHit[i] ? LaneColors[i] : new Color(0.953f, 0.545f, 0.659f, 1f);
                    _tickFlashes[i].color = WithAlpha(flashColor, tt * TickFlashAlpha);
                    // Quick scale punch: starts at 1.15x, settles to 1x
                    float punchScale = 1f + tt * 0.15f;
                    _tickFlashes[i].transform.localScale = new Vector3(
                        PlayfieldVisuals.LaneSpacing * 0.65f * punchScale,
                        0.3f * punchScale,
                        1f);
                }
            }
            else if (_tickFlashes[i] != null)
            {
                _tickFlashes[i].color = WithAlpha(Color.white, 0f);
            }

            // Judgement text: float up and fade
            if (_judgementTimers[i] > 0f)
            {
                _judgementTimers[i] -= dt;
                float t = Mathf.Clamp01(_judgementTimers[i] / JudgementDuration);
                TextMesh tm = _judgementTexts[i];
                if (tm != null)
                {
                    Color baseColor = TierColors[_judgementTier[i]];
                    tm.color = WithAlpha(baseColor, t);

                    // Float upward
                    Vector3 pos = _judgementBasePos[i];
                    pos.y += (1f - t) * JudgementRiseSpeed;
                    tm.transform.localPosition = pos;

                    // Scale punch on hit (starts big, settles to 1)
                    if (_judgementTier[i] < 3) // scale punch on hits only
                    {
                        float scale = 1f + (t * 0.3f);
                        tm.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }
            }
            else if (_judgementTexts[i] != null)
            {
                _judgementTexts[i].color = WithAlpha(Color.white, 0f);
            }
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static GameObject CreateQuad(string objectName, float x, float y, float width, float height)
    {
        GameObject quad = new GameObject(objectName);
        SpriteRenderer sr = quad.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        quad.transform.localPosition = new Vector3(x, y, 0.1f);
        quad.transform.localScale = new Vector3(width, height, 1f);
        return quad;
    }

    private static Sprite _sharedWhiteSprite;

    private static Sprite CreateWhiteSprite()
    {
        if (_sharedWhiteSprite != null) return _sharedWhiteSprite;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        _sharedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _sharedWhiteSprite;
    }
}

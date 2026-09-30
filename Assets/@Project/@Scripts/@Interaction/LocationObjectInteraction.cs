using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 장소에 배치된 오브젝트 그림을 직접 클릭해 조사한다. 화면 표시/마우스 반응만 담당하고,
// 조사 내용과 조건은 LocationData의 LocationHotspot(Display Name으로 연결)을 그대로 사용한다.
[RequireComponent(typeof(Image))]
public class LocationObjectInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private LocationManager locationManager;

    [Tooltip("연결할 LocationHotspot의 Display Name")]
    [SerializeField] private string hotspotName;

    [Tooltip("이 값보다 불투명한 픽셀만 클릭된다 (텍스처 Read/Write 필요)")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaHitThreshold = 0.1f;

    [Header("Soft Glow (원본 뒤의 형제 Image 3개)")]
    [SerializeField] private Image glowInner;
    [SerializeField] private Image glowMiddle;
    [SerializeField] private Image glowOuter;

    [SerializeField] private Color glowColor = new Color(1f, 0.93f, 0.72f, 1f);

    [SerializeField] private float innerScale = 1.0f;
    [Range(0f, 1f)] [SerializeField] private float innerAlpha = 0.3f;
    [SerializeField] private float middleScale = 1.015f;
    [Range(0f, 1f)] [SerializeField] private float middleAlpha = 0.18f;
    [SerializeField] private float outerScale = 1.03f;
    [Range(0f, 1f)] [SerializeField] private float outerAlpha = 0.1f;

    [SerializeField] private float fadeDuration = 0.2f;

    // 흐린 외곽 그림 생성: 원본 크기와 상관없이 가로 약 GlowTextureWidth 픽셀로 줄인 뒤 흐리게 한다.
    // 화면에 표시되는 크기 대비 흐림 폭이 일정해진다. (반경이 클수록 더 넓게 퍼짐)
    private const int GlowTextureWidth = 160;
    private const int GlowBlurRadius = 3;
    private const int GlowBlurPasses = 3;

    private float glowAmount;
    private float targetAmount;

    public string HotspotName => hotspotName;

    private void Awake()
    {
        Image image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = alphaHitThreshold;

        Vector2 padding;
        Sprite glowSprite = CreateSoftSilhouette(image.sprite, out padding);
        SetupGlow(glowInner, glowSprite, innerScale, padding);
        SetupGlow(glowMiddle, glowSprite, middleScale, padding);
        SetupGlow(glowOuter, glowSprite, outerScale, padding);
        ApplyGlow(0f);
    }

    private void OnDisable()
    {
        targetAmount = 0f;
        ApplyGlow(0f);
    }

    private void Update()
    {
        if (Mathf.Approximately(glowAmount, targetAmount))
            return;
        float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
        ApplyGlow(Mathf.MoveTowards(glowAmount, targetAmount, step));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAmount = locationManager.CanInspect(hotspotName) ? 1f : 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAmount = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!locationManager.CanInspect(hotspotName))
            return;
        targetAmount = 0f;
        ApplyGlow(0f);
        locationManager.InspectByName(hotspotName);
    }

    // padding: 빛 그림에 추가된 여백 (원본 크기 대비 비율). 그만큼 넓혀서 원본과 정확히 겹치게 한다.
    private static void SetupGlow(Image glow, Sprite sprite, float scale, Vector2 padding)
    {
        if (glow == null)
            return;
        glow.sprite = sprite;
        glow.preserveAspect = false;
        glow.raycastTarget = false;
        RectTransform rect = glow.rectTransform;
        rect.anchorMin = -padding;
        rect.anchorMax = Vector2.one + padding;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one * scale;
    }

    private void ApplyGlow(float amount)
    {
        glowAmount = amount;
        SetGlowAlpha(glowInner, innerAlpha * amount);
        SetGlowAlpha(glowMiddle, middleAlpha * amount);
        SetGlowAlpha(glowOuter, outerAlpha * amount);
    }

    private void SetGlowAlpha(Image glow, float alpha)
    {
        if (glow == null)
            return;
        Color c = glowColor;
        c.a = alpha;
        glow.color = c;
    }

    // 원본과 같은 모양의 흰색 그림(알파만 사용)을 작게 줄여 흐리게 만든다. 늘려서 표시되면 부드럽게 퍼진다.
    // 흐림이 잘리지 않도록 가장자리에 여백을 두고, 그 비율을 padding으로 돌려준다.
    private static Sprite CreateSoftSilhouette(Sprite source, out Vector2 padding)
    {
        Rect rect = source.textureRect;
        Color32[] pixels = source.texture.GetPixels32();
        int texWidth = source.texture.width;
        int x0 = (int)rect.x, y0 = (int)rect.y, srcW = (int)rect.width, srcH = (int)rect.height;

        int step = Mathf.Max(1, Mathf.RoundToInt((float)srcW / GlowTextureWidth));
        int innerW = Mathf.Max(1, srcW / step), innerH = Mathf.Max(1, srcH / step);
        int pad = GlowBlurRadius * GlowBlurPasses;
        int w = innerW + pad * 2, h = innerH + pad * 2;

        var alpha = new float[w * h];
        for (int y = 0; y < innerH; y++)
        {
            for (int x = 0; x < innerW; x++)
            {
                float sum = 0f;
                for (int dy = 0; dy < step; dy++)
                    for (int dx = 0; dx < step; dx++)
                        sum += pixels[(y0 + y * step + dy) * texWidth + x0 + x * step + dx].a;
                alpha[(y + pad) * w + x + pad] = sum / (step * step * 255f);
            }
        }

        // 박스 블러 여러 번 = 가우시안에 가까운 부드러운 퍼짐
        for (int pass = 0; pass < GlowBlurPasses; pass++)
        {
            alpha = BoxBlur(alpha, w, h, GlowBlurRadius, true);
            alpha = BoxBlur(alpha, w, h, GlowBlurRadius, false);
        }
        padding = new Vector2((float)pad / innerW, (float)pad / innerH);

        var result = new Color32[w * h];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha[i]) * 255f));

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = source.name + "_Glow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(result);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), source.pixelsPerUnit / step);
    }

    private static float[] BoxBlur(float[] src, int w, int h, int radius, bool horizontal)
    {
        var dst = new float[src.Length];
        int lineCount = horizontal ? h : w, length = horizontal ? w : h;
        for (int line = 0; line < lineCount; line++)
        {
            for (int i = 0; i < length; i++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int j = i + k;
                    if (j < 0 || j >= length)
                        continue; // 그림 밖은 투명으로 취급
                    sum += horizontal ? src[line * w + j] : src[j * w + line];
                }
                dst[horizontal ? line * w + i : i * w + line] = sum / (radius * 2 + 1);
            }
        }
        return dst;
    }
}

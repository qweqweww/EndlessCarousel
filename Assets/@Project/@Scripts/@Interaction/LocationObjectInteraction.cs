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

    [SerializeField] private float innerScale = 1.02f;
    [Range(0f, 1f)] [SerializeField] private float innerAlpha = 0.22f;
    [SerializeField] private float middleScale = 1.045f;
    [Range(0f, 1f)] [SerializeField] private float middleAlpha = 0.14f;
    [SerializeField] private float outerScale = 1.075f;
    [Range(0f, 1f)] [SerializeField] private float outerAlpha = 0.07f;

    [SerializeField] private float fadeDuration = 0.2f;

    // 흐린 외곽 그림 생성: 1/4 크기로 줄인 뒤 박스 블러 (값이 클수록 더 퍼짐)
    private const int GlowDownsample = 4;
    private const int GlowBlurRadius = 3;

    private float glowAmount;
    private float targetAmount;

    public string HotspotName => hotspotName;

    private void Awake()
    {
        Image image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = alphaHitThreshold;

        Sprite glowSprite = CreateSoftSilhouette(image.sprite);
        SetupGlow(glowInner, glowSprite, innerScale, image.preserveAspect);
        SetupGlow(glowMiddle, glowSprite, middleScale, image.preserveAspect);
        SetupGlow(glowOuter, glowSprite, outerScale, image.preserveAspect);
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

    private static void SetupGlow(Image glow, Sprite sprite, float scale, bool preserveAspect)
    {
        if (glow == null)
            return;
        glow.sprite = sprite;
        glow.preserveAspect = preserveAspect;
        glow.raycastTarget = false;
        glow.rectTransform.localScale = Vector3.one * scale;
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
    private static Sprite CreateSoftSilhouette(Sprite source)
    {
        Rect rect = source.textureRect;
        Color32[] pixels = source.texture.GetPixels32();
        int texWidth = source.texture.width;
        int x0 = (int)rect.x, y0 = (int)rect.y, srcW = (int)rect.width, srcH = (int)rect.height;

        int w = Mathf.Max(1, srcW / GlowDownsample), h = Mathf.Max(1, srcH / GlowDownsample);
        var alpha = new float[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int dy = 0; dy < GlowDownsample; dy++)
                    for (int dx = 0; dx < GlowDownsample; dx++)
                        sum += pixels[(y0 + y * GlowDownsample + dy) * texWidth + x0 + x * GlowDownsample + dx].a;
                alpha[y * w + x] = sum / (GlowDownsample * GlowDownsample * 255f);
            }
        }

        // 박스 블러 두 번 = 가우시안에 가까운 부드러운 퍼짐
        for (int pass = 0; pass < 2; pass++)
        {
            alpha = BoxBlur(alpha, w, h, GlowBlurRadius, true);
            alpha = BoxBlur(alpha, w, h, GlowBlurRadius, false);
        }

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
        return Sprite.Create(texture, new Rect(0, 0, w, h), source.pivot / source.rect.size, source.pixelsPerUnit / GlowDownsample);
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

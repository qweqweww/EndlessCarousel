using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 조사 대상의 이미지를 화면 중앙에 크게 보여준다. 두 가지 방식으로 쓴다.
// Show: 조사창처럼 띄운다. 어두운 배경이나 X 버튼을 클릭하거나 Esc를 누르면 닫히고, 다 닫힌 뒤에 onClosed를 호출한다.
// ShowOverlay: 대화 연출용. 닫기 UI 없이 떠 있다가 DialoguePanel이 Hide를 호출하면 사라진다.
public class InspectImagePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image image;

    [Tooltip("화면 전체를 덮는 어두운 배경. 클릭하면 닫히고, 뒤쪽 오브젝트 클릭을 막는다")]
    [SerializeField] private Button closeArea;
    [SerializeField] private Button closeButton;

    [Tooltip("이미지 최대 크기 (1920x1080 기준). 비율을 유지한 채 이 안에 맞춘다")]
    [SerializeField] private Vector2 maxImageSize = new Vector2(1500f, 760f);

    [SerializeField] private float openDuration = 0.18f;
    [SerializeField] private float closeDuration = 0.15f;
    [SerializeField] private float openStartScale = 0.95f;

    [Header("대화 연출 (ShowOverlay)")]
    [Tooltip("대화 중에 띄울 때의 어두운 배경 투명도 (조사창 방식보다 옅게)")]
    [Range(0f, 1f)] [SerializeField] private float overlayDimAlpha = 0.4f;
    [SerializeField] private float overlayCloseDuration = 0.25f;

    private System.Action onClosed;
    private bool closing;
    private bool overlay;
    private Image dimImage;
    private float modalDimAlpha = -1f;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        closeArea.onClick.AddListener(Hide);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(Sprite sprite, System.Action onClosed = null)
    {
        Open(sprite, false);
        this.onClosed = onClosed;
    }

    // 이미 같은 이미지가 떠 있으면 다시 페이드하지 않고 그대로 둔다
    public void ShowOverlay(Sprite sprite)
    {
        if (IsOpen && !closing && overlay && image.sprite == sprite)
            return;
        Open(sprite, true);
        onClosed = null;
    }

    private void Open(Sprite sprite, bool asOverlay)
    {
        // 사라지는 중에 다시 열리면 현재 투명도에서 이어서 나타난다
        if (!IsOpen)
            group.alpha = 0f;
        IsOpen = true;
        closing = false;
        overlay = asOverlay;

        image.sprite = sprite;
        image.preserveAspect = true;
        // 원본 비율 그대로, 최대 크기 안에 맞춘다
        float aspect = sprite.rect.width / sprite.rect.height;
        Vector2 size = new Vector2(maxImageSize.y * aspect, maxImageSize.y);
        if (size.x > maxImageSize.x)
            size = new Vector2(maxImageSize.x, maxImageSize.x / aspect);
        image.rectTransform.sizeDelta = size;

        gameObject.SetActive(true);
        ApplyMode();
        StopAllCoroutines();
        StartCoroutine(Fade(group.alpha, 1f, openStartScale, 1f, openDuration, null));
    }

    // 대화 연출일 때는 닫기 버튼을 숨기고, 배경을 옅게 하고, 클릭을 받지 않는다 (대화 진행은 DialoguePanel이 받는다)
    private void ApplyMode()
    {
        if (dimImage == null)
        {
            dimImage = closeArea.GetComponent<Image>();
            modalDimAlpha = dimImage.color.a;
        }
        Color color = dimImage.color;
        color.a = overlay ? overlayDimAlpha : modalDimAlpha;
        dimImage.color = color;
        dimImage.raycastTarget = !overlay;
        if (closeButton != null)
            closeButton.gameObject.SetActive(!overlay);
    }

    public void Hide()
    {
        if (!IsOpen || closing)
            return;
        closing = true;
        // 다 사라질 때까지 뒤쪽 클릭은 계속 막는다
        StopAllCoroutines();
        StartCoroutine(Fade(group.alpha, 0f, 1f, 1f, overlay ? overlayCloseDuration : closeDuration, Finish));
    }

    private void Update()
    {
        if (IsOpen && !closing && !overlay && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    private void Finish()
    {
        gameObject.SetActive(false);
        IsOpen = false;
        System.Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration, System.Action done)
    {
        Transform target = image.transform;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float eased = 1f - (1f - k) * (1f - k);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            target.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            yield return null;
        }
        group.alpha = toAlpha;
        target.localScale = Vector3.one * toScale;
        done?.Invoke();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 메뉴의 텍스트형 항목. 마우스를 올리면 글자색이 바뀌고 살짝 오른쪽으로 밀리며 왼쪽 표시가 나타난다.
// 매 프레임 목표 상태로 다가가기만 하므로, 마우스를 빠르게 움직여도 상태가 꼬이지 않는다.
[RequireComponent(typeof(Button))]
public class TitleMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [Tooltip("Hover 때 나타나는 왼쪽 표시 (비워도 됨)")]
    [SerializeField] private Graphic marker;

    [SerializeField] private Color normalColor = new Color(0.90f, 0.87f, 0.80f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.84f, 0.56f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.90f, 0.87f, 0.80f, 0.45f);

    [Tooltip("Hover 때 글자가 오른쪽으로 이동하는 거리 (px)")]
    [SerializeField] private float hoverOffset = 8f;
    [Tooltip("Normal ↔ Hover 전환 시간 (초)")]
    [SerializeField] private float transitionDuration = 0.12f;

    private Button button;
    private RectTransform labelRect;
    private Vector2 labelBasePosition;
    private bool pointerInside;
    private bool wasInteractable = true;
    private bool selected; // 옵션 값처럼 "현재 선택됨"을 표시할 때 (색과 표시만 유지, 이동은 Hover 때만)
    private float amount; // 0 = Normal, 1 = Hover

    public void SetSelected(bool value)
    {
        selected = value;
        if (labelRect != null)
            Apply();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        labelRect = label.rectTransform;
        labelBasePosition = labelRect.anchoredPosition;
        Apply();
    }

    private void OnDisable()
    {
        pointerInside = false;
        amount = 0f;
        Apply();
    }

    private void Update()
    {
        float target = pointerInside && button.interactable ? 1f : 0f;
        if (Mathf.Approximately(amount, target) && wasInteractable == button.interactable)
            return;
        wasInteractable = button.interactable;
        float step = transitionDuration > 0f ? Time.unscaledDeltaTime / transitionDuration : 1f;
        amount = Mathf.MoveTowards(amount, target, step);
        Apply();
    }

    private void Apply()
    {
        // Ease Out
        float k = 1f - (1f - amount) * (1f - amount);
        float highlight = selected ? 1f : k;
        Color baseColor = button != null && !button.interactable ? disabledColor : normalColor;
        label.color = Color.Lerp(baseColor, hoverColor, highlight);
        labelRect.anchoredPosition = labelBasePosition + new Vector2(hoverOffset * k, 0f);
        if (marker != null)
        {
            Color c = hoverColor;
            c.a = highlight;
            marker.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;
    public void OnPointerExit(PointerEventData eventData) => pointerInside = false;
}

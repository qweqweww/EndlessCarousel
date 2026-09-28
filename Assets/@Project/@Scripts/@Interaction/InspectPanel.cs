using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 조사 내용을 보여주는 공통 창. 디자인은 Prefab에서 바꾸고, 내용은 LocationHotspot 데이터에서 받는다.
public class InspectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image inspectImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    private System.Action onClosed;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(Hide);
    }

    // onClosed: 플레이어가 창을 닫았을 때 한 번 호출 (조사 완료 처리용)
    public void Show(LocationHotspot hotspot, System.Action onClosed = null)
    {
        this.onClosed = onClosed;

        titleText.text = hotspot.displayName;
        bodyText.text = hotspot.inspectText;

        inspectImage.sprite = hotspot.inspectImage;
        inspectImage.gameObject.SetActive(hotspot.inspectImage != null);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        System.Action callback = onClosed;
        onClosed = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }
}

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

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(Hide);
    }

    public void Show(LocationHotspot hotspot)
    {
        titleText.text = hotspot.displayName;
        bodyText.text = hotspot.inspectText;

        inspectImage.sprite = hotspot.inspectImage;
        inspectImage.gameObject.SetActive(hotspot.inspectImage != null);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

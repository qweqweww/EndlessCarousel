using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 타이틀의 가벼운 옵션 창. 화면 모드(전체 화면 / 창 모드)와 16:9 해상도만 바꾼다.
// 열고 닫는 연출은 TitleMenuController가 맡고, 여기서는 설정 적용과 선택 표시만 한다.
public class TitleOptionPanel : MonoBehaviour
{
    [System.Serializable]
    private class ResolutionOption
    {
        public int width = 1920;
        public int height = 1080;
        public TitleMenuItem item;
    }

    [Header("화면 모드")]
    [SerializeField] private TitleMenuItem fullscreenItem;
    [SerializeField] private TitleMenuItem windowedItem;

    [Header("해상도 (모니터보다 큰 해상도는 숨긴다)")]
    [SerializeField] private List<ResolutionOption> resolutions = new List<ResolutionOption>();

    [SerializeField] private Button backButton;

    public event System.Action BackRequested;

    private FullScreenMode mode;
    private int width;
    private int height;

    private void Awake()
    {
        fullscreenItem.GetComponent<Button>().onClick.AddListener(() => SetMode(FullScreenMode.FullScreenWindow));
        windowedItem.GetComponent<Button>().onClick.AddListener(() => SetMode(FullScreenMode.Windowed));
        foreach (ResolutionOption option in resolutions)
        {
            ResolutionOption captured = option;
            option.item.GetComponent<Button>().onClick.AddListener(() => SetResolution(captured.width, captured.height));
        }
        backButton.onClick.AddListener(() => BackRequested?.Invoke());
    }

    // 열 때마다 현재 화면 상태를 읽어 선택 표시를 맞춘다
    public void Refresh()
    {
        mode = Screen.fullScreenMode == FullScreenMode.Windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        width = Screen.width;
        height = Screen.height;

        int maxWidth = Screen.currentResolution.width;
        int maxHeight = Screen.currentResolution.height;
        foreach (ResolutionOption option in resolutions)
        {
            bool fits = option.width <= maxWidth && option.height <= maxHeight;
            option.item.gameObject.SetActive(fits);
        }
        UpdateSelection();
    }

    private void SetMode(FullScreenMode newMode)
    {
        mode = newMode;
        Apply();
    }

    private void SetResolution(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        Apply();
    }

    private void Apply()
    {
        Screen.SetResolution(width, height, mode);
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        fullscreenItem.SetSelected(mode != FullScreenMode.Windowed);
        windowedItem.SetSelected(mode == FullScreenMode.Windowed);
        foreach (ResolutionOption option in resolutions)
            option.item.SetSelected(option.width == width && option.height == height);
    }
}

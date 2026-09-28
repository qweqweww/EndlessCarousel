using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 현재 장소를 화면에 표시하고, LocationData의 연결 정보로 이동 버튼을 만든다. 모든 장소가 이 하나를 공유한다.
public class LocationManager : MonoBehaviour
{
    [SerializeField] private LocationData startLocation;

    [Header("UI 참조")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text locationNameText;
    [SerializeField] private RectTransform exitButtonParent;
    [SerializeField] private Button exitButtonPrefab;

    [Header("조사")]
    [SerializeField] private RectTransform hotspotParent;
    [SerializeField] private Button hotspotPrefab;
    [SerializeField] private InspectPanel inspectPanel;

    [Header("대화")]
    [SerializeField] private DialoguePanel dialoguePanel;

    [Header("기억 이벤트")]
    [SerializeField] private MemoryPanel memoryPanel;

    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<Button> hotspotButtons = new List<Button>();

    public LocationData CurrentLocation { get; private set; }

    private void OnEnable()
    {
        GameFlags.Changed += OnFlagChanged;
        ChapterManager.Changed += OnChapterChanged;
    }

    private void OnDisable()
    {
        GameFlags.Changed -= OnFlagChanged;
        ChapterManager.Changed -= OnChapterChanged;
    }

    private void Start()
    {
        MoveTo(startLocation);
    }

    public void MoveTo(LocationData location)
    {
        if (location == null)
        {
            Debug.LogWarning("[LocationManager] 이동할 LocationData가 비어 있습니다.", this);
            return;
        }

        CurrentLocation = location;
        backgroundImage.sprite = location.Background;
        if (locationNameText != null)
            locationNameText.text = location.DisplayName;

        RefreshExits();
        RefreshHotspots();

        if (inspectPanel != null)
            inspectPanel.Hide();
    }

    // 현재 장소의 이동 버튼을 다시 만든다. 아직 해금되지 않은 장소로 가는 버튼은 만들지 않는다.
    private void RefreshExits()
    {
        foreach (Button button in spawnedButtons)
            Destroy(button.gameObject);
        spawnedButtons.Clear();

        foreach (LocationExit exit in CurrentLocation.Exits)
        {
            if (exit.target == null || ChapterManager.IsReached(exit.target.UnlockChapter))
                spawnedButtons.Add(CreateExitButton(exit));
        }
    }

    // 현재 장소의 Hotspot을 조건(canInspect, 챕터, requiredFlag)에 맞게 다시 만든다.
    private void RefreshHotspots()
    {
        foreach (Button button in hotspotButtons)
            Destroy(button.gameObject);
        hotspotButtons.Clear();

        foreach (LocationHotspot hotspot in CurrentLocation.Hotspots)
        {
            if (IsHotspotAvailable(hotspot))
                hotspotButtons.Add(CreateHotspotButton(hotspot));
        }
    }

    private static bool IsHotspotAvailable(LocationHotspot hotspot)
    {
        if (!hotspot.canInspect)
            return false;
        if (!ChapterManager.IsReached(hotspot.minChapter))
            return false;
        if (hotspot.limitMaxChapter && ChapterManager.Current > hotspot.maxChapter)
            return false;
        return string.IsNullOrWhiteSpace(hotspot.requiredFlag) || GameFlags.Has(hotspot.requiredFlag);
    }

    private void OnFlagChanged(string id, bool value)
    {
        if (CurrentLocation != null)
            RefreshHotspots();
    }

    private void OnChapterChanged(Chapter chapter)
    {
        if (CurrentLocation == null)
            return;
        RefreshExits();
        RefreshHotspots();
    }

    private Button CreateExitButton(LocationExit exit)
    {
        Button button = Instantiate(exitButtonPrefab, exitButtonParent);

        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = exit.screenPosition;
        rect.anchorMax = exit.screenPosition;
        rect.anchoredPosition = Vector2.zero;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = exit.label;

        LocationData target = exit.target;
        button.onClick.AddListener(() => MoveTo(target));
        return button;
    }

    private Button CreateHotspotButton(LocationHotspot hotspot)
    {
        Button button = Instantiate(hotspotPrefab, hotspotParent);

        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = hotspot.screenPosition;
        rect.anchorMax = hotspot.screenPosition;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = hotspot.size;

        button.onClick.AddListener(() =>
        {
            System.Action onComplete = () => GameFlags.Set(hotspot.setFlagOnComplete);
            if (hotspot.memoryEvent != null)
                memoryPanel.Play(hotspot.memoryEvent, onComplete);
            else if (hotspot.dialogue != null)
                dialoguePanel.Play(hotspot.dialogue, onComplete);
            else
                inspectPanel.Show(hotspot, onComplete);
        });
        return button;
    }
}

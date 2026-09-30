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

    [Header("엔딩")]
    [SerializeField] private EndingManager endingManager;

    [System.Serializable]
    private class LocationObjectGroup
    {
        public LocationData location;
        [Tooltip("이 장소에 있을 때만 켜지는 오브젝트 (조사 대상 그림 등)")]
        public GameObject root;
    }

    [Header("장소별 오브젝트 이미지")]
    [SerializeField] private List<LocationObjectGroup> locationObjects = new List<LocationObjectGroup>();

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

        foreach (LocationObjectGroup group in locationObjects)
        {
            if (group.root != null)
                group.root.SetActive(group.location == location);
        }

        RefreshExits();
        RefreshHotspots();

        if (inspectPanel != null)
            inspectPanel.Hide();

        TryRunAutoEvent();
    }

    // 조건이 맞는 Auto Event를 하나 재생한다. 대화/조사/기억이 진행 중이면 건너뛰고, 그것이 끝날 때 다시 확인된다.
    private void TryRunAutoEvent()
    {
        if (CurrentLocation == null || dialoguePanel == null || IsBusy())
            return;

        foreach (LocationAutoEvent autoEvent in CurrentLocation.AutoEvents)
        {
            if (autoEvent.IsReady())
            {
                dialoguePanel.Play(autoEvent.dialogue, () => CompleteAutoEvent(autoEvent));
                return;
            }
        }
    }

    private void CompleteAutoEvent(LocationAutoEvent autoEvent)
    {
        GameFlags.Set(autoEvent.completeFlag);
        if (autoEvent.changeChapter)
            ChapterManager.SetChapter(autoEvent.targetChapter);
        TryRunAutoEvent();
    }

    // 현재 장소의 이동 버튼을 다시 만든다. 아직 해금되지 않은 장소나 플래그 조건이 안 된 버튼은 만들지 않는다.
    private void RefreshExits()
    {
        foreach (Button button in spawnedButtons)
            Destroy(button.gameObject);
        spawnedButtons.Clear();

        foreach (LocationExit exit in CurrentLocation.Exits)
        {
            if (IsExitAvailable(exit))
                spawnedButtons.Add(CreateExitButton(exit));
        }
    }

    private static bool IsExitAvailable(LocationExit exit)
    {
        if (exit.target != null && !ChapterManager.IsReached(exit.target.UnlockChapter))
            return false;
        return string.IsNullOrWhiteSpace(exit.requiredFlag) || GameFlags.Has(exit.requiredFlag);
    }

    // 현재 장소의 Hotspot을 조건(canInspect, 챕터, requiredFlag)에 맞게 다시 만든다.
    private void RefreshHotspots()
    {
        foreach (Button button in hotspotButtons)
            Destroy(button.gameObject);
        hotspotButtons.Clear();

        foreach (LocationHotspot hotspot in CurrentLocation.Hotspots)
        {
            if (IsHotspotAvailable(hotspot) && !HasSceneObject(hotspot))
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
        if (!string.IsNullOrWhiteSpace(hotspot.hiddenIfFlag) && GameFlags.Has(hotspot.hiddenIfFlag))
            return false;
        return string.IsNullOrWhiteSpace(hotspot.requiredFlag) || GameFlags.Has(hotspot.requiredFlag);
    }

    private void OnFlagChanged(string id, bool value)
    {
        if (CurrentLocation == null)
            return;
        RefreshExits();
        RefreshHotspots();
        TryRunAutoEvent();
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

        button.onClick.AddListener(() => Inspect(hotspot));
        return button;
    }

    private void Inspect(LocationHotspot hotspot)
    {
        // 대화 도중 켜진 플래그로 조건이 맞은 Auto Event도 여기서 이어서 실행된다
        System.Action onComplete = () =>
        {
            GameFlags.Set(hotspot.setFlagOnComplete);
            TryRunAutoEvent();
        };
        if (hotspot.startsEnding)
            endingManager.StartEnding();
        else if (hotspot.memoryEvent != null)
            memoryPanel.Play(hotspot.memoryEvent, onComplete);
        else if (hotspot.dialogue != null)
            dialoguePanel.Play(hotspot.dialogue, onComplete);
        else
            inspectPanel.Show(hotspot, onComplete);
    }

    // ---- 장소 오브젝트(LocationObjectInteraction)에서 호출 ----

    // 현재 장소의 Hotspot을 조사할 수 있는 상태인지 (조건 충족 + 다른 창이 열려 있지 않음)
    public bool CanInspect(string hotspotName)
    {
        LocationHotspot hotspot = FindHotspot(hotspotName);
        return hotspot != null && IsHotspotAvailable(hotspot) && !IsBusy();
    }

    public void InspectByName(string hotspotName)
    {
        if (CanInspect(hotspotName))
            Inspect(FindHotspot(hotspotName));
    }

    private LocationHotspot FindHotspot(string hotspotName)
    {
        if (CurrentLocation == null || string.IsNullOrEmpty(hotspotName))
            return null;
        foreach (LocationHotspot hotspot in CurrentLocation.Hotspots)
        {
            if (hotspot.displayName == hotspotName)
                return hotspot;
        }
        return null;
    }

    private bool IsBusy()
    {
        return (dialoguePanel != null && dialoguePanel.IsPlaying)
            || (inspectPanel != null && inspectPanel.IsOpen)
            || (memoryPanel != null && memoryPanel.IsPlaying);
    }

    // 현재 장소의 오브젝트 그림에 연결된 Hotspot인지 (그렇다면 사각형 클릭 영역을 만들지 않는다)
    private bool HasSceneObject(LocationHotspot hotspot)
    {
        foreach (LocationObjectGroup group in locationObjects)
        {
            if (group.location != CurrentLocation || group.root == null)
                continue;
            foreach (LocationObjectInteraction obj in group.root.GetComponentsInChildren<LocationObjectInteraction>(true))
            {
                if (obj.HotspotName == hotspot.displayName)
                    return true;
            }
        }
        return false;
    }
}

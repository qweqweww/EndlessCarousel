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

    private readonly List<Button> spawnedButtons = new List<Button>();

    public LocationData CurrentLocation { get; private set; }

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

        foreach (Button button in spawnedButtons)
            Destroy(button.gameObject);
        spawnedButtons.Clear();

        foreach (LocationExit exit in location.Exits)
            spawnedButtons.Add(CreateExitButton(exit));
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
}

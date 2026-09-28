using System.Collections.Generic;
using UnityEngine;

// 장소 하나의 콘텐츠(이름, 배경, 연결된 장소). 코드 수정 없이 에셋만 추가/수정해서 장소를 만든다.
[CreateAssetMenu(fileName = "NewLocation", menuName = "EndlessCarousel/Location Data")]
public class LocationData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite background;
    [SerializeField] private List<LocationExit> exits = new List<LocationExit>();
    [SerializeField] private List<LocationHotspot> hotspots = new List<LocationHotspot>();

    public string DisplayName => displayName;
    public Sprite Background => background;
    public IReadOnlyList<LocationExit> Exits => exits;
    public IReadOnlyList<LocationHotspot> Hotspots => hotspots;
}

[System.Serializable]
public class LocationExit
{
    [Tooltip("이동 버튼에 표시할 글자")]
    public string label;

    [Tooltip("이동할 장소")]
    public LocationData target;

    [Tooltip("버튼 위치 (화면 비율, 0~1). (0,0)=왼쪽 아래, (1,1)=오른쪽 위")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.1f);
}

// 장소 화면 안의 조사 가능한 영역 하나. 피아노, 사진 등 모든 조사 대상이 이 형식을 공유한다.
[System.Serializable]
public class LocationHotspot
{
    [Tooltip("조사 창 제목")]
    public string displayName;

    [Tooltip("조사 창 본문")]
    [TextArea(2, 6)] public string inspectText;

    [Tooltip("조사 창에 띄울 이미지 (비워두면 이미지 없이 표시)")]
    public Sprite inspectImage;

    [Tooltip("끄면 화면에 클릭 영역이 생기지 않는다")]
    public bool canInspect = true;

    [Tooltip("클릭 영역 중심 위치 (화면 비율, 0~1). (0,0)=왼쪽 아래, (1,1)=오른쪽 위")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.5f);

    [Tooltip("클릭 영역 크기 (1920x1080 기준 픽셀)")]
    public Vector2 size = new Vector2(200f, 200f);

    [Tooltip("지정하면 조사창 대신 이 대화를 시작한다 (캐릭터 등). 비워두면 조사창을 연다")]
    public DialogueData dialogue;

    [Tooltip("지정하면 조사창/대화 대신 이 기억 이벤트를 시작한다 (Dialogue보다 우선)")]
    public MemoryEventData memoryEvent;

    [Tooltip("이 플래그가 켜져 있어야 화면에 나타난다. 비워두면 조건 없음")]
    public string requiredFlag;

    [Tooltip("조사창을 닫거나 대화가 끝까지 진행된 뒤 켤 플래그. 비워두면 설정하지 않음")]
    public string setFlagOnComplete;
}

using System.Collections.Generic;
using UnityEngine;

// 장소 하나의 콘텐츠(이름, 배경, 연결된 장소). 코드 수정 없이 에셋만 추가/수정해서 장소를 만든다.
[CreateAssetMenu(fileName = "NewLocation", menuName = "EndlessCarousel/Location Data")]
public class LocationData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite background;
    [SerializeField] private List<LocationExit> exits = new List<LocationExit>();

    public string DisplayName => displayName;
    public Sprite Background => background;
    public IReadOnlyList<LocationExit> Exits => exits;
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

using UnityEngine;

// 기억 이벤트 하나. CG를 띄운 채 기존 DialogueData를 재생한다. 기억마다 스크립트를 만들지 않고 이 에셋만 추가한다.
[CreateAssetMenu(fileName = "NewMemoryEvent", menuName = "EndlessCarousel/Memory Event Data")]
public class MemoryEventData : ScriptableObject
{
    [Tooltip("기억 이벤트 이름 (화면 상단에 표시. 비워두면 표시하지 않음)")]
    [SerializeField] private string eventName;

    [Tooltip("기억 중 화면 전체에 표시할 CG 또는 배경")]
    [SerializeField] private Sprite memoryImage;

    [Tooltip("기억 중 재생할 대화")]
    [SerializeField] private DialogueData dialogue;

    [Tooltip("기억이 끝난 뒤 켤 플래그. 비워두면 설정하지 않음")]
    [SerializeField] private string setFlagOnComplete;

    public string EventName => eventName;
    public Sprite MemoryImage => memoryImage;
    public DialogueData Dialogue => dialogue;
    public string SetFlagOnComplete => setFlagOnComplete;
}

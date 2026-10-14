using System.Collections.Generic;
using UnityEngine;

// 대화 하나(여러 줄의 대사). 대사 내용은 이 에셋에서만 편집하고 코드에는 쓰지 않는다.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "EndlessCarousel/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    [Tooltip("마지막 줄이 끝난 뒤 이어서 재생할 대화 (분기 후 공통 대사로 합류할 때 사용). 비워두면 대화 종료")]
    [SerializeField] private DialogueData nextDialogue;

    [Tooltip("이 플래그가 켜져 있으면 Next Dialogue로 이어가지 않는다 (처음 한 번만 이어지는 후속 대화용). 비워두면 항상 이어감")]
    [SerializeField] private string skipNextIfFlag;

    public IReadOnlyList<DialogueLine> Lines => lines;
    public DialogueData NextDialogue =>
        !string.IsNullOrWhiteSpace(skipNextIfFlag) && GameFlags.Has(skipNextIfFlag) ? null : nextDialogue;
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("화자 이름 (비워두면 이름 칸을 숨긴다: 나레이션용)")]
    public string speakerName;

    [TextArea(2, 6)] public string text;

    [Tooltip("이 대사에서 보여줄 캐릭터 이미지. 표정마다 다른 Sprite를 지정한다. 비워두면 이미지 없이 표시")]
    public Sprite portrait;

    [Tooltip("이 대사가 나오는 동안 화면 중앙에 크게 띄울 이미지 (사진 등). 연속된 줄에 같은 이미지를 넣으면 계속 떠 있고, 이미지가 없는 줄이 나오면 자동으로 사라진다")]
    public Sprite previewImage;

    [Tooltip("비워두면 클릭으로 다음 줄 진행. 항목이 있으면 이 대사를 보여준 뒤 선택지를 띄운다")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("선택지 버튼 문구")]
    public string text;

    [Tooltip("선택 후 재생할 대화. 비워두면 현재 대화의 다음 줄로 계속한다")]
    public DialogueData next;

    [Tooltip("선택 결과를 구분하는 ID")]
    public string choiceId;

    [Tooltip("켜면 이 선택지를 골랐을 때 Choice Id를 플래그로 기록한다")]
    public bool recordAsFlag;
}

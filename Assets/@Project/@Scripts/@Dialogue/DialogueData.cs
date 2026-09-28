using System.Collections.Generic;
using UnityEngine;

// 대화 하나(여러 줄의 대사). 대사 내용은 이 에셋에서만 편집하고 코드에는 쓰지 않는다.
[CreateAssetMenu(fileName = "NewDialogue", menuName = "EndlessCarousel/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("화자 이름 (비워두면 이름 칸을 숨긴다: 나레이션용)")]
    public string speakerName;

    [TextArea(2, 6)] public string text;

    [Tooltip("이 대사에서 보여줄 캐릭터 이미지. 표정마다 다른 Sprite를 지정한다. 비워두면 이미지 없이 표시")]
    public Sprite portrait;
}

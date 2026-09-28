using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임 전체가 공유하는 대화창. 디자인은 Prefab에서, 내용은 DialogueData에서 받는다.
public class DialoguePanel : MonoBehaviour
{
    [SerializeField] private GameObject nameBox;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;

    [Tooltip("화면 전체를 덮는 버튼. 클릭하면 다음 대사로 넘어가고, 뒤쪽 UI 클릭을 막는다")]
    [SerializeField] private Button advanceButton;

    private DialogueData currentDialogue;
    private int lineIndex;

    public bool IsPlaying => currentDialogue != null;

    private void Awake()
    {
        advanceButton.onClick.AddListener(Next);
    }

    public void Play(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.Lines.Count == 0)
        {
            Debug.LogWarning("[DialoguePanel] 재생할 대사가 없습니다.", this);
            return;
        }

        currentDialogue = dialogue;
        lineIndex = 0;
        gameObject.SetActive(true);
        ShowLine(currentDialogue.Lines[lineIndex]);
    }

    public void Next()
    {
        if (currentDialogue == null)
            return;

        lineIndex++;
        if (lineIndex >= currentDialogue.Lines.Count)
        {
            Close();
            return;
        }

        ShowLine(currentDialogue.Lines[lineIndex]);
    }

    public void Close()
    {
        currentDialogue = null;
        gameObject.SetActive(false);
    }

    private void ShowLine(DialogueLine line)
    {
        bool hasSpeaker = !string.IsNullOrEmpty(line.speakerName);
        nameBox.SetActive(hasSpeaker);
        speakerNameText.text = line.speakerName;

        dialogueText.text = line.text;

        portraitImage.sprite = line.portrait;
        portraitImage.gameObject.SetActive(line.portrait != null);
    }
}

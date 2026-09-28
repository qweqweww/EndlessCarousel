using System.Collections.Generic;
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
    [SerializeField] private GameObject nextIndicator;

    [Tooltip("화면 전체를 덮는 버튼. 클릭하면 다음 대사로 넘어가고, 뒤쪽 UI 클릭을 막는다")]
    [SerializeField] private Button advanceButton;

    [Header("선택지")]
    [SerializeField] private RectTransform choiceParent;
    [SerializeField] private Button choiceButtonPrefab;

    // 선택 결과를 받을 곳 (추후 플래그 시스템에서 구독)
    public event System.Action<DialogueChoice> ChoiceSelected;

    private const int MaxDialogueHops = 32;

    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueData currentDialogue;
    private int lineIndex;
    private System.Action onFinished;

    public bool IsPlaying => currentDialogue != null;
    private bool IsChoosing => choiceButtons.Count > 0;

    private void Awake()
    {
        advanceButton.onClick.AddListener(Next);
    }

    // onFinished: 대화가 마지막 줄까지 진행되어 끝났을 때 한 번 호출 (Close로 중단하면 호출하지 않음)
    public void Play(DialogueData dialogue, System.Action onFinished = null)
    {
        if (dialogue == null)
        {
            Debug.LogWarning("[DialoguePanel] 재생할 대화가 없습니다.", this);
            return;
        }

        this.onFinished = onFinished;
        gameObject.SetActive(true);
        ContinueFrom(dialogue, 0);
    }

    public void Next()
    {
        if (currentDialogue == null || IsChoosing)
            return;

        ContinueFrom(currentDialogue, lineIndex + 1);
    }

    public void Close()
    {
        ClearChoices();
        currentDialogue = null;
        onFinished = null;
        gameObject.SetActive(false);
    }

    // dialogue의 index번째 줄부터 이어간다. 줄이 끝나면 Next Dialogue로, 그것도 없으면 대화를 닫는다.
    private void ContinueFrom(DialogueData dialogue, int index)
    {
        for (int hops = 0; dialogue != null && index >= dialogue.Lines.Count; hops++)
        {
            if (hops >= MaxDialogueHops)
            {
                Debug.LogWarning("[DialoguePanel] Next Dialogue 연결이 순환하고 있습니다.", dialogue);
                dialogue = null;
                break;
            }
            dialogue = dialogue.NextDialogue;
            index = 0;
        }

        if (dialogue == null)
        {
            System.Action callback = onFinished;
            Close();
            callback?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        lineIndex = index;
        ShowLine(dialogue.Lines[index]);
    }

    private void ShowLine(DialogueLine line)
    {
        bool hasSpeaker = !string.IsNullOrEmpty(line.speakerName);
        nameBox.SetActive(hasSpeaker);
        speakerNameText.text = line.speakerName;

        dialogueText.text = line.text;

        portraitImage.sprite = line.portrait;
        portraitImage.gameObject.SetActive(line.portrait != null);

        ClearChoices();
        foreach (DialogueChoice choice in line.choices)
            choiceButtons.Add(CreateChoiceButton(choice));

        if (nextIndicator != null)
            nextIndicator.SetActive(!IsChoosing);
    }

    private Button CreateChoiceButton(DialogueChoice choice)
    {
        Button button = Instantiate(choiceButtonPrefab, choiceParent);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = choice.text;

        button.onClick.AddListener(() => Choose(choice));
        return button;
    }

    private void Choose(DialogueChoice choice)
    {
        if (!IsChoosing)
            return;

        ClearChoices();
        if (choice.recordAsFlag)
            GameFlags.Set(choice.choiceId);
        ChoiceSelected?.Invoke(choice);

        if (choice.next != null)
            ContinueFrom(choice.next, 0);
        else
            ContinueFrom(currentDialogue, lineIndex + 1);
    }

    private void ClearChoices()
    {
        foreach (Button button in choiceButtons)
            Destroy(button.gameObject);
        choiceButtons.Clear();
    }
}

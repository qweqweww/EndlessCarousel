using System.Collections;
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

    [Header("타자 효과 (초)")]
    [Tooltip("한 글자가 나타나는 간격. 0이면 문장을 바로 전부 표시")]
    [SerializeField] private float characterInterval = 0.035f;

    [Header("▼ 연출 (px / 초)")]
    [Tooltip("대기 중 위아래로 떠오르는 높이")]
    [SerializeField] private float indicatorBobDistance = 6f;
    [Tooltip("대기 중 한 번 왕복하는 시간")]
    [SerializeField] private float indicatorBobDuration = 0.9f;
    [Tooltip("클릭 시 아래로 내려가는 거리 (위로 살짝 올라가는 높이는 이 값의 30%)")]
    [SerializeField] private float indicatorClickDropDistance = 12f;
    [SerializeField] private float indicatorClickDuration = 0.14f;

    // 선택 결과를 받을 곳 (추후 플래그 시스템에서 구독)
    public event System.Action<DialogueChoice> ChoiceSelected;

    private const int MaxDialogueHops = 32;
    private const int ShowAllCharacters = 99999; // TMP maxVisibleCharacters 기본값

    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueData currentDialogue;
    private int lineIndex;
    private System.Action onFinished;
    private Coroutine typing;

    // ▼ 상태. 위치를 움직이는 주체는 상태마다 하나뿐이다 (Idle = Update, ClickAnimation = 클릭 코루틴)
    private enum IndicatorState { Hidden, Idle, ClickAnimation }

    private RectTransform indicatorRect;
    private Vector2 indicatorBasePosition;
    private IndicatorState indicatorState;
    private float indicatorIdleStartTime;
    private Coroutine indicatorClick;

    public bool IsPlaying => currentDialogue != null;
    private bool IsChoosing => choiceButtons.Count > 0;

    // 전체 화면 CG가 떠 있는 동안 초상화만 숨긴다 (대사 데이터의 Portrait는 그대로 둔다)
    private bool portraitSuppressed;

    public void SetPortraitSuppressed(bool suppressed)
    {
        portraitSuppressed = suppressed;
        if (portraitImage != null)
            portraitImage.gameObject.SetActive(!suppressed && portraitImage.sprite != null && IsPlaying);
    }

    private void Awake()
    {
        advanceButton.onClick.AddListener(Next);
        if (nextIndicator != null)
        {
            indicatorRect = (RectTransform)nextIndicator.transform;
            indicatorBasePosition = indicatorRect.anchoredPosition;
        }
        SetIndicatorState(IndicatorState.Hidden);
    }

    private void OnDisable()
    {
        // 비활성화되면 코루틴이 멈추므로 상태를 처음으로 되돌린다
        typing = null;
        indicatorClick = null;
        SetIndicatorState(IndicatorState.Hidden);
    }

    // Idle일 때만 위아래로 떠 있는다 (TutorialPanel과 같은 계산: cos 곡선 = Ease In/Out)
    private void Update()
    {
        if (indicatorState != IndicatorState.Idle || indicatorBobDuration <= 0f)
            return;
        float phase = (Time.unscaledTime - indicatorIdleStartTime) / indicatorBobDuration * 2f * Mathf.PI;
        float offset = indicatorBobDistance * 0.5f * (1f - Mathf.Cos(phase));
        indicatorRect.anchoredPosition = indicatorBasePosition + new Vector2(0f, offset);
    }

    // ▼ 상태는 여기서만 바꾼다. 진행 중인 클릭 연출은 다른 상태로 바뀔 때 항상 멈추고 기본 위치로 돌아간다.
    private void SetIndicatorState(IndicatorState state)
    {
        if (nextIndicator == null)
            return;

        if (indicatorClick != null && state != IndicatorState.ClickAnimation)
        {
            StopCoroutine(indicatorClick);
            indicatorClick = null;
        }

        indicatorState = state;
        switch (state)
        {
            case IndicatorState.Hidden:
                indicatorRect.anchoredPosition = indicatorBasePosition;
                nextIndicator.SetActive(false);
                break;
            case IndicatorState.Idle:
                indicatorRect.anchoredPosition = indicatorBasePosition;
                indicatorIdleStartTime = Time.unscaledTime;
                nextIndicator.SetActive(true);
                break;
            case IndicatorState.ClickAnimation:
                nextIndicator.SetActive(true);
                indicatorClick = StartCoroutine(PlayIndicatorClick());
                break;
        }
    }

    // 현재 위치에서 살짝 위로(Ease Out) → 아래로(Ease In) 내려간 뒤 숨긴다. 다음 대사는 기다리지 않고 바로 시작된다.
    private IEnumerator PlayIndicatorClick()
    {
        Vector2 start = indicatorRect.anchoredPosition;
        Vector2 top = start + new Vector2(0f, indicatorClickDropDistance * 0.3f);
        Vector2 bottom = start - new Vector2(0f, indicatorClickDropDistance);
        float upTime = indicatorClickDuration * 0.35f;
        float downTime = indicatorClickDuration - upTime;

        for (float t = 0f; t < upTime; t += Time.unscaledDeltaTime)
        {
            float k = t / upTime;
            indicatorRect.anchoredPosition = Vector2.Lerp(start, top, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        for (float t = 0f; t < downTime; t += Time.unscaledDeltaTime)
        {
            float k = t / downTime;
            indicatorRect.anchoredPosition = Vector2.Lerp(top, bottom, k * k);
            yield return null;
        }

        indicatorClick = null;
        SetIndicatorState(IndicatorState.Hidden);
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

    // 출력 중이면 문장을 즉시 완성하고, 다 나온 상태면 바로 다음 대사로 넘어간다 (강제 대기 없음)
    public void Next()
    {
        if (currentDialogue == null)
            return;

        if (typing != null)
        {
            FinishLineDisplay();
            return;
        }
        if (IsChoosing)
            return;

        if (indicatorState == IndicatorState.Idle)
            SetIndicatorState(IndicatorState.ClickAnimation);
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

        portraitImage.sprite = line.portrait;
        portraitImage.gameObject.SetActive(line.portrait != null && !portraitSuppressed);

        ClearChoices();

        // 새 줄은 ▼를 숨긴 상태로 시작한다. 방금 시작된 클릭 연출은 그대로 내려가며 사라지게 둔다.
        if (indicatorState != IndicatorState.ClickAnimation)
            SetIndicatorState(IndicatorState.Hidden);

        // 글자는 숨겨두고 한 글자씩 보인다. 선택지는 문장이 다 나온 뒤에 띄운다.
        if (typing != null)
            StopCoroutine(typing);
        typing = null;
        dialogueText.text = line.text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        if (characterInterval > 0f && dialogueText.textInfo.characterCount > 0)
            typing = StartCoroutine(Type(dialogueText.textInfo.characterCount));
        else
            FinishLineDisplay();
    }

    private IEnumerator Type(int total)
    {
        float elapsed = 0f;
        int shown = 0;
        while (shown < total)
        {
            elapsed += Time.unscaledDeltaTime;
            shown = Mathf.Min(total, 1 + (int)(elapsed / characterInterval));
            dialogueText.maxVisibleCharacters = shown;
            yield return null;
        }
        typing = null;
        FinishLineDisplay();
    }

    // 현재 줄을 '전부 표시된 상태'로 만든다. 타이핑이 자연스럽게 끝났을 때와 클릭으로 완성했을 때 모두 여기를 거친다.
    // 선택지가 있으면 선택지를, 없으면 ▼를 이 시점에 바로 띄운다.
    private void FinishLineDisplay()
    {
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
        }
        dialogueText.maxVisibleCharacters = ShowAllCharacters;
        if (!IsChoosing)
        {
            foreach (DialogueChoice choice in currentDialogue.Lines[lineIndex].choices)
                choiceButtons.Add(CreateChoiceButton(choice));
        }
        SetIndicatorState(IsChoosing ? IndicatorState.Hidden : IndicatorState.Idle);
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

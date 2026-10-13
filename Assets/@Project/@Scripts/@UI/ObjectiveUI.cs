using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 화면 구석에 현재 조사 목표를 작게 표시한다. 문구와 표시 조건(플래그)은 Inspector의 Steps에서 정한다.
// 다른 스크립트에서 SetObjective / Show / Hide로 직접 제어할 수도 있다.
public class ObjectiveUI : MonoBehaviour
{
    [System.Serializable]
    public class ObjectiveStep
    {
        [Tooltip("이 플래그가 켜져 있어야 표시한다. 비워두면 조건 없음")]
        public string requiredFlag;

        [Tooltip("이 플래그가 켜지면 이 문구는 끝난다 (다음 단계로 넘어가거나 숨김)")]
        public string endFlag;

        [TextArea(1, 3)] public string text;
    }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text objectiveText;

    [Tooltip("위에서부터 확인해 조건이 맞는 마지막 항목을 표시한다. 맞는 항목이 없으면 숨긴다")]
    [SerializeField] private List<ObjectiveStep> steps = new List<ObjectiveStep>();

    [Header("이 화면이 떠 있는 동안에는 잠시 숨긴다")]
    [SerializeField] private ChapterTitleCard chapterTitleCard;
    [SerializeField] private MemoryPanel memoryPanel;

    [Tooltip("문구가 바뀔 때 사라지는 시간과 나타나는 시간 (각각, 초)")]
    [SerializeField] private float fadeDuration = 0.15f;

    private string shownText;   // 화면에 적혀 있는 문구 (null이면 없음)
    private string targetText;  // 표시해야 할 문구 (null이면 숨김)
    private bool hiddenByCall;  // Hide()로 숨긴 상태

    public void SetObjective(string text)
    {
        targetText = string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public void Show()
    {
        hiddenByCall = false;
    }

    public void Hide()
    {
        hiddenByCall = true;
    }

    private void OnEnable()
    {
        GameFlags.Changed += OnFlagChanged;
        RefreshFromSteps();
    }

    private void OnDisable()
    {
        GameFlags.Changed -= OnFlagChanged;
    }

    private void Start()
    {
        // 처음에는 보이지 않은 상태에서 나타난다
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void OnFlagChanged(string id, bool value)
    {
        RefreshFromSteps();
    }

    private void RefreshFromSteps()
    {
        if (steps.Count == 0)
            return;
        string found = null;
        foreach (ObjectiveStep step in steps)
        {
            bool started = string.IsNullOrWhiteSpace(step.requiredFlag) || GameFlags.Has(step.requiredFlag);
            if (started && !GameFlags.Has(step.endFlag))
                found = step.text;
        }
        SetObjective(found);
    }

    // 문구가 바뀌면: 사라짐 → 문구 교체 → 나타남. 챕터 제목 / 기억 화면 중에는 사라진 상태로 둔다.
    private void Update()
    {
        float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;

        if (shownText != targetText)
        {
            if (group.alpha > 0f && shownText != null)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, step);
                return;
            }
            shownText = targetText;
            if (shownText != null)
                objectiveText.text = shownText;
        }

        bool visible = shownText != null && !hiddenByCall && !IsCovered();
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, step);
    }

    private bool IsCovered()
    {
        return (chapterTitleCard != null && chapterTitleCard.IsShowing)
            || (memoryPanel != null && memoryPanel.IsPlaying);
    }
}

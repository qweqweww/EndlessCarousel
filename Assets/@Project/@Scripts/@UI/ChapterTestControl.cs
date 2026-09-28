using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 개발 테스트용: 현재 챕터를 표시하고 버튼으로 다음 챕터로 넘긴다. 실제 진행에서는 이벤트가 ChapterManager를 호출한다.
public class ChapterTestControl : MonoBehaviour
{
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private Button nextChapterButton;

    [Tooltip("{0} 자리에 현재 챕터가 들어간다")]
    [SerializeField] private string labelFormat = "[TEST] {0}";

    private void OnEnable()
    {
        ChapterManager.Changed += OnChapterChanged;
        UpdateLabel();
    }

    private void OnDisable()
    {
        ChapterManager.Changed -= OnChapterChanged;
    }

    private void Start()
    {
        nextChapterButton.onClick.AddListener(ChapterManager.AdvanceToNext);
    }

    private void OnChapterChanged(Chapter chapter)
    {
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (chapterText != null)
            chapterText.text = string.Format(labelFormat, ChapterManager.Current);
    }
}

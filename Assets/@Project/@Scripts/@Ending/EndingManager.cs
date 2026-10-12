using System.Collections;
using UnityEngine;

// 엔딩 진행: X의 마지막 선택 대화 → GameFlags로 판정 → 결과 화면. Chapter6의 마지막 이벤트에서 StartEnding()을 호출한다.
// Final Chapter에서는 챕터 진입 시 판정 결과를 플래그로 기록하고(대화 분기용), 마지막 플래그가 켜지면 타이틀로 돌아간다.
public class EndingManager : MonoBehaviour
{
    [SerializeField] private EndingConfig config;
    [SerializeField] private DialoguePanel dialoguePanel;
    [SerializeField] private EndingPanel endingPanel;

    [Header("Final Chapter")]
    [Tooltip("이 챕터가 되면 판정 결과를 플래그로 기록한다")]
    [SerializeField] private Chapter resultChapter = Chapter.Chapter6;
    [Tooltip("결과 플래그를 기록한 뒤 켜는 플래그 (Final Chapter 첫 이벤트의 조건)")]
    [SerializeField] private string resultReadyFlag = "Final_Started";
    [Tooltip("이 플래그가 켜지면 화면을 검게 덮고 Return Scene으로 돌아간다")]
    [SerializeField] private string gameEndFlag = "Final_Complete";
    [SerializeField] private string returnSceneName = "Title";
    [SerializeField] private ScreenFader screenFader;

    // 판정이 끝났을 때 (추후 에필로그 연출 연결용)
    public event System.Action<EndingResult> EndingDecided;

    public EndingResult LastResult { get; private set; }

    private void OnEnable()
    {
        ChapterManager.Changed += OnChapterChanged;
        GameFlags.Changed += OnFlagChanged;
    }

    private void OnDisable()
    {
        ChapterManager.Changed -= OnChapterChanged;
        GameFlags.Changed -= OnFlagChanged;
    }

    private void OnChapterChanged(Chapter chapter)
    {
        if (chapter == resultChapter && config != null)
            StartCoroutine(ApplyResultNextFrame());
    }

    // 같은 프레임에 챕터 제목이 먼저 표시되도록 한 프레임 뒤에 기록한다 (제목과 첫 대화가 겹치지 않게)
    private IEnumerator ApplyResultNextFrame()
    {
        yield return null;
        LastResult = config.Evaluate();
        config.ApplyResultFlags(LastResult);
        GameFlags.Set(resultReadyFlag);
        EndingDecided?.Invoke(LastResult);
    }

    private void OnFlagChanged(string id, bool value)
    {
        if (!value || string.IsNullOrWhiteSpace(gameEndFlag) || id != gameEndFlag.Trim())
            return;
        if (screenFader != null)
            screenFader.FadeToBlackAndLoad(returnSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(returnSceneName);
    }

    public void StartEnding()
    {
        if (config == null)
        {
            Debug.LogWarning("[EndingManager] EndingConfig가 지정되지 않았습니다.", this);
            return;
        }

        if (config.XFinalChoiceDialogue != null)
        {
            // 이전 엔딩 시도의 선택이 남지 않도록, 이번 선택 결과만 반영한다.
            GameFlags.Set(config.XSearchFlag, false);
            dialoguePanel.Play(config.XFinalChoiceDialogue, ShowResult);
        }
        else
            ShowResult();
    }

    public EndingResult Evaluate()
    {
        return config.Evaluate();
    }

    private void ShowResult()
    {
        LastResult = config.Evaluate();
        endingPanel.Show(LastResult, config);
        EndingDecided?.Invoke(LastResult);
    }
}

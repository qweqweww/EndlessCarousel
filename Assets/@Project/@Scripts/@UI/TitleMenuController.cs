using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면의 버튼 동작과 등장/퇴장 연출을 한곳에서 관리한다. 버튼 디자인과 배경은 씬/Prefab에서 교체한다.
public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Header("옵션 창")]
    [SerializeField] private TitleOptionPanel optionPanel;
    [SerializeField] private CanvasGroup optionGroup;
    [Tooltip("메뉴 ↔ 옵션 창 전환 시간 (초)")]
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Tooltip("시작하기를 눌렀을 때 열 씬 이름 (Build Settings에 등록되어 있어야 한다)")]
    [SerializeField] private string gameSceneName = "Main";

    [Tooltip("지정하면 검정으로 페이드 아웃한 뒤 씬을 연다. 비워두면 바로 연다")]
    [SerializeField] private ScreenFader screenFader;

    [Header("등장 / 퇴장 연출 (비워두면 해당 단계는 건너뛴다)")]
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup menuGroup;

    [Tooltip("등장: 배경 / 로고 / 메뉴의 시작 시점과 길이 (초)")]
    [SerializeField] private float backgroundDelay = 0.2f;
    [SerializeField] private float backgroundFadeIn = 0.7f;
    [SerializeField] private float logoDelay = 0.6f;
    [SerializeField] private float logoFadeIn = 0.6f;
    [SerializeField] private float menuDelay = 1.1f;
    [SerializeField] private float menuFadeIn = 0.5f;

    [Tooltip("시작하기: 메뉴 → 로고 순서로 사라진 뒤 화면을 검정으로 덮는다 (초)")]
    [SerializeField] private float menuFadeOut = 0.25f;
    [SerializeField] private float logoFadeOut = 0.4f;

    private bool starting;
    private bool optionOpen;
    private Coroutine panelTransition;

    private void Awake()
    {
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);
        if (optionButton != null)
            optionButton.onClick.AddListener(OpenOption);
        if (optionPanel != null)
            optionPanel.BackRequested += CloseOption;

        SetGroup(backgroundGroup, 0f);
        SetGroup(logoGroup, 0f);
        SetGroup(menuGroup, 0f);
        SetGroup(optionGroup, 0f);
        SetMenuInput(false);
        SetInput(optionGroup, false);
    }

    private void Update()
    {
        // Esc로 옵션 창 닫기
        if (optionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseOption();
    }

    public void OpenOption()
    {
        if (starting || optionOpen || optionPanel == null)
            return;
        optionOpen = true;
        optionPanel.Refresh();
        SwitchPanels(menuGroup, optionGroup);
    }

    public void CloseOption()
    {
        if (starting || !optionOpen)
            return;
        optionOpen = false;
        SwitchPanels(optionGroup, menuGroup);
    }

    // 한쪽을 사라지게 한 뒤 다른 쪽을 나타낸다. 전환 중에는 양쪽 모두 입력을 받지 않고,
    // 새 전환이 시작되면 이전 전환을 멈추고 현재 알파에서 이어간다.
    private void SwitchPanels(CanvasGroup from, CanvasGroup to)
    {
        if (panelTransition != null)
            StopCoroutine(panelTransition);
        panelTransition = StartCoroutine(SwitchRoutine(from, to));
    }

    private IEnumerator SwitchRoutine(CanvasGroup from, CanvasGroup to)
    {
        SetInput(from, false);
        SetInput(to, false);
        yield return FadeGroup(from, GetAlpha(from), 0f, 0f, panelFadeDuration * GetAlpha(from));
        yield return FadeGroup(to, GetAlpha(to), 1f, 0f, panelFadeDuration * (1f - GetAlpha(to)));
        SetInput(to, true);
        panelTransition = null;
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    // 검은 화면 → 배경 → 로고 → 메뉴. 메뉴가 다 나타난 뒤부터 조작할 수 있다.
    private IEnumerator PlayIntro()
    {
        StartCoroutine(FadeGroup(backgroundGroup, 0f, 1f, backgroundDelay, backgroundFadeIn));
        StartCoroutine(FadeGroup(logoGroup, 0f, 1f, logoDelay, logoFadeIn));
        yield return FadeGroup(menuGroup, 0f, 1f, menuDelay, menuFadeIn);
        if (!starting && !optionOpen)
            SetMenuInput(true);
    }

    public void StartGame()
    {
        // 페이드/씬 로드 중 중복 클릭 방지
        if (starting || optionOpen || !startButton.interactable)
            return;
        starting = true;

        startButton.interactable = false;
        quitButton.interactable = false;
        if (optionButton != null)
            optionButton.interactable = false;
        SetMenuInput(false);

        // 엔딩 후 타이틀로 돌아와 다시 시작할 때 이전 진행이 남지 않도록 처음 상태로 되돌린다
        GameFlags.ClearAll();
        ChapterManager.SetChapter(Chapter.Prologue);

        StopAllCoroutines();
        StartCoroutine(PlayOutroAndLoad());
    }

    private IEnumerator PlayOutroAndLoad()
    {
        StartCoroutine(FadeGroup(menuGroup, GetAlpha(menuGroup), 0f, 0f, menuFadeOut));
        yield return FadeGroup(logoGroup, GetAlpha(logoGroup), 0f, menuFadeOut * 0.5f, logoFadeOut);

        if (screenFader != null)
            screenFader.FadeToBlackAndLoad(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        if (starting || optionOpen)
            return;
#if UNITY_EDITOR
        Debug.Log("[TitleMenu] 게임 종료 (에디터에서는 종료되지 않습니다)");
#else
        Application.Quit();
#endif
    }

    private void SetMenuInput(bool enabled)
    {
        SetInput(menuGroup, enabled);
    }

    private static void SetInput(CanvasGroup group, bool enabled)
    {
        if (group == null)
            return;
        group.interactable = enabled;
        group.blocksRaycasts = enabled;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float delay, float duration)
    {
        if (group == null)
            yield break;
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            // Ease Out
            float k = t / duration;
            group.alpha = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        group.alpha = to;
    }

    private static void SetGroup(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    private static float GetAlpha(CanvasGroup group)
    {
        return group != null ? group.alpha : 0f;
    }
}

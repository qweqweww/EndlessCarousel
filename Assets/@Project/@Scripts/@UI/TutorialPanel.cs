using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화자 없이 게임 시스템이 설명하는 작은 안내창. 문구와 표시 조건은 Inspector에서 입력한다.
// 조건(플래그 + 장소 + 대화 중 아님)이 맞으면 한 번만 표시한다.
// 등장 Fade In → 한 글자씩 출력 → 클릭(출력 중이면 즉시 완성, 다 나왔으면 다음 페이지) → 마지막에 Fade Out 후 닫는다.
public class TutorialPanel : MonoBehaviour
{
    [Tooltip("켜고 끄는 창 본체. 화면 전체를 덮는 투명 버튼이며, 떠 있는 동안 뒤쪽 클릭을 막는다")]
    [SerializeField] private Button content;
    [Tooltip("Fade In/Out 되는 배경+텍스트 묶음 (Box의 CanvasGroup)")]
    [SerializeField] private CanvasGroup boxGroup;
    [SerializeField] private TMP_Text messageText;
    [Tooltip("다음으로 넘어갈 수 있을 때만 보이는 작은 표시 (▼). 비워두면 사용하지 않음")]
    [SerializeField] private GameObject nextIndicator;

    [TextArea(2, 4)]
    [SerializeField] private string[] pages;

    [Header("표시 조건")]
    [Tooltip("이 플래그가 켜진 뒤에만 표시")]
    [SerializeField] private string showAfterFlag;
    [Tooltip("마지막 페이지를 닫을 때 켜는 플래그. 이미 켜져 있으면 다시 표시하지 않는다")]
    [SerializeField] private string shownFlag;
    [Tooltip("이 장소에 있을 때만 표시. 비워두면 장소 상관없음")]
    [SerializeField] private LocationData location;
    [SerializeField] private LocationManager locationManager;
    [Tooltip("이 대화창이 재생 중이면 끝날 때까지 기다린다")]
    [SerializeField] private DialoguePanel dialoguePanel;

    [Header("연출 (초)")]
    [SerializeField] private float characterInterval = 0.035f;
    [SerializeField] private float panelFadeInDuration = 0.25f;
    [SerializeField] private float textFadeDuration = 0.1f;
    [SerializeField] private float panelFadeOutDuration = 0.2f;
    [Tooltip("문장이 다 표시된 뒤 이 시간 동안은 다음 페이지/종료 클릭을 받지 않는다 (연타로 스킵 방지)")]
    [SerializeField] private float advanceDelay = 0.5f;

    [Header("▼ 연출 (px / 초)")]
    [Tooltip("대기 중 위아래로 떠오르는 높이")]
    [SerializeField] private float indicatorBobDistance = 6f;
    [Tooltip("대기 중 한 번 왕복하는 시간")]
    [SerializeField] private float indicatorBobDuration = 0.9f;
    [Tooltip("클릭 시 아래로 내려가는 거리 (위로 살짝 올라가는 높이는 이 값의 30%)")]
    [SerializeField] private float indicatorClickDropDistance = 12f;
    [SerializeField] private float indicatorClickDuration = 0.14f;

    private const int ShowAllCharacters = 99999; // TMP maxVisibleCharacters 기본값

    private int pageIndex = -1; // 0 이상이면 표시 중
    private bool transitioning; // Fade In / 페이지 전환 / Fade Out 중에는 클릭 무시
    private Coroutine typing;
    private float advanceReadyTime; // 이 시각(unscaled) 이후의 클릭부터 다음으로 진행

    private RectTransform indicatorRect;
    private Vector2 indicatorBasePosition;
    private float indicatorShownTime;
    private bool indicatorClicking; // 클릭 연출 중에는 Update가 ▼를 건드리지 않는다

    private bool CanAdvance => !transitioning && typing == null && Time.unscaledTime >= advanceReadyTime;

    private void Awake()
    {
        content.onClick.AddListener(OnClick);
        content.gameObject.SetActive(false);
        if (nextIndicator != null)
        {
            indicatorRect = (RectTransform)nextIndicator.transform;
            indicatorBasePosition = indicatorRect.anchoredPosition;
        }
        SetIndicator(false);
    }

    private void Update()
    {
        if (pageIndex >= 0)
        {
            if (!indicatorClicking)
                UpdateIndicator();
            return;
        }
        if (IsReady())
            StartCoroutine(Open());
    }

    // 진행 가능할 때만 보이고, 보이는 동안 천천히 위아래로 떠 있는다 (cos 곡선 = Ease In/Out)
    private void UpdateIndicator()
    {
        SetIndicator(CanAdvance);
        if (indicatorRect == null || !nextIndicator.activeSelf || indicatorBobDuration <= 0f)
            return;
        float phase = (Time.unscaledTime - indicatorShownTime) / indicatorBobDuration * 2f * Mathf.PI;
        float offset = indicatorBobDistance * 0.5f * (1f - Mathf.Cos(phase));
        indicatorRect.anchoredPosition = indicatorBasePosition + new Vector2(0f, offset);
    }

    private bool IsReady()
    {
        if (pages == null || pages.Length == 0 || GameFlags.Has(shownFlag) || !GameFlags.Has(showAfterFlag))
            return false;
        if (location != null && (locationManager == null || locationManager.CurrentLocation != location))
            return false;
        return dialoguePanel == null || !dialoguePanel.IsPlaying;
    }

    private void OnClick()
    {
        if (transitioning)
            return;

        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
            ShowFullText();
            return;
        }
        if (!CanAdvance)
            return;

        transitioning = true;
        StartCoroutine(Advance());
    }

    // ▼ 클릭 연출 → 다음 페이지 또는 종료
    private IEnumerator Advance()
    {
        yield return PlayIndicatorClick();
        if (pageIndex + 1 < pages.Length)
            yield return ChangePage();
        else
            yield return Close();
    }

    // 현재 위치에서 살짝 위로(Ease Out) → 아래로(Ease In) 내려간 뒤 숨긴다
    private IEnumerator PlayIndicatorClick()
    {
        if (indicatorRect == null || !nextIndicator.activeSelf)
        {
            SetIndicator(false);
            yield break;
        }

        indicatorClicking = true;
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

        indicatorClicking = false;
        SetIndicator(false);
    }

    // 문장 전체를 보이고, 이 시점부터 Advance Delay를 시작한다
    private void ShowFullText()
    {
        messageText.maxVisibleCharacters = ShowAllCharacters;
        advanceReadyTime = Time.unscaledTime + advanceDelay;
    }

    // 켜질 때 Idle 시작 시각을 기록하고, 꺼질 때 항상 기본 위치로 되돌린다
    private void SetIndicator(bool visible)
    {
        if (nextIndicator == null || nextIndicator.activeSelf == visible)
            return;
        nextIndicator.SetActive(visible);
        indicatorShownTime = Time.unscaledTime;
        if (!visible && indicatorRect != null)
            indicatorRect.anchoredPosition = indicatorBasePosition;
    }

    private IEnumerator Open()
    {
        pageIndex = 0;
        transitioning = true;
        SetPage(pageIndex);
        boxGroup.alpha = 0f;
        content.gameObject.SetActive(true); // 입력 차단은 즉시
        yield return Fade(boxGroup, 0f, 1f, panelFadeInDuration);
        transitioning = false;
        typing = StartCoroutine(Type());
    }

    private IEnumerator ChangePage()
    {
        transitioning = true;
        for (float t = 0f; t < textFadeDuration; t += Time.unscaledDeltaTime)
        {
            messageText.alpha = 1f - t / textFadeDuration;
            yield return null;
        }
        pageIndex++;
        SetPage(pageIndex);
        transitioning = false;
        typing = StartCoroutine(Type());
    }

    private IEnumerator Close()
    {
        transitioning = true; // Fade Out이 끝날 때까지 Content가 계속 클릭을 막는다
        yield return Fade(boxGroup, 1f, 0f, panelFadeOutDuration);
        GameFlags.Set(shownFlag);
        content.gameObject.SetActive(false);
        enabled = false;
    }

    // 다음 페이지 문장을 넣고 글자는 숨겨둔다 (Type에서 하나씩 보인다)
    private void SetPage(int index)
    {
        messageText.text = pages[index];
        messageText.maxVisibleCharacters = 0;
        messageText.alpha = 1f;
    }

    private IEnumerator Type()
    {
        messageText.ForceMeshUpdate();
        int total = messageText.textInfo.characterCount;
        float elapsed = 0f;
        int shown = 0;
        while (shown < total)
        {
            elapsed += Time.unscaledDeltaTime;
            shown = characterInterval > 0f ? Mathf.Min(total, 1 + (int)(elapsed / characterInterval)) : total;
            messageText.maxVisibleCharacters = shown;
            yield return null;
        }
        ShowFullText();
        typing = null;
    }

    // Ease Out (처음 빠르고 끝에서 부드럽게 멈춤)
    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            group.alpha = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        group.alpha = to;
    }
}

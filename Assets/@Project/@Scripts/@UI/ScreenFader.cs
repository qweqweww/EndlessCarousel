using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// 화면 전체를 검정으로 덮었다가 걷어내는 단순한 페이드. 페이드 중(검정이 남아 있는 동안)에는 뒤쪽 UI 입력을 막는다.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float blackHoldDuration = 0.7f;
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("켜면 씬 시작 첫 프레임부터 검정 화면으로 시작한다 (씬 진입 시 배경이 먼저 보이는 것 방지)")]
    [SerializeField] private bool startBlack;

    [Tooltip("켜면 시작 시 Black Hold 후 자동으로 Fade In 한다")]
    [SerializeField] private bool fadeInOnStart;

    [Tooltip("Fade In이 끝났을 때 호출 (추후 프롤로그 첫 대화 시작 등)")]
    [SerializeField] private UnityEvent onFadeInComplete;

    public event System.Action FadeInCompleted;

    private CanvasGroup canvasGroup;
    private Coroutine running;

    public bool IsFading => running != null;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetAlpha(startBlack ? 1f : 0f);
    }

    private void Start()
    {
        if (fadeInOnStart)
            FadeFromBlack(blackHoldDuration);
    }

    public void FadeToBlack(System.Action onComplete = null)
    {
        Run(Fade(canvasGroup.alpha, 1f, 0f, fadeOutDuration, onComplete));
    }

    // holdDuration: 검정 상태를 유지한 뒤 페이드 인을 시작할 때까지의 시간
    public void FadeFromBlack(float holdDuration = 0f)
    {
        Run(Fade(canvasGroup.alpha, 0f, holdDuration, fadeInDuration, NotifyFadeInComplete));
    }

    public void FadeToBlackAndLoad(string sceneName)
    {
        FadeToBlack(() => SceneManager.LoadScene(sceneName));
    }

    private void Run(IEnumerator routine)
    {
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(routine);
    }

    private IEnumerator Fade(float from, float to, float delay, float duration, System.Action onComplete)
    {
        SetAlpha(from);
        canvasGroup.blocksRaycasts = true;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);

        running = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        // 완전히 투명할 때만 입력을 통과시킨다
        canvasGroup.blocksRaycasts = alpha > 0f;
    }

    private void NotifyFadeInComplete()
    {
        onFadeInComplete?.Invoke();
        FadeInCompleted?.Invoke();
    }
}

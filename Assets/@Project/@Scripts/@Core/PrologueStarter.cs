using System.Collections;
using UnityEngine;

// Main 씬 시작 페이드 인이 끝나면 프롤로그를 진행한다.
// 검은 화면 대사(Black Intro) → 오버레이 페이드 아웃 → 프롤로그 대화
// → (선택) 오버레이를 반쯤 어둡게 하고 기억 회상 대화 → 다시 밝게 → 회상 이후 대화 → 완료 플래그.
// 프롤로그 대화를 비워두면 아무것도 하지 않는다.
public class PrologueStarter : MonoBehaviour
{
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialoguePanel dialoguePanel;

    [Tooltip("화면이 검은 상태에서 재생할 대화. 비워두면 바로 오버레이를 걷고 프롤로그 대화를 시작")]
    [SerializeField] private DialogueData blackIntroDialogue;

    [Tooltip("장소 화면을 가리는 검은 오버레이. Black Intro 뒤 페이드 아웃되고, 기억 회상 때 다시 반투명하게 쓰인다. 비워두면 페이드 없이 진행")]
    [SerializeField] private CanvasGroup introOverlay;

    [Tooltip("검은 오버레이가 사라지는 시간(초)")]
    [SerializeField] private float overlayFadeDuration = 0.9f;

    [Tooltip("오버레이가 걷힌 뒤 재생할 프롤로그 대화. 비워두면 바로 탐색 상태로 시작")]
    [SerializeField] private DialogueData prologueDialogue;

    [Header("기억 회상 (선택)")]
    [Tooltip("프롤로그 대화가 끝난 뒤, 배경을 어둡게 한 상태로 재생할 대화. 비워두면 회상 없이 완료")]
    [SerializeField] private DialogueData recallDialogue;

    [Tooltip("회상이 끝나고 배경이 밝아진 뒤 재생할 대화")]
    [SerializeField] private DialogueData afterRecallDialogue;

    [Tooltip("회상 중 오버레이 불투명도 (0=투명, 1=완전 검정)")]
    [Range(0f, 1f)]
    [SerializeField] private float recallDimAlpha = 0.55f;

    [Tooltip("회상 시작/끝의 어두워짐·밝아짐 시간(초)")]
    [SerializeField] private float recallFadeDuration = 0.4f;

    [Header("완료")]
    [Tooltip("프롤로그 전체(회상 이후 대화 포함)가 끝났을 때 켤 플래그. 대화가 비어 있으면 설정하지 않는다")]
    [SerializeField] private string setFlagOnComplete;

    private void Awake()
    {
        // 첫 프레임부터 장소 화면이 보이지 않도록 가려둔다
        if (introOverlay != null && prologueDialogue != null)
        {
            introOverlay.gameObject.SetActive(true);
            introOverlay.alpha = 1f;
            introOverlay.blocksRaycasts = true;
        }
    }

    private void OnEnable()
    {
        if (screenFader != null)
            screenFader.FadeInCompleted += OnFadeInCompleted;
    }

    private void OnDisable()
    {
        if (screenFader != null)
            screenFader.FadeInCompleted -= OnFadeInCompleted;
    }

    private void OnFadeInCompleted()
    {
        if (prologueDialogue == null)
            return;

        if (blackIntroDialogue != null)
            dialoguePanel.Play(blackIntroDialogue, () => StartCoroutine(RunPrologue()));
        else
            StartCoroutine(RunPrologue());
    }

    private IEnumerator RunPrologue()
    {
        yield return FadeOverlay(1f, 0f, overlayFadeDuration);
        yield return PlayAndWait(prologueDialogue);

        if (recallDialogue != null)
        {
            yield return FadeOverlay(0f, recallDimAlpha, recallFadeDuration);
            yield return PlayAndWait(recallDialogue);
            yield return FadeOverlay(recallDimAlpha, 0f, recallFadeDuration);

            if (afterRecallDialogue != null)
                yield return PlayAndWait(afterRecallDialogue);
        }

        GameFlags.Set(setFlagOnComplete);
    }

    // 대화가 마지막 줄까지 끝날 때까지 기다린다
    private IEnumerator PlayAndWait(DialogueData dialogue)
    {
        bool finished = false;
        dialoguePanel.Play(dialogue, () => finished = true);
        while (!finished)
            yield return null;
    }

    // 오버레이 불투명도를 바꾼다. 페이드 중과 오버레이가 보이는 동안에는 뒤쪽 클릭을 막고, 투명해지면 끈다.
    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (introOverlay == null)
            yield break;

        introOverlay.gameObject.SetActive(true);
        introOverlay.blocksRaycasts = true;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            introOverlay.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        introOverlay.alpha = to;

        if (to <= 0f)
            introOverlay.gameObject.SetActive(false);
    }
}

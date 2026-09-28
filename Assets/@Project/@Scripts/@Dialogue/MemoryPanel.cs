using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 기억 이벤트 화면. CG를 띄우고 공통 DialoguePanel로 대화를 재생한 뒤, 끝나면 닫고 완료 플래그를 켠다.
[RequireComponent(typeof(CanvasGroup))]
public class MemoryPanel : MonoBehaviour
{
    [SerializeField] private Image memoryImage;
    [SerializeField] private TMP_Text eventNameText;
    [SerializeField] private DialoguePanel dialoguePanel;

    [Tooltip("페이드 인/아웃 시간(초). 0이면 즉시 전환")]
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private MemoryEventData currentEvent;
    private System.Action onFinished;

    public bool IsPlaying => currentEvent != null;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // onFinished: 기억이 끝나고 화면이 닫힌 뒤 한 번 호출
    public void Play(MemoryEventData memoryEvent, System.Action onFinished = null)
    {
        if (memoryEvent == null || IsPlaying)
            return;

        currentEvent = memoryEvent;
        this.onFinished = onFinished;

        memoryImage.sprite = memoryEvent.MemoryImage;
        if (eventNameText != null)
        {
            eventNameText.text = memoryEvent.EventName;
            eventNameText.gameObject.SetActive(!string.IsNullOrEmpty(memoryEvent.EventName));
        }

        gameObject.SetActive(true);
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(0f, 1f);

        if (currentEvent.Dialogue != null)
        {
            bool dialogueDone = false;
            dialoguePanel.Play(currentEvent.Dialogue, () => dialogueDone = true);
            while (!dialogueDone)
                yield return null;
        }
        else
        {
            Debug.LogWarning("[MemoryPanel] 기억 이벤트에 대화가 지정되지 않았습니다.", currentEvent);
        }

        yield return Fade(1f, 0f);

        string flag = currentEvent.SetFlagOnComplete;
        System.Action callback = onFinished;
        currentEvent = null;
        onFinished = null;
        gameObject.SetActive(false);

        GameFlags.Set(flag);
        callback?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}

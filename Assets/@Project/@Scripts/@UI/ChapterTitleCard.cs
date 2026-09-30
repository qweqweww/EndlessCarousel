using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 챕터가 바뀌면 제목을 잠깐 보여준다. 제목은 Inspector 목록에서 입력하며, 목록에 없는 챕터는 표시하지 않는다.
[RequireComponent(typeof(CanvasGroup))]
public class ChapterTitleCard : MonoBehaviour
{
    [System.Serializable]
    public class ChapterTitle
    {
        public Chapter chapter;
        public string line1;
        public string line2;
    }

    [SerializeField] private TMP_Text line1Text;
    [SerializeField] private TMP_Text line2Text;

    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float holdDuration = 2.0f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [SerializeField] private List<ChapterTitle> titles = new List<ChapterTitle>();

    private CanvasGroup canvasGroup;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        ChapterManager.Changed += OnChapterChanged;
    }

    private void OnDisable()
    {
        ChapterManager.Changed -= OnChapterChanged;
    }

    private void OnChapterChanged(Chapter chapter)
    {
        foreach (ChapterTitle title in titles)
        {
            if (title.chapter == chapter)
            {
                StopAllCoroutines();
                StartCoroutine(Show(title));
                return;
            }
        }
    }

    private IEnumerator Show(ChapterTitle title)
    {
        IsShowing = true;
        line1Text.text = title.line1;
        line2Text.text = title.line2;

        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);

        IsShowing = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        // 보이는 동안 뒤쪽 입력을 막는다
        canvasGroup.blocksRaycasts = alpha > 0f;
    }
}

using UnityEngine;

// Main 씬 시작 페이드 인이 끝나면 프롤로그 첫 대화를 재생한다. 대화를 비워두면 아무것도 하지 않는다.
public class PrologueStarter : MonoBehaviour
{
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialoguePanel dialoguePanel;

    [Tooltip("페이드 인 직후 재생할 프롤로그 대화. 비워두면 바로 탐색 상태로 시작")]
    [SerializeField] private DialogueData prologueDialogue;

    [Tooltip("프롤로그 대화가 마지막까지 끝났을 때 켤 플래그. 대화가 비어 있으면 설정하지 않는다")]
    [SerializeField] private string setFlagOnComplete;

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
        if (prologueDialogue != null)
            dialoguePanel.Play(prologueDialogue, () => GameFlags.Set(setFlagOnComplete));
    }
}

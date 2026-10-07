using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 개발 테스트 전용: 숫자키로 각 챕터의 "시작 상태"로 이동한다. (Editor / Development Build에서만 동작)
// 플래그를 모두 지운 뒤 이전 챕터까지 정상 진행했을 때 켜지는 플래그만 다시 켜고,
// 챕터를 바꿔 제목 카드를 띄운 다음 시작 장소로 이동한다. 반복해서 써도 이전 점프의 상태가 남지 않는다.
public class ChapterJumpDebug : MonoBehaviour
{
    [System.Serializable]
    private class ChapterJump
    {
        public Chapter chapter = Chapter.Chapter1;
        public Key key = Key.Digit1;
        public Key altKey = Key.Numpad1;
        [Tooltip("이 챕터가 시작될 때 플레이어가 있는 장소")]
        public LocationData startLocation;

        [Tooltip("이전 챕터까지 정상 진행했을 때 켜져 있는 플래그 (이 챕터 안에서 얻는 플래그는 넣지 않는다)")]
        public List<string> flags = new List<string>();
    }

    [SerializeField] private List<ChapterJump> jumps = new List<ChapterJump>();

    [Header("정리할 진행 중 연출")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private DialoguePanel dialoguePanel;
    [SerializeField] private MemoryPanel memoryPanel;
    [SerializeField] private InspectPanel inspectPanel;
    [SerializeField] private ChapterTitleCard chapterTitleCard;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private PrologueStarter prologueStarter;

    // 정식 빌드(Release)에서는 아래 입력 처리가 컴파일되지 않아 숫자키를 눌러도 아무 일도 일어나지 않는다
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        foreach (ChapterJump jump in jumps)
        {
            if (keyboard[jump.key].wasPressedThisFrame || (jump.altKey != Key.None && keyboard[jump.altKey].wasPressedThisFrame))
            {
                Jump(jump);
                return;
            }
        }
    }

    private void Jump(ChapterJump jump)
    {
        if (prologueStarter != null)
            prologueStarter.Cancel();
        StopRunningPresentation();

        // 진행 상태 초기화 후, 이전 챕터 완료 상태만 다시 만든다
        GameFlags.ClearAll();
        ChapterManager.SetChapter(Chapter.Prologue);
        foreach (string flag in jump.flags)
            GameFlags.Set(flag);

        // 플래그 변경으로 현재 장소의 Auto Event가 시작됐을 수 있으므로 한 번 더 정리
        StopRunningPresentation();

        ChapterManager.SetChapter(jump.chapter); // 실제 진행과 같이 제목 카드가 표시된다
        locationManager.MoveTo(jump.startLocation);
        Debug.Log($"[ChapterJump] {jump.chapter} 시작 상태로 이동 ({jump.startLocation.name}, 플래그 {jump.flags.Count}개)", this);
    }

    private void StopRunningPresentation()
    {
        if (dialoguePanel != null && dialoguePanel.IsPlaying)
            dialoguePanel.Close();
        if (memoryPanel != null && memoryPanel.IsPlaying)
            memoryPanel.StopImmediate();
        if (inspectPanel != null && inspectPanel.IsOpen)
            inspectPanel.Hide();
        if (chapterTitleCard != null)
            chapterTitleCard.HideImmediate();
        if (screenFader != null)
            screenFader.ClearImmediate();
    }
#endif
}

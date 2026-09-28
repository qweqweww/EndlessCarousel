using System;
using UnityEngine;

// 게임의 큰 진행 단계. 순서대로 진행되므로 비교(>=)로 "이 챕터 이후"를 판단한다.
public enum Chapter
{
    Prologue,
    Chapter1,
    Chapter2,
    Chapter3,
    Chapter4,
    Chapter5,
    Chapter6
}

// 현재 챕터를 관리한다. 세부 진행 상태는 GameFlags, 큰 진행 단계는 여기서 관리한다.
public static class ChapterManager
{
    public const Chapter FirstChapter = Chapter.Prologue;
    public const Chapter LastChapter = Chapter.Chapter6;

    public static Chapter Current { get; private set; } = FirstChapter;

    // 챕터가 바뀌었을 때 (새 챕터)
    public static event Action<Chapter> Changed;

    // 현재 챕터가 chapter에 도달했거나 지났는지
    public static bool IsReached(Chapter chapter)
    {
        return Current >= chapter;
    }

    public static void SetChapter(Chapter chapter)
    {
        if (chapter == Current)
            return;

        Current = chapter;
        Changed?.Invoke(chapter);
    }

    public static void AdvanceToNext()
    {
        if (Current < LastChapter)
            SetChapter(Current + 1);
    }

    // Play 시작마다 Prologue로 초기화 (도메인 리로드를 끈 설정 대비)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Current = FirstChapter;
        Changed = null;
    }
}

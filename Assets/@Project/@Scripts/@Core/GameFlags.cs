using System;
using System.Collections.Generic;
using UnityEngine;

// 진행 플래그 저장소. 문자열 ID로 켜고 끈다. 플래그 이름은 코드가 아닌 데이터(Inspector)에서 입력한다.
public static class GameFlags
{
    private static readonly HashSet<string> activeFlags = new HashSet<string>();

    // 플래그 값이 실제로 바뀌었을 때 (id, 새 값)
    public static event Action<string, bool> Changed;

    public static bool Has(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && activeFlags.Contains(id.Trim());
    }

    // 빈 ID는 무시한다. 그래서 데이터의 플래그 칸을 비워두면 아무 일도 일어나지 않는다.
    public static void Set(string id, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        id = id.Trim();
        bool changed = value ? activeFlags.Add(id) : activeFlags.Remove(id);
        if (changed)
            Changed?.Invoke(id, value);
    }

    // 저장용: 현재 켜진 플래그 목록 (불러올 때는 ClearAll 후 각각 Set)
    public static string[] GetActiveFlags()
    {
        string[] result = new string[activeFlags.Count];
        activeFlags.CopyTo(result);
        return result;
    }

    public static void ClearAll()
    {
        foreach (string id in GetActiveFlags())
            Set(id, false);
    }

    // Play 시작마다 초기화 (도메인 리로드를 끈 설정에서도 이전 플레이의 값이 남지 않게)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        activeFlags.Clear();
        Changed = null;
    }
}

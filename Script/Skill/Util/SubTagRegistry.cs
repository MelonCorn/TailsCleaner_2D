using System.Collections.Generic;
using UnityEngine;

public static class SubTagRegistry  // 서브 태그 ID -> Flag 변환기
{
    // Key : SubTag,     Value : Flag
    private static Dictionary<int, int> _subTagToFlag = new Dictionary<int, int>();

    // 다음 할당 비트 자리
    private static int _nextBitIndex = 0;

    // 서브태그 등록
    public static void Register(int subTag)
    {
        // 등록된 태그면 패스
        if (_subTagToFlag.ContainsKey(subTag))
            return;

        // int라 0 ~ 31 까지 long으로 변환 가능
        if (_nextBitIndex >= 32)
        {
            Debug.LogError($"[SubTagRegistry] 태그 등록 한계 초과");
            return;
        }

        // 비트 플래그 생성
        int flag = 1 << _nextBitIndex;

        // 서브 태그에 플래그 추가
        _subTagToFlag.Add(subTag, flag);

        // 다음 비트 자리
        _nextBitIndex++;
    }

    // 서브 태그를 Key로 사용해 플래그 반환
    // 등록되어 있지 않다면 0 반환
    public static int GetFlag(int subTag)
    {
        return _subTagToFlag.TryGetValue(subTag, out int flag) ? flag : 0;
    }
}

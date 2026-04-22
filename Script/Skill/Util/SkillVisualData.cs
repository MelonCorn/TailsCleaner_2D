using System.Collections.Generic;
using UnityEngine;

// 연출 액션
[System.Serializable]
public class VisualAction
{
    // 연출 타입
    public enum VISUALACTION_TYPE
    {
        PlaySprites,        // 스프라이트 순서대로 재생
        FadeIn,             // 알파값 0 -> 1 로 전환
        FadeOut,            // 알파값 1 -> 0 으로 전환
        ScaleUp,            // 스케일 0 -> 원본 크기로 전환
        ScaleDown,          // 스케일 원본 크기 -> 0 으로 전환
        FadeInAndScaleUp,   // 페이드인 + 확대
        FadeOutAndScaleDown,// 페이드아웃 + 축소 
        HitEffect,          // 이펙트 프리팹을 지정 위치에 생성
        AttachEffect,       // 궤적 이펙트를 자신에게 자식으로
    }

    public VISUALACTION_TYPE type;

    [Header("재생할 스프라이트")]
    public Sprite[] sprites;

    [Header("재생 시간")]
    public float duration = 0.1f;

    [Header("페이드용 커브")]
    public AnimationCurve curve;

    [Header("풀 오브젝트")]
    public PoolObject prefab;
    
    [Header("무한 반복")]
    public bool loop = false;

    [Header("초당 프레임(속도)")]
    public float fps = 5f;
}


// onActivate : 발동 연출
// onDuration : 유지 연출, duration=-1 이 남은 시간 채우기
// onExpire   : 종료 연출, 다 실행 후 콜백으로 풀 반환
// onHit      : 적 명중 시 적 위치에서 실행 (이펙트 스폰)
// 리스트를 비워두면 스킵
[CreateAssetMenu(menuName = "Skill/SkillVisualData")]
public class SkillVisualData : ScriptableObject
{
    [Header("발동 연출")]
    public List<VisualAction> onActivate = new();

    [Header("유지 연출")]
    public List<VisualAction> onDuration = new();

    [Header("종료 연출")]
    public List<VisualAction> onExpire = new();

    [Header("적중 연출")]
    public List<VisualAction> onHit = new();
}

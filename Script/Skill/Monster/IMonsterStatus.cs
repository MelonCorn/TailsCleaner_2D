using UnityEngine;

public interface IMonsterStatus
{
    /// <summary>
    /// 현재 약화 상태
    /// </summary>
    public bool IsWeakened { get; }

    /// <summary>
    /// 현재 기절 상태
    /// </summary>
    public bool IsStunned { get; }

    /// <summary>
    /// 현재 넉백 상태
    /// </summary>
    public bool IsKnockbacked { get; }

    /// <summary>
    /// 현재 최대 체력 감소 상태
    /// </summary>
    public bool HasReducedMaxHp { get; }

    /// <summary>
    /// 기절 장판 유지 시간
    /// </summary>
    public float StunAreaTime { get; }

    /// <summary>
    /// 시간제 슬로우 적용
    /// </summary>
    public void ApplySlow(string key, float amount, float duration);

    /// <summary>
    /// 슬로우 장판 입장
    /// </summary>
    public void EnterSlowArea(string key, float amount);

    /// <summary>
    /// 슬로우 장판 퇴장
    /// </summary>
    public void ExitSlowArea(string key);

    /// <summary>
    /// 기절 장판 입장
    /// </summary>
    public void EnterStunArea(float requiredTime, float duration);
    /// <summary>
    /// 기절 장판 퇴장
    /// </summary>
    public void ExitStunArea();

    /// <summary>
    /// 기절 시간 초기화
    /// </summary>
    public void ResetStunAreaTime();

    /// <summary>
    /// 기절 발동
    /// </summary>
    public void ApplyStun(float duration);

    /// <summary>
    /// 군중제어 발동 시 (SuperClean 패시브)
    /// </summary>
    public void OnCC();

    /// <summary>
    /// 넉백
    /// </summary>
    public void Knockback(Vector2 direction, float force);

    /// <summary>
    /// 최대 체력 감소 시도. 1회 한정.
    /// </summary>
    public void TryReduceMaxHp(float ratio);
}


public interface ISkillStat
{
    // MaxHp는 ISkillable에 추가하면될 것 같습니다.
    public int CurrentShield { get; }       // 현재 방어막
    public int MaxShield { get; }           // 최대 방어막

    /// <summary>
    /// 체력 회복.
    /// </summary>
    public void Heal(float amount);

    /// <summary>
    /// 방어막 추가.
    /// 방어막 있을 시 회피판정.
    /// </summary>
    public void AddShield(int count);

    /// <summary>
    /// 최대 방어막 설정.
    /// </summary>
    public void SetMaxShield(int maxShield);


    /// <summary>
    /// 스킬 스탯 갱신.
    /// flat  고정 스탯.
    /// multi 계수 스탯.
    /// cacheFlat.CopyFrom(flat) 처럼 값복사.
    /// 예) PlayerBase의 AttackPower 사용 시 (기존 플레이어 스탯 + flat.AttackPower) * multi.AttackPower 사실 계산 식 파악 안됨..
    /// </summary>
    public void SetSkillStat(PlayerStatFlat flat, PlayerStatMul multi);
}

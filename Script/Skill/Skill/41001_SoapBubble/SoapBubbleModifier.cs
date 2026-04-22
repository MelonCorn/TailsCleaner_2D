using UnityEngine;

public class SoapBubbleModifierData
{
    // 추적
    public bool Tracking = false;
    public float TrackingInterval = 0f;         // 탐색 간격

    // 플레이어 방어력
    public bool PlayerDefenseBoost = false;
    public float PlayerDefenseBonus = 0f;

    // 적 슬로우 효과
    public const string DEBUFF_KEY_SLOW = "SoapBubbleSlow";
    public bool SlowOnArea = false;
    public float SlowAmount = 0f;               

    // 적 기절 효과
    public bool StunOnArea = false;
    public float StunRequiredTime = 0f;         // 기절 발동 체류 시간
    public float StunDuration = 0f;             // 기절 지속 시간

    // 소멸 피해
    public bool BurstOnExpire = false;
    public float BurstDamage = 0f;              // 최대 체력 비율

}


// 40002 자동 추척 비누 지우개     /      장판 적 추적
public class SoapBubbleTrackingModifier : ActiveModifier<SoapBubbleSkill>
{
    [Header("추적 탐색 간격")]
    public float TrackingInterval = 0.2f;

    public override void ApplyModifier(SoapBubbleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.Tracking = true;
        skill._modifierData.TrackingInterval = TrackingInterval;
    }
}

// 40003 버블버블    /      장판 위 플레이어 방어력 버프
public class SoapBubblePlayerDefenseModifier : ActiveModifier<SoapBubbleSkill>
{
    [Header("방어력 증가량")]
    public float IncreasePlayerDefence = 1f;

    public override void ApplyModifier(SoapBubbleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.PlayerDefenseBoost = true;
        skill._modifierData.PlayerDefenseBonus = IncreasePlayerDefence;
    }
}


// 40004 빨래당함    /      장판 위 적 슬로우
public class SoapBubbleSlowModifier : ActiveModifier<SoapBubbleSkill>
{
    [Header("이동속도 감소율")]
    public float SlowAmount = 0.2f;

    public override void ApplyModifier(SoapBubbleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.SlowOnArea = true;
        skill._modifierData.SlowAmount = SlowAmount;
    }
}

// 40007 슬랩스틱    /      장판 위 일정 시간 체류 시 일정 시간 기절
public class SoapBubbleStunModifier : ActiveModifier<SoapBubbleSkill>
{
    [Header("기절 발동 체류 시간")]
    public float StunRequiredTime = 2f;
    [Header("기절 지속 시간")]
    public float StunDuration = 1f;

    public override void ApplyModifier(SoapBubbleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.StunOnArea = true;
        skill._modifierData.StunRequiredTime = StunRequiredTime;
        skill._modifierData.StunDuration = StunDuration;
    }
}

// 40008 거품 펑!    /      장판 소멸 시 최대 체력 피해
public class SoapBubbleBurstModifier : ActiveModifier<SoapBubbleSkill>
{
    [Header("소멸 시 최대 체력 피해")]
    public float BurstDamage = 0.01f;

    public override void ApplyModifier(SoapBubbleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.BurstOnExpire = true;
        skill._modifierData.BurstDamage = BurstDamage;
    }
}
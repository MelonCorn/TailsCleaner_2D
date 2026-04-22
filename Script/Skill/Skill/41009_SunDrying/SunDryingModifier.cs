using UnityEngine;

public class SunDryingModifierData
{
    // 따스한 태양 - 틱마다 피해 업그레이드 스탯 증가
    public bool DamagePerTick = false;
    public float DamagePerTickAmount = 0f;

    // 이불 털기 - 켜질 때 범위 내 적 넉백
    public bool KnockbackOnActivate = false;
    public float KnockbackForce = 0f;

    // 으슬으슬 - 시전 중 적 슬로우
    public bool SlowOnArea = false;
    public float SlowAmount = 0f;

    // 이불 두르기 - 꺼진 동안 방어력 증가
    public bool DefenseOnInactive = false;
    public int DefenseBonus = 0;

    // 두꺼운 이불 - 꺼질 때 방어막
    public bool ShieldOnDeactivate = false;

    // 기상! - 켜질 때 체력 회복
    public bool HealOnActivate = false;
    public float HealRatio = 0f;
}


// 40050 따스한 태양 / 틱마다 피해 증가
public class SunDryingDamagePerTickModifier : ActiveModifier<SunDryingSkill>
{
    [Header("틱당 추가 피해")]
    public float DamagePerTickAmount = 0.2f;

    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.DamagePerTick = true;
        skill._modifierData.DamagePerTickAmount += DamagePerTickAmount;
    }
}

// 40051 이불 털기 / 켜질 때 범위 내 적 넉백
public class SunDryingKnockbackModifier : ActiveModifier<SunDryingSkill>
{
    [Header("넉백 강도")]
    public float KnockBackForce = 1f;

    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.KnockbackOnActivate = true;
        skill._modifierData.KnockbackForce += KnockBackForce;
    }
}

// 40052 으슬으슬 / 시전 중 적 이동속도 감소
public class SunDryingSlowModifier : ActiveModifier<SunDryingSkill>
{
    [Header("이동속도 감소율")]
    public float SlowAmount = 0.2f;

    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.SlowOnArea = true;
        skill._modifierData.SlowAmount += SlowAmount;
    }
}

// 40053 이불 두르기 / 꺼진 동안 플레이어 방어력 증가
public class SunDryingDefenseModifier : ActiveModifier<SunDryingSkill>
{
    [Header("방어력 증가량")]
    public int DefenseBonus = 5;

    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.DefenseOnInactive = true;
        skill._modifierData.DefenseBonus += DefenseBonus;
    }
}

// 40054 두꺼운 이불 / 꺼질 때 방어막 생성
public class SunDryingShieldModifier : ActiveModifier<SunDryingSkill>
{
    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.ShieldOnDeactivate = true;
    }
}

// 40055 기상! / 켜질 때 체력 회복
public class SunDryingHealModifier : ActiveModifier<SunDryingSkill>
{
    [Header("체력 회복 비율")]
    public float HealRatio = 0.05f;

    public override void ApplyModifier(SunDryingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.HealOnActivate = true;
        skill._modifierData.HealRatio += HealRatio;
    }
}

using UnityEngine;

public class WashWaveModifierData
{
    // 밀물 썰물
    public bool HasReturn = false;
    public float ReturnDelay = 0f;

    // 탈수
    public bool DrainHp = false;
    public float DrainDelay = 0f;
    public float DrainAmount = 0f;

    // 차오르는 체력
    public bool HealOnCast = false;
    public float HealRatio = 0f;

    // 적 강화, 경험치 증가 (모든 업그레이드)
    public float EnemyStrengthBonus = 0f;   // 적 강화 수치
    public float ExpGainBonus = 0f;         // 경험치 증가량
}

// 40032 끌고오는 파도
// 적 강화 + 경험치 증가
public class WashWaveCurseModifier : ActiveModifier<WashWaveSkill>
{
    [Header("적 강화 수치")]
    public float EnemyStrengthBonus = 1f;
    [Header("경험치 증가량")]
    public float ExpGainBonus = 5f;

    public override void ApplyModifier(WashWaveSkill skill, ActiveUpgradeData upgradeData)
    {
        int level = skill.GetUpgradeLevel(upgradeData.Id);

        skill._modifierData.EnemyStrengthBonus += EnemyStrengthBonus * level;
        skill._modifierData.ExpGainBonus += ExpGainBonus * level;

        // 적 강화 적용
        // 경험치 증가 적용 
    }
}


// 40033 밀물 썰물
// 파도 왕복 + 적 강화 + 경험치 증가
public class WashWaveReturnModifier : ActiveModifier<WashWaveSkill>
{
    [Header("왕복 딜레이")]
    public float ReturnDelay = 2f;
    [Header("적 강화 수치")]
    public float EnemyStrengthBonus = 1f;
    [Header("경험치 증가량")]
    public float ExpGainBonus = 5f;

    public override void ApplyModifier(WashWaveSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.HasReturn = true;

        int level = skill.GetUpgradeLevel(upgradeData.Id);

        skill._modifierData.ReturnDelay = ReturnDelay;
        skill._modifierData.EnemyStrengthBonus += EnemyStrengthBonus * level;
        skill._modifierData.ExpGainBonus += ExpGainBonus * level;

        // 적 강화 적용
        // 경험치 증가 적용 
    }
}


// 40034 탈수
// 1초마다 적/플레이어 체력 감소 + 적 강화 + 경험치 증가
public class WashWaveDrainModifier : ActiveModifier<WashWaveSkill>
{
    [Header("감소 딜레이")]
    public float DrainDelay = 1f;
    [Header("체력 감소량")]
    public float DrainAmount = 0.01f;
    [Header("적 강화 수치")]
    public float EnemyStrengthBonus = 1f;
    [Header("경험치 증가량")]
    public float ExpGainBonus = 5f;

    public override void ApplyModifier(WashWaveSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.DrainHp = true;
        skill._modifierData.DrainDelay = DrainDelay;
        skill._modifierData.DrainAmount = DrainAmount;

        int level = skill.GetUpgradeLevel(upgradeData.Id);

        skill._modifierData.EnemyStrengthBonus += EnemyStrengthBonus * level;
        skill._modifierData.ExpGainBonus += ExpGainBonus * level;

        // 적 강화 적용
        // 경험치 증가 적용 
    }
}

// 40035 차오르는 체력
// 시전 시 최대 체력의 10% 만큼 회복 + 적 강화 + 경험치 증가
public class WashWaveHealModifier : ActiveModifier<WashWaveSkill>
{
    [Header("체력 회복 비율")]
    public float HealRatio = 0.1f;
    [Header("적 강화 수치")]
    public float EnemyStrengthBonus = 1f;
    [Header("경험치 증가량")]
    public float ExpGainBonus = 5f;

    public override void ApplyModifier(WashWaveSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.HealOnCast = true;
        skill._modifierData.HealRatio += HealRatio;

        int level = skill.GetUpgradeLevel(upgradeData.Id);

        skill._modifierData.EnemyStrengthBonus += EnemyStrengthBonus * level;
        skill._modifierData.ExpGainBonus += ExpGainBonus * level;

        // 적 강화 적용
        // 경험치 증가 적용
    }
}
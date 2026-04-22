using UnityEngine;

// 비누 던지기 모디파이어
public class SoapThrowModifierData
{
    public bool Retracking = false;     // 재추적
    public bool PierceDamage = false;   // 관통 추가 피해
    public bool PierceSpeed = false;    // 관통 추가 속도
    public bool RemovePierce = false;   // 관통 제거

    // Apply하면서 적용
    public float DamagePerPierce = 0f;
    public float SpeedPerPierce = 0f;
    public float DamagePerRemovalPierce = 0f;
}


// 40011 감나빗!    /      재추적
public class SoapRetargetModifier : ActiveModifier<SoapThrowSkill>
{
    public override void ApplyModifier(SoapThrowSkill skill, ActiveUpgradeData upgradeData)
    {
        // 재추적 설정
        skill._modifierData.Retracking = true;
    }
}

// 40012 거품내기    /      관통 시 추가 피해
public class SoapPierceDamageModifier : ActiveModifier<SoapThrowSkill>
{
    [Header("관통당 추가 피해")]
    public float DamagePerPierce = 0.5f;

    public override void ApplyModifier(SoapThrowSkill skill, ActiveUpgradeData upgradeData)
    {
        // 관통 추가 피해 설정
        skill._modifierData.PierceDamage = true;
        skill._modifierData.DamagePerPierce += DamagePerPierce;
    }
}

// 40014 거품 가속   /      관통 시 속도 증가
public class SoapPierceSpeedModifier : ActiveModifier<SoapThrowSkill>
{
    [Header("관통당 추가 속도")]
    public float SpeedPerPierce = 1f;

    public override void ApplyModifier(SoapThrowSkill skill, ActiveUpgradeData upgradeData)
    {
        // 관통 추가 속도 설정
        skill._modifierData.PierceSpeed = true;
        skill._modifierData.SpeedPerPierce += SpeedPerPierce;
    }
}

// 40015 비누 덩어리  /     관통 제거
public class SoapRemovalPierceModifier : ActiveModifier<SoapThrowSkill>
{
    [Header("감소 관통당 추가 피해")]
    public float DamagePerRemovalPierce = 2f;

    public override void ApplyModifier(SoapThrowSkill skill, ActiveUpgradeData upgradeData)
    {
        // 관통 제거 설정
        skill._modifierData.RemovePierce = true;
        skill._modifierData.DamagePerRemovalPierce += DamagePerRemovalPierce;
    }
}
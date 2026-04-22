using UnityEngine;

public class DetergentCapsuleModifierData
{
    // 1만 시간의 법칙
    public bool RapidFire = false;
    public float CooldownPerProjectile = 0f;
}


// 40040 1만 시간의 법칙
// 투사체 수를 업그레이드 스탯에서 제거하고 그만큼 쿨타임 감소
public class DetergentRapidFireModifier : ActiveModifier<DetergentCapsuleSkill>
{
    [Header("투사체 1개당 쿨타임 감소량")]
    public float CooldownPerProjectile = 0.5f;

    public override void ApplyModifier(DetergentCapsuleSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.RapidFire = true;
        skill._modifierData.CooldownPerProjectile = CooldownPerProjectile;
    }
}
using UnityEngine;

public class BluetoothShowerModifierData
{
    // 수압 최대로!
    public bool RapidFire = false;
    public float RapidFireDuration = 0f;

    // 온수샤워
    public bool HealAfterDelay = false;
    public float HealDelay = 0f;
    public float HealInterval = 0f;
    public float HealRatio = 0f;

    // 냉수마찰
    public bool SpeedBoostOnStart = false;
    public float SpeedBoostDuration = 0f;
    public float SpeedBoostAmount = 0f;

    // 예열완료
    public bool KnockbackAfterDelay = false;
    public float KnockbackBonus = 0f;
    public float KnockbackDelay = 0f;

    // 방수코팅
    public bool DefenseOnActive = false;
    public float DefenseBonus = 0f;

    // 키친건!
    public bool AlwaysFire = false;
}



// 40057 수압 최대로!
// 시전 시작 2초간 투사체 +1
public class BluetoothRapidFireModifier : ActiveModifier<BluetoothShowerSkill>
{
    [Header("유지 시간")]
    public float RapidFireDuration = 2f;

    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.RapidFire = true;
        skill._modifierData.RapidFireDuration = RapidFireDuration;
    }
}

// 40058 온수샤워
// 시전 2초 이후 체력 회복
public class BluetoothHealModifier : ActiveModifier<BluetoothShowerSkill>
{
    [Header("회복 발동 대기 시간")]
    public float HealDelay = 2f;
    [Header("회복 간격")]
    public float HealInterval = 1f;
    [Header("체력 회복 비율")]
    public float HealRatio = 0.01f;

    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.HealAfterDelay = true;
        skill._modifierData.HealDelay = HealDelay;
        skill._modifierData.HealInterval = HealInterval;
        skill._modifierData.HealRatio = HealRatio;
    }
}

// 40059 냉수마찰
// 시전 시작 2초간 이동속도 증가
public class BluetoothSpeedBoostModifier : ActiveModifier<BluetoothShowerSkill>
{
    [Header("유지 시간")]
    public float SpeedBoostDuration = 2f;
    [Header("이동속도 증가율")]
    public float SpeedBoostAmount = 1f;

    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.SpeedBoostOnStart = true;
        skill._modifierData.SpeedBoostDuration = SpeedBoostDuration;
        skill._modifierData.SpeedBoostAmount = SpeedBoostAmount;
    }
}

// 40060 예열완료
// 시전 2초 이후 넉백 활성화
public class BluetoothKnockbackModifier : ActiveModifier<BluetoothShowerSkill>
{
    [Header("추가 넉백")]
    public float KnockbackBonus = 5f;
    [Header("넉백 발동 대기 시간")]
    public float KnockbackDelay = 2f;

    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.KnockbackAfterDelay = true;
        skill._modifierData.KnockbackBonus = KnockbackBonus;
        skill._modifierData.KnockbackDelay = KnockbackDelay;
    }
}

// 40061 방수코팅
// 시전 중 방어력 증가
public class BluetoothDefenseModifier : ActiveModifier<BluetoothShowerSkill>
{
    [Header("방어력 증가량")]
    public float DefenseBonus = 1f;

    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.DefenseOnActive = true;
        skill._modifierData.DefenseBonus = DefenseBonus;
    }
}

// 40062 키친건!
// 정지 시에도 마지막 이동 방향으로 발사
// 모든 조건부 효과 상시 적용
public class BluetoothAlwaysFireModifier : ActiveModifier<BluetoothShowerSkill>
{
    public override void ApplyModifier(BluetoothShowerSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.AlwaysFire = true;
    }
}
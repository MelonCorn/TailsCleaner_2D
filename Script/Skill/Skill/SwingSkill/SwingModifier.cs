using System;
using UnityEngine;

// Towel / Mop 공통 모디파이어 데이터
public class SwingModifierData
{
    // 리사이클 (타올 / 걸레)
    // 상대 스킬 보유 시 반대 방향에 초승달 장판 추가 생성
    public bool HasOwnRecycle = false;
    public bool HasOtherRecycle = false;

    // 패링 (휘두르며로 걸레에 복사)
    // 장판 범위 내 적 탄환 제거
    public bool BulletClear = false;

    // 젖은 걸레 (휘두르며로 타올에 복사)
    public const string DEBUFF_KEY_SLOW = "SwingSlow";
    public bool SlowOnHit = false;
    public float SlowAmount = 0f;
    public float SlowDuration = 0f;

    // 어질어질 (휘두르며로 타올에 복사)
    public bool StunOnHit = false;
    public float StunDuration = 0f;

    // 휘두르며 (타올 / 걸레)
    public bool HasSyncUpgrade = false;

    // 상대 효과 복사 (휘두르며 전용)
    public void SyncEffect(SwingModifierData other)
    {
        // 리사이클
        if (other.HasOwnRecycle) HasOtherRecycle = true;
        // 패링
        if (other.BulletClear) BulletClear = true;

        // 젖은 걸레
        if (other.SlowOnHit)
        {
            SlowOnHit = true;
            SlowAmount = other.SlowAmount;
            SlowDuration = other.SlowDuration;
        }

        // 어질어질
        if (other.StunOnHit)
        {
            StunOnHit = true;
            StunDuration = other.StunDuration;
        }
    }
}


public class TowelSwingModifierData : SwingModifierData { }
public class MopSwingModifierData : SwingModifierData { }


// 타올 휘두르기 전용 모디파이어

// 40022 타올 리사이클
// 공격방향의 반대 방향에 초승달 장판 생성
[Serializable]
public class TowelRecycleModifier : ActiveModifier<TowelSwingSkill>
{
    public override void ApplyModifier(TowelSwingSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.HasOwnRecycle = true;
}

// 40024 패링
// 타올과 초승달 장판에 탄환 제거 기능 추가
[Serializable]
public class TowelParryModifier : ActiveModifier<TowelSwingSkill>
{
    public override void ApplyModifier(TowelSwingSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.BulletClear = true;
}

// 40025 타올 휘두르며
// 타올에 걸레 업그레이드 적용 (슬로우, 기절)
[Serializable]
public class TowelSyncModifier : ActiveModifier<TowelSwingSkill>
{
    public override void ApplyModifier(TowelSwingSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.HasSyncUpgrade = true;
}



// 걸레 휘두르기 전용 모디파이어

// 40027 걸레 리사이클
// 공격 방향에 초승달 장판 생성
[Serializable]
public class MopRecycleModifier : ActiveModifier<MopSwingSkill>
{
    public override void ApplyModifier(MopSwingSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.HasOwnRecycle = true;
}

// 40028 젖은 걸레
// 걸레와 초승달에 피격된 적 일정 시간동안 슬로우
[Serializable]
public class MopSlowModifier : ActiveModifier<MopSwingSkill>
{
    [Header("이동속도 감소율")] public float SlowAmount = 0.2f;
    [Header("슬로우 지속시간")] public float SlowDuration = 1.5f;

    public override void ApplyModifier(MopSwingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.SlowOnHit = true;
        skill._modifierData.SlowAmount = SlowAmount;
        skill._modifierData.SlowDuration = SlowDuration;
    }
}

// 40029 어질어질
// 걸레와 초승달에 피격된 적 일정 시간동안 이동속도, 피해량 100% 감소
[Serializable]
public class MopStunModifier : ActiveModifier<MopSwingSkill>
{
    [Header("이동속도 감소율")] public float SpeedAmount = 1f;
    [Header("공격력 감소율")] public float DamageAmount = 1f;
    [Header("지속시간")] public float StunDuration = 0.5f;

    public override void ApplyModifier(MopSwingSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.StunOnHit = true;
        skill._modifierData.StunDuration = StunDuration;
    }
}

// 40030 걸레 휘두르며
// 걸레에 타올 업그레이드 적용 (초승달, 탄환 제거)
[Serializable]
public class MopSyncModifier : ActiveModifier<MopSwingSkill>
{
    public override void ApplyModifier(MopSwingSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.HasSyncUpgrade = true;
}
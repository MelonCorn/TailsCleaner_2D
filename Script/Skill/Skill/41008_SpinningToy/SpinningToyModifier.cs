using System.Collections.Generic;
using UnityEngine;
using static SpinningToySkill;

public class SpinningToyModifierData
{
    public float SizeMultiplier = 0f;

    public bool BurstOnExpire = false;
    public WaitForSeconds BurstDelay;

    // 팽이 넉백
    public float TopKnockback = 0f;

    // 타입별 추가 스탯 수치
    public float TrainSpeed = 0f;      // 기차   이동속도
    public float MoonMaxHp = 0f;       // 달     최대 체력
    public float SharkDamage = 0f;     // 상어   공격력
    public float PirateExpGain = 0f;       // 해적선 경험치 획득량
    public float PirateMonsterStr = 0f;  // 해적선 적 강화 수치

    // 업그레이드로 추가된 타입별 투사체 목록
    public List<(TOY_TYPE type, int count)> AddedToys = new();
}


// 40042 기차 장난감 - 플레이어 속도 증가
public class SpinningToyTrainModifier : ActiveModifier<SpinningToySkill>
{
    [Header("증가 플레이어 이동속도")]
    [SerializeField] private float _playerSpeed = 1f;

    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.AddedToys.Add((TOY_TYPE.Train, upgradeData.ProjectileCount));
        skill._modifierData.TrainSpeed = _playerSpeed;
    }
}

// 40043 팽이 장난감 - 넉백 추가
public class SpinningToyTopModifier : ActiveModifier<SpinningToySkill>
{
    [Header("증가 넉백")]
    [SerializeField] private float _knockBack = 1f;

    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    { 
        skill._modifierData.AddedToys.Add((TOY_TYPE.Top, upgradeData.ProjectileCount));
        skill._modifierData.TopKnockback = _knockBack;
    }
}
// 40044 달 장난감 - 플레이어 체력 증가
public class SpinningToyMoonModifier : ActiveModifier<SpinningToySkill>
{
    [Header("증가 플레이어 최대 체력")]
    [SerializeField] private float _playerMaxHp = 1f;

    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.AddedToys.Add((TOY_TYPE.Moon, upgradeData.ProjectileCount));
        skill._modifierData.MoonMaxHp = _playerMaxHp;
    }
}

// 40045 오리 장난감
public class SpinningToyDuckModifier : ActiveModifier<SpinningToySkill>
{
    [Header("큰 오리 수")]
    [SerializeField] private int _duck_B_Count = 1;
    [Header("작은 오리 수")]
    [SerializeField] private int _duck_S_Count = 2;
    [Header("작은 오리 크기 배율")]
    [SerializeField] private float _sizeMultiplier = 0.5f;

    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.SizeMultiplier = _sizeMultiplier;
        skill._modifierData.AddedToys.Add((TOY_TYPE.Duck_B, _duck_B_Count));    // 큰오리
        skill._modifierData.AddedToys.Add((TOY_TYPE.Duck_S, _duck_S_Count));    // 작은오리
    }
}

// 40046 해적선 장난감 - 적이 강해짐, 경험치 추가
public class SpinningToyPirateModifier : ActiveModifier<SpinningToySkill>
{
    [Header("적 강화 수치")]
    [SerializeField] private float _monsterStr = 1f;
    [Header("증가 플레이어 획득 경험치")]
    [SerializeField] private float _playerExpGain = 1f;
    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.AddedToys.Add((TOY_TYPE.Pirate, upgradeData.ProjectileCount));
        skill._modifierData.PirateMonsterStr = _monsterStr;
        skill._modifierData.PirateExpGain = _playerExpGain;
    }
}

// 40047 상어 장난감 - 플레이어 공격력 증가
public class SpinningToySharkModifier : ActiveModifier<SpinningToySkill>
{
    [Header("증가 플레이어 공격력")]
    [SerializeField] private float _playerDamage = 1f;
    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.AddedToys.Add((TOY_TYPE.Shark, upgradeData.ProjectileCount));
        skill._modifierData.SharkDamage = _playerDamage;
    }
}

// 40048 물놀이 끝! - 지속시간 종료 시 퍼짐
public class SpinningToyBurstModifier : ActiveModifier<SpinningToySkill>
{
    [Header("버스트 사이 딜레이")]
    [SerializeField] private float _burstDelay = 0.4f;

    public override void ApplyModifier(SpinningToySkill skill, ActiveUpgradeData upgradeData)
    {
        if(skill._modifierData.BurstDelay == null)
            skill._modifierData.BurstDelay = new WaitForSeconds(_burstDelay);
        skill._modifierData.BurstOnExpire = true;
    }
}
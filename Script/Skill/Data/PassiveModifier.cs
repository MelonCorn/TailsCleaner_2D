using System;
using UnityEngine;

[Serializable]
public abstract class PassiveModifier
{
    // 기본 스탯에 추가
    // ex) 목표를 중앙에 두고 스위치
    public virtual void ModifyBaseAdd(SkillStat baseStat) { }

    // 패시브 스탯 배율 합
    // 패시브 배율 합을 업그레이드 스탯 계산 후 곱함
    // ex) 더 크게, 스노우 볼링, 임플란트
    public virtual void ModifyMul(SkillStat mulSumStat) { }

    // 최종 곱산용
    // 최종 스탯에 최종 곱산
    // ex) 양손잡이, 냥빨래, 황금왕관
    public virtual void ModifyFinalMultiply(SkillStat finalStat) { }


    // 최종 스탯에서 스탯 당 추가 스탯 적용
    public virtual void ModifyPostFinal(SkillStat finalStat) { }




    public virtual bool OnProjectileInit(SkillStat runtimeBaseStat, SkillStat runtimeFinalStat) { return false; }   // 투사체 생성 시, bool은 재계산 확인용
    public virtual void OnDamage(MonsterBase monster) { }                             // 적 피해 시
    public virtual bool OnPierce(SkillStat runTimePassiveMulStat) { return false; }   // 관통 시
    public virtual void OnDurationTick(SkillStat runTimePassiveMulStat) { }           // 지속시간마다


    // 영구 보너스 계산
    public virtual void ModifyPlayerPermanent(PlayerStatFlat flat, PlayerStatMul multi, SkillManager skillManager, int subTag) { }


    // 특정 서브태그를 가진 스킬 수
    protected static int CountSkillsWithSubTag(SkillManager skillManager, int subTag)
    {
        // 서브태그 플래그 변환
        int flag = SubTagRegistry.GetFlag(subTag);

        if (flag == 0) return 0;

        int count = 0;

        // 카운팅
        foreach (var skill in skillManager.MyActiveSkills)
            if ((skill.CurrentSubTag & flag) != 0) count++;

        return count;
    }
}


// ID 42001 / SubTag 40101
// 매이크 라쿤 크레이트 어겐! (보유 업그레이드 40101 태그 수만큼 방어력, 회피율, 치명타율, 치명타 피해 증가)
public class RaccoonCrateModifier : PassiveModifier
{
    [Header("방어력 증가량")]
    [SerializeField] float _defencePerTag = 1f;
    [Header("회피율 증가량")]
    [SerializeField] float _evasionChancePerTag = 1f;
    [Header("치명타확률 증가량")]
    [SerializeField] float _criticalChancePerTag = 1f;
    [Header("치명타피해 증가량")]
    [SerializeField] float _criticalDamageMultiPerTag = 1f;

    public override void ModifyPlayerPermanent(PlayerStatFlat flat, PlayerStatMul multi, SkillManager skillManager, int subTag)
    {
        int count = CountSkillsWithSubTag(skillManager, subTag);

        // 고정 수치
        flat.DefensePower             += _defencePerTag             * count;
        flat.EvasionChance            += _evasionChancePerTag       * count;
        flat.CriticalChance           += _criticalChancePerTag      * count;
        flat.CriticalDamageMultiplier += _criticalDamageMultiPerTag * count;
    }
}


// ID 42002 / SubTag 40102
// 목표를 중앙에 두고 스위치 (투사체 속도 증가, 넉백 강화)
public class CenterSwitchModifier : PassiveModifier
{
    [Header("추가 속도")]
    [SerializeField] float _projectileSpeedBonus = 1f;
    [Header("필요 넉백")]
    [SerializeField] float _requireKnockback = 1f;
    [Header("추가 넉백")]
    [SerializeField] float _knockbackBonus = 2f;

    public override void ModifyBaseAdd(SkillStat baseStat)
    {
        baseStat.ProjectileSpeed += _projectileSpeedBonus;

        // 넉백 조건부 추가는 투사체 Init 에서 초기 finalStat 보고 처리
    }

    public override bool OnProjectileInit(SkillStat runtimeBaseStat, SkillStat runtimeFinalStat)
    {
        // 투사체 생성 시 넉백이 조건 이상이면
        if (runtimeFinalStat.Knockback >= _requireKnockback)
        {
            // 추가 넉백을 기본 스탯에 추가
            runtimeBaseStat.Knockback += _knockbackBonus;
            return true;
        }
        return false;
    }
}

// ID 42003 / SubTag 40103
// 집중공략 (약화(슬로우 등)된 적은 최대 체력 5% 감소)
public class FocusAttackModifier : PassiveModifier
{
    [Header("최대 체력 감소율")]
    [SerializeField] float _maxHpReduceRate = 0.05f;
    
    public override void OnDamage(MonsterBase monster)
    {
        if (monster is IMonsterStatus status)
        {
            // 약화 상태 체크
            if (status.IsWeakened)
            {
                // 약화 상태면 최대 체력 감소 시도
                status.TryReduceMaxHp(_maxHpReduceRate);
            }
        }
    }
}

// ID 42004 / SubTag 40104
// 추가 추가 피해 (추가 피해 * 2)
public class DoubleExtraDamageModifier : PassiveModifier
{
    [Header("추가 횟수")]
    [SerializeField] int _extraMultiplier = 1;
    public override void ModifyBaseAdd(SkillStat baseStat)
    {
        // 1회 추가
        baseStat.ExtraMultiplier += _extraMultiplier;
    }
}

// ID 42005 / SubTag 40105
// SuperClean (군중제어 스킬이 적 속도를 5초간 추가로 느려지게 함)
public class SuperCleanModifier : PassiveModifier
{
    // 디버프 키
    public const string DEBUFF_KEY = "SuperClean";
    [Header("이동속도 감소율")]
    [SerializeField] float _slowAmount = 0.2f;
    [Header("슬로우 지속시간")]
    [SerializeField] float _slowDuration = 5f;

    public float SlowAmont => _slowAmount;
    public float SlowDuration => _slowDuration;
}

// ID 42006 / SubTag 40106
// 객기 (저주 수만큼 플레이어 방어력 증가)

public class BravadoModifier : PassiveModifier
{
    [Header("저주당 추가 방어력")]
    [SerializeField] float _defencePerTag = 1f;

    public override void ModifyPlayerPermanent(PlayerStatFlat flat, PlayerStatMul multi, SkillManager skillManager, int subTag)
    {
        int count = CountSkillsWithSubTag(skillManager, subTag);

        // 고정 수치
        flat.DefensePower += _defencePerTag * count;
    }

}
// ID 42007 / SubTag 40107
// 청소용 비닐옷 (체력 초과 회복 시 방어막 1회 생성)
public class CleanerVinylSuitModifier : PassiveModifier
{

}


// ID 42008 / SubTag 40108
// 고전비급  (타올 <-> 걸레 기본 스탯 공유)
public class ClassicSecretModifier : PassiveModifier
{

}


// ID 42009 / SubTag 40109
// 더 크게! 더! 더더! 크고 아름답게! (투사체 크기에 비례해 넉백과 데미지 계수가 증가)
public class BiggerBetterModifier : PassiveModifier
{
    [Header("크기 1당 데미지 증가 배율")]
    [SerializeField] float _damagePerSize = 0.2f;
    [Header("크기 1당 넉백 증가 배율")]
    [SerializeField] float _knockbackPerSize = 0.2f;

    public override void ModifyPostFinal(SkillStat finalStat)
    {
        // 사이즈 체크
        float excess = Mathf.Floor(finalStat.Size - 1f);

        if (excess <= 0f) return;

        finalStat.Damage    *= 1f + _damagePerSize    * excess;
        finalStat.Knockback *= 1f + _knockbackPerSize * excess;
    }
}


// ID 42010 / SubTag 40110
// 크고 아름다운 황금 왕관!(물에 뜹니다.)  (데미지 계수가 3배, 플레이어의 이동 속도 절반)
public class GoldCrownModifier : PassiveModifier
{
    [Header("투사체 데미지 계수")]
    [SerializeField] float _damageMultiplier = 3f;
    [Header("플레이어 이동속도 계수")]
    [SerializeField] float _speedMultiplier = 0.5f;

    public override void ModifyFinalMultiply(SkillStat finalStat)
    {
        finalStat.Damage *= _damageMultiplier;
    }

    public override void ModifyPlayerPermanent( PlayerStatFlat flat, PlayerStatMul multi, SkillManager skillManager, int subTag)
    {
        multi.MoveSpeed += _speedMultiplier;
    }
}

// ID 42011 / SubTag 40111
// 원딜의 정석 (공격 시전 속도에 비례해 치명타 확률 증가)
public class ADCarryModifier : PassiveModifier
{
    [Header("공격속도 1당 증가 치명타확률")]
    [SerializeField] float _criticalPerCooldown = 1f;

    public override void ModifyPlayerPermanent(PlayerStatFlat flat, PlayerStatMul multi, SkillManager skillManager, int subTag)
    {
        // 100에서 얼마나 줄었는지
        float reduction = 100f - skillManager.Player.AttackSpeed;

        // 감소량 없으면 패스
        if (reduction <= 0f) return;

        // 감소 1당 치명타 확률 증가
        flat.CriticalChance += reduction * _criticalPerCooldown;
    }
}


// ID 42012 / SubTag 40112
// 스노우볼링 (스킬 지속 시간 동안 일정 시간마다 스탯 증가)
public class SnowballingModifier : PassiveModifier
{
    [Header("배율 증가 간격")]
    [SerializeField] float _tickInterval = 0.5f;
    [Header("틱당 배율 증가량")]
    [SerializeField] float _multiplierPerTick = 0.2f;

    public override void ModifyBaseAdd(SkillStat baseStat)
    {
        baseStat.DurationTickInterval = _tickInterval;
    }

    public override void OnDurationTick(SkillStat runTimePassiveMulStat)
    {
        runTimePassiveMulStat.Damage            += _multiplierPerTick;
        runTimePassiveMulStat.Size              += _multiplierPerTick;
        runTimePassiveMulStat.ProjectileSpeed   += _multiplierPerTick;
    }
}


// ID 42013 / SubTag 40113
// 양손잡이 (데미지 계수 절반. 투사체가 두배)
public class AmbiModifier : PassiveModifier
{
    [Header("데미지 계수")]
    [SerializeField] float _damageMultiplier = 0.5f;
    [Header("투사체 계수")]
    [SerializeField] int _projectileCountMultiplier = 2;

    public override void ModifyFinalMultiply(SkillStat finalStat)
    {
        finalStat.Damage *= _damageMultiplier;
        finalStat.ProjectileCount *= _projectileCountMultiplier;
    }
}



// ID 42014 / SubTag 40114
// 기초적인 임플란트입니다 (투사체 관통 시 추가 피해 부여)
public class ImplantModifier : PassiveModifier
{
    [Header("관통 추가 피해 계수")]
    [SerializeField] float _damagePerPierce = 0.2f;

    public override bool OnPierce(SkillStat runTimePassiveMulSum)
    {
        runTimePassiveMulSum.Damage += _damagePerPierce; // 1.0 -> 1.2 -> 1.4
        return true;
    }
}

// ID 42015 / SubTag 40115
// 탄산수 (데미지 틱이 피해를 입힐 때 마다 적의 최대 체력비례 피해)
public class SodaWaterModifier : PassiveModifier
{
    [Header("최대 체력 비례 피해율")]
    [SerializeField] float _maxHpDamageRate = 0.01f;

    public override void OnDamage(MonsterBase monster)
    {
        //monster.TakeDamage(monster.MaxHp * _maxHpDamageRate);
    }
}



// ID 42016 / SubTag 40116
// 냥빨래 (넉백 강화, 밀친 적 화면 밖으로 나갈 시 체력 비례 고정 피해)

public class CatLaundryModifier : PassiveModifier
{
    [Header("추가 넉백 배율")]
    [SerializeField] float _knockbackBonus = 2f;
    [Header("체력 비례 피해")]
    [SerializeField] float _offScreenDamageRatio = 0.1f;
    public float OffScreenDamageRatio => _offScreenDamageRatio;

    public override void ModifyFinalMultiply(SkillStat finalStat)
    {
        finalStat.Knockback *= _knockbackBonus;
    }
}

// ID 42017 / SubTag 40117
// 하지만 이렇게 간단하게 피했습니다. (방어막 최대 3중첩 + 탄환 제거 시 방어막 충전)
public class NimbleBlockModifier : PassiveModifier
{
    [Header("최대 방어막 중첩 수")]
    [SerializeField] int _maxShieldStack = 3;

    public int MaxShieldStack => _maxShieldStack;

    // SkillPlayerStatHandler에 전달
}


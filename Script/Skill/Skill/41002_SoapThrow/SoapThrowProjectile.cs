using UnityEngine;

public class SoapThrowProjectile : SkillProjectile<SoapThrowModifierData>
{
    SoapThrowSkill _soapThrowSkill;

    public override void Init(ActiveSkill owner, SoapThrowModifierData modifierData, Vector2 dir)
    {
        _soapThrowSkill = owner as SoapThrowSkill;

        base.Init(owner, modifierData, dir);
    }

    protected override bool OnCustomInit()
    {
        // 비누 덩어리 관통 제거
        if (_modifierData.RemovePierce)
        {
            // 관통 스택만큼 피해 계수 추가
            _runtimeUpgradeStat.Damage += _runtimeFinalStat.PierceCount * _modifierData.DamagePerRemovalPierce;

            // 관통 제거
            _runtimeBaseStat.PierceCount = 0;
            _runtimeUpgradeStat.PierceCount = 0;

            // 스탯 재계산
            return true;
        }

        // 관통 제거 안했으면 재계산 필요 X
        return false;
    }

    // 연출 시작 직전 설정
    protected override void OnBeforeStartSequence()
    {
        if (_modifierData.RemovePierce == true && _animator != null)
        {
            // 강철 비누 스프라이트로 변경
            //_animator.OverrideMainSprite(_soapThrowSkill.MetalSprite);
        }
    }


    // 관통 시
    protected override bool OnPierce(MonsterBase hitMonster)
    {
        // 비누 덩어리
        // 관통없이 즉시 파괴
        if (_modifierData.RemovePierce == true)
        {
            ExpireObject();
            return true;
        }

        // 거품내기
        // 관통 시 추가 피해
        PierceDamage();

        // 거품 가속
        // 관통 시 속도 증가
        PierceSpeed();

        // 감나빗!
        // 관통 후 재추적
        if (_modifierData.Retracking == true)
            RetargetEnemy(hitMonster);

        return false;
    }


    // 관통 추가 피해
    private void PierceDamage()
    {
        // 패시브 없거나 혹시나 관통당 추가 피해 없으면 무시
        if (_modifierData.PierceDamage == false || _modifierData.DamagePerPierce <= 0f) return;

        // 전용 모디파이어 관통 추가 피해
        float bonus = _modifierData.DamagePerPierce;

        // 추가 추가 피해 있을 때 (0.5 * 2)
        if (_runtimeFinalStat.ExtraMultiplier > 1)
            bonus *= _runtimeFinalStat.ExtraMultiplier;

        // 업그레이드 피해에 추가
        _runtimeUpgradeStat.Damage += bonus;
    }

    // 관통 추가 속도
    private void PierceSpeed()
    {
        // 패시브 없거나 혹시나 관통당 추가 속도 없으면 무시
        if (_modifierData.PierceSpeed == false || _modifierData.SpeedPerPierce <= 0f) return;

        // 업그레이드 스탯에 추가
        _runtimeUpgradeStat.ProjectileSpeed += _modifierData.SpeedPerPierce;
    }

    // 관통 후 가장 가까운 적 재추적
    private void RetargetEnemy(MonsterBase hitMonster)
    {
        // 관통한 적 제외 가장 가까운 타겟
        MonsterBase target = SkillManager.Instance.FindClosestMonster(_rigidbody.position, SkillManager.DEFAULT_SEARCH_RADIUS, hitMonster);

        // 타겟이 없거나 죽었다면 그냥 직진
        if (target == null) return;

        // 타겟 있다면
        // 관통한 현재 위치에서 타겟 방향으로 꺾기
        Vector2 newDir = (target.Position - _rigidbody.position).normalized;
        SetDirection(newDir);

        // 회전도 방향에 맞게 갱신
        float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}

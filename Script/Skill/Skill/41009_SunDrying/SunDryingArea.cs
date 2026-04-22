using UnityEngine;

public class SunDryingArea : SkillArea<SunDryingModifierData>
{
    private SunDryingSkill _sunDryingSkill;


    // 넉백용 overlap 버퍼
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[64];

    public override void Init(ActiveSkill owner, SunDryingModifierData modifierData, Vector2 dir = default)
    {
        // 형변환 후 캐싱
        _sunDryingSkill = owner as SunDryingSkill;

        base.Init(owner, modifierData, dir);

        // 이불 털기 - 시전 순간 넉백
        if (_modifierData.KnockbackOnActivate)
            KnockbackOnActivate();
    }

    protected void LateUpdate()
    {
        // 플레이어 위치 따라다님
        transform.position = GetPlayerPos();
    }

    // 틱마다
    protected override void OnTick()
    {
        base.OnTick();

        // 따스한 태양
        // 틱마다 업그레이드 스탯 증가
        if (_modifierData.DamagePerTick)
            ApplyDamagePerTick();
    }

    // 따스한 태양
    // 틱마다 업그레이드 스탯 누적
    // 추가추가피해 패시브 적용
    private void ApplyDamagePerTick()
    {
        // 따스한 태양 추가 업그레이드 스탯
        float bonus = _modifierData.DamagePerTickAmount;

        // 추가추가피해 패시브
        if (_runtimeFinalStat.ExtraMultiplier > 1)
            bonus *= _runtimeFinalStat.ExtraMultiplier;

        // 업그레이드 스탯에 더함
        _runtimeUpgradeStat.Damage += bonus;

        // 재계산
        SetDirty();
        CalculateStat();
    }

    // 소멸 시
    protected override void OnExpire()
    {
        base.OnExpire();

        //Skill에 꺼짐 알림
       if(_sunDryingSkill != null) _sunDryingSkill.OnAreaExpired();
    }
    
    
    // 시전 순간 콜라이더 범위 내 몬스터 즉시 넉백
    private void KnockbackOnActivate()
    {
        if (_collider == null) return;

        var filter = new ContactFilter2D();
        filter.useTriggers = true;

        int count = Physics2D.OverlapCollider(_collider, filter, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_overlapBuffer[i].CompareTag("Monster") &&
                _overlapBuffer[i].TryGetComponent(out MonsterBase monster))
            {
                TryActiveKnockback(monster);
            }
        }
    }
    
    
    // 넉백 적용
    protected void TryActiveKnockback(MonsterBase monster)
    {
        // 넉백 없으면 스킵
        if (_modifierData.KnockbackForce <= 0f) return;

        // 넉백 방향 투사체에서 몬스터 방향
        Vector2 dir = (monster.Position - _rigidbody.position).normalized;

        // 혹시 겹쳐서 방향 없으면 아래로
        if (dir == Vector2.zero) dir = Vector2.down;

        // 넉백 적용
        monster.Knockback(dir, _modifierData.KnockbackForce);
    }
}

using UnityEngine;

public class SkillProjectile<TModifierData> : SkillObjectBase
    where TModifierData : class, new()
{
    protected TModifierData _modifierData;

    private int _currentPierceCount = 0;        // 현재 관통 횟수

    public virtual void Init(ActiveSkill owner, TModifierData modifierData, Vector2 dir)
    {
        // 관통 초기화
        _currentPierceCount = 0;

        // 전용 모디파이어 데이터 설정
        _modifierData = modifierData;

        // 스킬 스탯 스냅샷, 패시브 모디파이어 캐싱, OnInit 등
        base.Init(owner, dir);
    }

    // 방향 설정
    protected void SetDirection(Vector2 newDir)
    {
        _dir = newDir;
    }


    // 트리거 충돌 체크
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (_expired == true) return;

        // 몬스터 태그 아니면 무시
        if (col.CompareTag("Monster") == false) return;

        // 몬스터 컴포넌트 참조 시도 후 피해
        if (col.TryGetComponent(out MonsterBase monster))
        {
            if (monster.hp <= 0) return;
            // 데미지 계산 후 피해
            monster.TakeDamage(GetFinalDamage());
            // 넉백 시도
            TryKnockback(monster);
        }

        // 현재 관통 추가
        _currentPierceCount++;

        // 자식 전용 관통 로직
        bool destroy = OnPierce(monster);

        // 내부에서 파괴되면 뒤는 무시
        if (destroy) return;

        bool mulChanged = false;

        // 패시브 관통 효과 (임플란트 등)
        foreach (var passive in _passiveModifiers)
        {
            // 관통으로 true 반환할 때
            if (passive.OnPierce(_runtimePassiveMulStat))
            {
                mulChanged = true;
            }
        }

        if (mulChanged)
        {
            SetDirty();
            CalculateStat();
        }

        // 최대 관통 초과 시 파괴
        if (_currentPierceCount > _runtimeFinalStat.PierceCount)
            ExpireObject();
    }

    // 관통 시
    // 내부에서 직접 파괴 시 ExpireObject() 호출 후 true 반환
    // ex) 비누 덩어리 - 관통 삭제 시 충돌 즉시 파괴
    protected virtual bool OnPierce(MonsterBase hitMonster)
    {
        return false;
    }
}

using UnityEngine;

//  TowelSwingArea / MopSwingArea / CrescentSwingArea 베이스
public abstract class SwingArea<TModifierData> : SkillArea<TModifierData> where TModifierData : SwingModifierData, new()
{
    [Header("플레이어와 거리")]
    [SerializeField] private float _swingDistance = 1.5f;

    private Vector2 _dirOffset;

    // 초기화
    public override void Init(ActiveSkill owner, TModifierData modifierData, Vector2 dir)
    {
        // 방향의 옵셋
        _dirOffset = dir.normalized * _swingDistance;

        // 각도
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 위치 - 플레이어 위치에 방향 옵셋만큼
        _rigidbody.position = GetPlayerPos() + _dirOffset;

        base.Init(owner, modifierData, dir);
    }

    //protected override void FixedUpdate()
    //{
    //    // 플레이어 위치 따라가기
    //    _rigidbody.MovePosition(GetPlayerPos() + _dirOffset);
    //}
    protected void LateUpdate()
    {
        // 플레이어 위치 따라가기
        transform.position = GetPlayerPos() + _dirOffset;
    }

    // 장판에 적 투사체 들어올시
    protected override void OnBulletEnter(PoolObject projectile)
    {
        if (_modifierData.BulletClear == false) return;

        // 풀 반환
        if(_poolObject != null) projectile.ReturnToPoolAfter(0);

        // 하지만 이렇게 간단하게 피했습니다.
        // 탄환 제거 시 방어막 충전 (패시브 있으면)
        SkillManager.Instance.SkillStatHandler.OnBulletCleared();
    }


    // 틱 피해
    // 젖은 걸레 (슬로우)
    // 어질어질 (기절)
    protected override void OnTick(MonsterBase monster)
    {
        base.OnTick(monster);

        // 젖은 걸레
        if (_modifierData.SlowOnHit)
        {
            monster.ApplySlow(SwingModifierData.DEBUFF_KEY_SLOW, _modifierData.SlowAmount, _modifierData.SlowDuration);

            // 집중공략 (약화 적 최대체력 감소)
            foreach (var passive in _passiveModifiers)
                passive.OnDamage(monster);
        }

        // 어질어질
        if (_modifierData.StunOnHit)
        {
            monster.ApplyStun(_modifierData.StunDuration);
        }
    }
}

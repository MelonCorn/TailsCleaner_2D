using UnityEngine;



public class DetergentCapsuleSkill : ActiveSkill<DetergentCapsuleProjectile, DetergentCapsuleModifierData>
{
    public override void RecheckModifiers()
    { 
        // FinalStat 완전 계산 완료 후
        base.RecheckModifiers();

        // 1만시간의 법칙 처리
        if (_modifierData.RapidFire)
        {
            // 최종 투사체 수만큼 쿨타임 줄이기
            _finalStat.Cooldown -= _finalStat.ProjectileCount * _modifierData.CooldownPerProjectile;
        }
    }


    protected override void OnActive(int index, int totalCount)
    {
        // 방지
        if (AttackDir == Vector2.zero) return;

        // 세제 캡슐 생성
        SpawnCapsule(AttackDir);
    }


    // 세제 캡슐 생성
    private void SpawnCapsule(Vector2 dir)
    {
        // 각도 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 투사체 생성
        //DetergentCapsuleProjectile capsule = Instantiate(_skillObjectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
        DetergentCapsuleProjectile capsule = SpawnFromPool<DetergentCapsuleProjectile>(_skillObjectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

        // 초기화
        if(capsule != null) capsule.Init(this, _modifierData, dir);
    }
}

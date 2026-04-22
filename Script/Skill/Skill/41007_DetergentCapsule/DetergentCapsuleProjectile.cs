using UnityEngine;

public class DetergentCapsuleProjectile : SkillProjectile<DetergentCapsuleModifierData>
{
    [Header("회전 객체")]
    [SerializeField] Transform _rotObject;

    [Header("속도 대비 회전 배율")]
    [SerializeField] private float _spinMultiplier = 30f;
    
    
    public override void Init(ActiveSkill owner, DetergentCapsuleModifierData modifierData, Vector2 dir)
    {
        base.Init(owner, modifierData, dir);

        // 재사용 시 회전 객체의 각도 초기화
        if (_rotObject != null)
        {
            _rotObject.localRotation = Quaternion.identity;
        }
    }

    protected override void Update()
    {
        // 부모 Update (수명 체크 및 이동 처리 등)
        base.Update();

        // 캡슐 회전 (Z축)
        float spinAmount = _runtimeFinalStat.ProjectileSpeed * _spinMultiplier * Time.deltaTime;

        // 시계 방향 회전 (마이너스)
        _rotObject.Rotate(0f, 0f, -spinAmount);
    }
}

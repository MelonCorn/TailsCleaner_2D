using UnityEngine;

public class WashWaveProjectile : SkillProjectile<WashWaveModifierData>
{
    private WashWaveSkill _washWaveSkill;

    public override void Init(ActiveSkill owner, WashWaveModifierData modifierData, Vector2 dir)
    {
        // 형변환 후 저장
        _washWaveSkill = owner as WashWaveSkill;

        base.Init(owner, modifierData, dir);
    }


    protected override void OnBeforeStartSequence()
    {
        if (_animator != null && _washWaveSkill != null)
        {
            // 랜덤 스프라이트 적용
            _animator.OverrideMainSprite(_washWaveSkill.GetRandomSprite());
        }
    }
}

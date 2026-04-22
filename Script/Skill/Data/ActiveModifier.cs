using System;

[Serializable]
public abstract class ActiveModifier
{
    // 특수 로직 적용
    public abstract void Apply(ActiveSkill skill, ActiveUpgradeData upgradeData);
}




[Serializable]
public abstract class ActiveModifier<T> : ActiveModifier where T : ActiveSkill
{
    // T 형변환
    public override void Apply(ActiveSkill skill, ActiveUpgradeData upgradeData)
    {
        if (skill is T specificSkill)
        {
            ApplyModifier(specificSkill, upgradeData);
        }
    }

    // 실제 특수 로직 적용
    public abstract void ApplyModifier(T skill, ActiveUpgradeData upgradeData);
}
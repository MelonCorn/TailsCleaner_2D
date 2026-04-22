
// 물바다 8방향 투사체
public class WaterBombSplashProjectile : SkillProjectile<WaterBombModifierData>
{
    protected override bool OnCustomInit()
    {
        if (_modifierData != null && _modifierData.SplashSize > 0f)
        {
            _runtimeBaseStat.Size *= _modifierData.SplashSize;

            return true;
        }

        return false;
    }
}

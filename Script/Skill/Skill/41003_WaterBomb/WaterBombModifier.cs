
using UnityEngine;

public class WaterBombModifierData
{
    // 물바다
    // 착탄 시 8방향 추가 투사체 발사
    public bool Splash = false;
    public int SplashCount = 0;     // 투사체 수
    public float SplashSize = 0f;   // 투사체 크기 배율
    public float SplashDelay = 0f;  // 추가추가피해 텀

    // 소용돌이
    // 착탄 지점에 끌어당기는 소용돌이 장판 생성
    public bool Vortex = false;
    public int VortexPullCount = 0;         // 끌어당기기 횟수 (레벨마다 +1 누적)
    public float VortexSize = 0f;           // 크기 배율
    public float VortexPullDelay = 0f;      // 끌어당기기 텀
    public float VortexMoveTime = 0f;       // 이동 시간

    // 폭발은 예술이다
    // 착탄 장판 범위 내 적 투사체 삭제
    public bool BulletClear = false;
}



// 40018 물바다
// 착탄 시 8방향으로 추가 투사체 발사
public class WaterBombSplashModifier : ActiveModifier<WaterBombSkill>
{
    [Header("투사체 수")] 
    [SerializeField] int _splashCount = 8;
    [Header("투사체 크기 배율")]
    [SerializeField] float _splashSize = 0.5f;
    [Header("추가추가피해 딜레이")]
    [SerializeField] float _splashDelay = 0.2f;

    public override void ApplyModifier(WaterBombSkill skill, ActiveUpgradeData upgradeData)
    {
        skill._modifierData.Splash = true;
        skill._modifierData.SplashCount = _splashCount;
        skill._modifierData.SplashSize = _splashSize;
        skill._modifierData.SplashDelay = _splashDelay;
    }
}


// 40019 소용돌이
// 착탄 지점에 finalStat.Size * 1.5 크기의 소용돌이 장판 생성
// 0.5초마다 적과 적 투사체를 끌어당김
// 레벨마다 끌어당기기 횟수 +1
public class WaterBombVortexModifier : ActiveModifier<WaterBombSkill>
{
    [Header("장판 크기 배율")]
    [SerializeField] float _vortexSize = 1.5f;
    [Header("끌어당기기 딜레이")]
    [SerializeField] float _vortexDelay = 0.5f;
    [Header("끌어당기기 이동시간")]
    [SerializeField] float _vortexMoveTime = 0.1f;

    public override void ApplyModifier(WaterBombSkill skill, ActiveUpgradeData upgradeData)
    {
        int level = skill.GetUpgradeLevel(upgradeData.Id);
        skill._modifierData.Vortex = true;
        skill._modifierData.VortexPullCount += level;
        skill._modifierData.VortexSize = _vortexSize;
        skill._modifierData.VortexPullDelay = _vortexDelay;
        skill._modifierData.VortexMoveTime = _vortexMoveTime;
    }
}


// 40020 폭발은 예술이다
// 장판 적 투사체 진입 시 삭제
public class WaterBombBulletClearModifier : ActiveModifier<WaterBombSkill>
{
    public override void ApplyModifier(WaterBombSkill skill, ActiveUpgradeData upgradeData)
        => skill._modifierData.BulletClear = true;
}
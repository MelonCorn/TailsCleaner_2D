using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WaterBombSkill : ActiveSkill<WaterBombArea, WaterBombModifierData>
{
    [Header("낙하 설정")]
    [SerializeField] private float _fallHeight = 8f;        // 타겟 위 생성 높이 오프셋
    [SerializeField] private float _fallDuration = 1f;      // 낙하 시간

    [Header("물바다 투사체 프리팹")]
    [SerializeField] private WaterBombSplashProjectile _splashProjectilePrefab;

    [Header("소용돌이 장판 프리팹")]
    [SerializeField] private WaterBombVortexArea _vortexAreaPrefab;

    public float FallHeight => _fallHeight;
    public float FallDuration => _fallDuration;

    private WaitForSeconds _splashDelay;


    // 착지 사운드
    public void PlayLandSFX() => PlaySpecialSFX();

    protected override void OnActive(int index, int totalCount)
    {
        // 혹시나 타겟 없으면 스킵
        if (_currentTarget == null) return;

        // 타겟 조준
        Vector2 targetPos = CurrentTarget.Position;

        // 물폭탄 생성 (낙하)
        WaterBombArea bomb = SpawnFromPool<WaterBombArea>(_skillObjectPrefab, transform.position, Quaternion.identity);
        bomb.Init(this, _modifierData, targetPos);
    }



    // 물바다 8방향 투사체 발사
    // WaterBombArea.OnLand에서 호출
    public void SpawnSplash(Vector2 spawnPos, SkillStat stat)
    {
        if (_splashProjectilePrefab == null)
        {
            Debug.LogWarning("[WaterBombSkill] SplashProjectilePrefab 미설정");
            return;
        }

        // 착지 시 물바다 투사체 생성
        StartCoroutine(SplashCoroutine(spawnPos, stat));
    }

    // 8방향 발사 코루틴
    private IEnumerator SplashCoroutine(Vector2 spawnPos, SkillStat stat)
    {
        // 추가추가피해
        // 0.5초 텀 두고 발사
        int burstCount = Mathf.Max(1, stat.ExtraMultiplier);

        for (int i = 0; i < burstCount; i++)
        {
            // 투사체 발사
            FireSplash(spawnPos);

            yield return _splashDelay;
        }
    }

    // 8방향 투사체 동시 발사 (45도 간격)
    private void FireSplash(Vector2 spawnPos)
    {
        // 투사체 수
        int spashCount = _modifierData.SplashCount;

        for (int i = 0; i < spashCount; i++)
        {
            // 각도
            float angleDeg = i * (360f / spashCount);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 물바다 투사체 생성
            //WaterBombSplashProjectile projectile = Instantiate(_splashProjectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angleDeg));
            WaterBombSplashProjectile projectile = SpawnFromPool(_splashProjectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angleDeg));

            // 물폭탄 스탯 사용
            if(projectile != null) projectile.Init(this, _modifierData, dir);
        }
    }
    
    
    
    // 소용돌이
    // WaterBombArea.OnLand에서 호출
    public void SpawnVortexArea(Vector2 spawnPos, SkillStat stat, WaterBombModifierData modifierData, List<PassiveModifier> passives)
    {
        if (_vortexAreaPrefab == null)
        {
            Debug.LogWarning("[WaterBombSkill] VortexAreaPrefab 미설정");
            return;
        }

        // 소용돌이 생성
        //WaterBombVortexArea vortex = Instantiate(_vortexAreaPrefab, spawnPos, Quaternion.identity);
        WaterBombVortexArea vortex = SpawnFromPool(_vortexAreaPrefab, spawnPos, Quaternion.identity);

        Debug.Log($"소용돌이 틱 수 : {modifierData.VortexPullCount}");

        // 물폭탄 finalStat.Size의 1.5배
        if(vortex != null)
            vortex.Init(
                modifierData.VortexPullDelay,
                modifierData.VortexPullCount,
                stat.Size * _modifierData.VortexSize,
                passives,
                modifierData.BulletClear );
    }

    // 재적용
    public override void RecheckModifiers()
    {
        base.RecheckModifiers();

        // 물바다 딜레이
        if (_modifierData.Splash == true)
            _splashDelay = new WaitForSeconds(_modifierData.SplashDelay);
    }
}

using System.Collections;
using UnityEngine;

public class WashWaveSkill : ActiveSkill<WashWaveProjectile, WashWaveModifierData>, IMonsterStrengthBonus
{
    [Header("스폰 설정")]
    [SerializeField] private float _spawnOffsetDistance = 15f;  // 플레이어에서 스폰 중심까지 거리
    [SerializeField] private float _spawnRadius = 5f;           // 스폰 랜덤 반경

    [Header("파도 스프라이트")]
    [SerializeField] Sprite[] _sprites;

    private Coroutine _drainCoroutine = null;        // 탈수 코루틴
    private WaitForSeconds _drainDelay;              // 탈수딜레이
    private WaitForSeconds _returnDelay;             // 밀물 썰물 딜레이

    // 적 강화 수치
    public float MonsterStrengthBonus => _modifierData.EnemyStrengthBonus;

    protected override void OnActive(int index, int totalCount) { } // 안씀

    protected override IEnumerator ActiveCoroutine()
    {
        // 공격 방향
        Vector2 attackDir = AttackDir;
        if (attackDir == Vector2.zero) yield break;

        // 차오르는 체력
        // 시전 시 최대 체력 회복
        if (_modifierData.HealOnCast)
            OnHealOnCast();

        // 메인 시전
        // 공격 방향 반대편에서 생성 -> 공격 방향으로
        StartCoroutine(FireWave(attackDir));

        // 밀물 썰물 없으면 종료
        if (_modifierData.HasReturn == false) yield break;

        // 밀물 썰물 시전
        // ExtraDamageMultiplier만큼 반복
        // 공격 방향 -> 공격 방향 반대편으로
        int returnCount = Mathf.Max(1, _finalStat.ExtraMultiplier);

        for (int i = 0; i < returnCount; i++)
        {
            yield return _returnDelay;

            // 공격 방향에서 생성 -> 공격 반대 방향으로
            StartCoroutine(FireWave(-attackDir));
        }
    }


    // 파도 발사
    // 발사 방향의 반대쪽 오프셋 중심에서 랜덤 위치로 생성 후 발사
    private IEnumerator FireWave(Vector2 fireDir)
    {
        // 플레이어 위치
        Vector2 playerPos = SkillManager.Instance.CurrentPlayerPos;

        // 스폰 중심 - 플레이어 기준 발사 반대 방향으로 오프셋
        Vector2 spawnCenter = playerPos + (-fireDir.normalized * _spawnOffsetDistance);

        // 투사체 수만큼 생성
        int count = Mathf.Max(1, _finalStat.ProjectileCount);
        for (int i = 0; i < count; i++)
        {
            // 파도 생성 시 사운드
            PlaySpecialSFX();

            // 스폰 중심 기준 원 안 랜덤 위치
            Vector2 randomOffset = Random.insideUnitCircle * _spawnRadius;
            Vector2 spawnPos = spawnCenter + randomOffset;

            // 파도 생성
            SpawnWave(spawnPos, fireDir);

            // 스킬 스폰 간격
            yield return _fireDelay;
        }
    }

    // 파도 생성
    private void SpawnWave(Vector2 spawnPos, Vector2 dir)
    {
        // 회전 각도
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 투사체 생성
        WashWaveProjectile washWave = SpawnFromPool<WashWaveProjectile>(_skillObjectPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        // 투사체 초기화
        if(washWave != null) washWave.Init(this, _modifierData, dir);
    }

    // 차오르는 체력 처리
    private void OnHealOnCast()
    {
        // 플레이어 최대 체력의 HealRatio만큼 회복
        // 청소용 비닐옷도 내부에서 처리 (초과 회복 시 방어막)
        SkillStatHandler.HealByRatio(_modifierData.HealRatio);

        Debug.Log($"[WashWave] 차오르는 체력 - 체력 {_modifierData.HealRatio * 100f}% 회복");
    }


    // 모디파이어 갱신 시
    // 밀물 썰물 딜레이
    // 탈수 딜레이, 코루틴 시작
    public override void RecheckModifiers()
    {
        base.RecheckModifiers();

        // 밀물 썰물
        // 딜레이 설정
        if (_modifierData.HasReturn)
        {
            _returnDelay = new WaitForSeconds(_modifierData.ReturnDelay);
        }

        // 탈수
        // 딜레이 설정
        // 코루틴 시작
        if (_modifierData.DrainHp)
        {
            _drainDelay = new WaitForSeconds(_modifierData.DrainDelay);

            // 코루틴 돌고있으면 무시
            if (_drainCoroutine != null) return;

            _drainCoroutine = StartCoroutine(DrainCoroutine());
        }
    }

    // 탈수 코루틴
    // 1초마다 적,플레이어 체력 감소
    private IEnumerator DrainCoroutine()
    {
        while (true)
        {
            // 플레이어 체력 감소
            SkillStatHandler.TakeDamageByRatio(_modifierData.DrainAmount);

            // 모든 적 체력 감소
            // foreach (var monster in MonsterManager.Instance.ActiveMonsters)
            //     monster.TakeDamage(_modifierData.DrainAmount);

            Debug.Log($"[WashWave] 탈수 - 플레이어/모든 적 체력 {_modifierData.DrainAmount * 100f}% 감소");

            yield return _drainDelay;
        }
    }

    // 랜덤 스프라이트 반환
    public Sprite GetRandomSprite()
    {
        if (_sprites == null || _sprites.Length == 0) return null;
        return _sprites[Random.Range(0, _sprites.Length)];
    }
    
    
    // 세탁파도 업그레이드에 누적된 경험치 증가량 전달
    public override void ModifyPlayerBonus(PlayerStatFlat flat, PlayerStatMul mul)
        => flat.ExpGainRate += _modifierData.ExpGainBonus;
}

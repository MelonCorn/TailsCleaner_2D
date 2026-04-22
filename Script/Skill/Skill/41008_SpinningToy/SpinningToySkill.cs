using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpinningToySkill : ActiveSkill<SpinningToyProjectile, SpinningToyModifierData>, IMonsterStrengthBonus
{
    public enum TOY_TYPE
    {
        Default = 0,    // 기본 회전 장난감
        Train,          // 기차
        Top,            // 팽이
        Moon,           // 달
        Duck_B,         // 큰 오리 
        Duck_S,         // 작은 오리 
        Pirate,         // 해적선
        Shark,          // 상어
    }

    [Header("공전 설정")]
    [SerializeField] float _orbitRadius = 2.5f;         // 공전 반지름
    [SerializeField] float _startAngle = 90f;           // 시작 각도 (12시)

    [Header("스프라이트")]
    [SerializeField] Sprite[] _sprites; // TOY_TYPE 순서대로

    // 해적선 적 강화 수치
    public float MonsterStrengthBonus => _modifierData.PirateMonsterStr;

    private int _currentWaveId = 0;




    protected override void OnActive(int index, int totalCount) { }


    protected override IEnumerator ActiveCoroutine()
    {
        // 스폰할 투사체 리스트
        List<TOY_TYPE> spawnList = BuildSpawnList();

        // 투사체 수
        int count = spawnList.Count;

        // 시전 ID 증가
        _currentWaveId++;

        // 이번 시전 ID
        int waveId = _currentWaveId;

        // 이번 발사 때 생성될 장난감
        List<SpinningToyProjectile> currentWaveToys = new List<SpinningToyProjectile>();

        // 속도 절대값 처리, 0.1 보장
        float safeSpeed = Mathf.Max(0.1f, Mathf.Abs(_finalStat.ProjectileSpeed));

        // 투사체의 공전 각속도 (초당 몇 도)
        float angularSpeedDeg = (safeSpeed / Mathf.Max(0.1f, _orbitRadius)) * Mathf.Rad2Deg;

        // 투사체 간 벌어져야 하는 간격
        float anglePerItem = count > 0 ? 360f / count : 0f;

        // 투사체가 anglePerItem 만큼 이동하는 데 걸리는 시간 = 스폰 텀
        float spawnInterval = anglePerItem / angularSpeedDeg;

        // 생성 시작 시간
        float startTime = Time.time;

        // 최종 스폰 딜레이
        WaitForSeconds spawnDelay = new WaitForSeconds(spawnInterval);

        for (int i = 0; i < count; i++)
        {
            // 코루틴 시작 후 흐른 시간
            float elapsed = Time.time - startTime;

            // 그 시간 동안 회전한 총 각도
            float currentTotalRot = elapsed * angularSpeedDeg;

            // 현재 번호에 맞는 정확한 현재 각도
            // 시작 12시 + 전체 회전량 - (번호 * 간격)
            float angle = _startAngle + currentTotalRot - (i * anglePerItem);

            // 보정된 목표 각도로 위치 설정
            Vector2 spawnPos = (Vector2)transform.position + GetOrbitOffset(angle);

            // 투사체 생성
            SpinningToyProjectile toy = SpawnFromPool(_skillObjectPrefab, spawnPos, Quaternion.identity);

            if (toy != null)
            {
                //Debug.Log($"[회전장난감] {waveId}번 시전의 {i + 1}/{count} 번째 투사체 생성 성공! (현재속도: {_finalStat.ProjectileSpeed})");

                // 공전체 하나당 사운드
                PlaySpecialSFX();

                // 공전상태로 초기화
                toy.InitOrbit(this, _modifierData, spawnList[i], angle, _orbitRadius, waveId);

                // 생성된 장난감에 추가
                currentWaveToys.Add(toy);
            }

            // 다음 투사체 생성 시간
            float nextSpawnTargetTime = startTime + (spawnInterval * (i + 1));

            // 대기 시간 (프레임 밀린 시간만큼 덜 대기)
            while (Time.time < nextSpawnTargetTime)
            {
                yield return null;
            }
        }

        // 전체 스폰에 걸린 시간
        float totalSpawnTime = Time.time - startTime;

        // 생성 시간 제외 남은 지속 시간
        float remainDuration = Mathf.Max(0f, _finalStat.Duration - totalSpawnTime);

        yield return new WaitForSeconds(remainDuration);

        // 대기 시간이 끝나면
        // 리스트에 있는 모든 투사체 버스트
        foreach (var toy in currentWaveToys)
        {
            // 투사체 살아있으면
            if (toy != null)
            {
                // 버스트
                toy.TriggerBurst(waveId);
            }
        }
    }

    // 스폰할 투사체 리스트 빌드
    // 양손잡이로 투사체 두배되었을 때 비율 알아내서 나누는 과정
    private List<TOY_TYPE> BuildSpawnList()
    {
        // 스폰할 투사체 리스트
        List<TOY_TYPE> spawnList = new();

        // 기본 투사체 수
        int basePart = _baseStat.ProjectileCount * _commonStat.ProjectileCount;

        // 업그레이드 투사체 수
        int upgradePart = 0;
        foreach (var (type, count) in _modifierData.AddedToys)
            upgradePart += count;

        // 총 투사체 수
        int totalPart = basePart + upgradePart;

        // 뻥튀기 된 비율
        // 기본 + 업그레이드 투사체 합과 최종 스탯 개수 차이로 배율 알아냄
        // 최종이 10개인데 총 투사체가 5개면 2배된 것
        float finalProjectileMul = totalPart > 0
            ? (float)_finalStat.ProjectileCount / totalPart
            : 1f;

        // 기본 장난감을 뻥튀기 배율만큼 넣기
        int finalBaseCount = Mathf.RoundToInt(basePart * finalProjectileMul);
        for (int i = 0; i < finalBaseCount; i++)
            spawnList.Add(TOY_TYPE.Default);

        // 특수 장난감들도 각각 배율만큼 넣기
        foreach (var (type, count) in _modifierData.AddedToys)
        {
            int typeCount = Mathf.RoundToInt(count * finalProjectileMul);
            for (int i = 0; i < typeCount; i++)
                spawnList.Add(type);
        }

        // 혹시나 0개 들어간 특수 장난감은 디폴트로 넣음
        if (spawnList.Count == 0)
            spawnList.Add(TOY_TYPE.Default);

        return spawnList;
    }


    // 복사본 버스트 (물놀이 끝 + 추가추가피해)
    public void SpawnBurstCopy(Vector2 spawnPos, Vector2 dir, TOY_TYPE type)
    {
        // 복사본 생성
        SpinningToyProjectile copy = SpawnFromPool(_skillObjectPrefab, spawnPos, Quaternion.identity);

        // 초기화
        if(copy != null) copy.InitBurst(this, _modifierData, dir, type);

        // 비주얼 별도로 적용 추가
    }

    // 투사체 수에 따라 각 벌리기
    private Vector2 GetOrbitOffset(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _orbitRadius;
    }

    public Sprite GetTypeSprite(TOY_TYPE type)
    {
        // 형변환
        int index = (int)type;

        // 유효성 체크 다 넘기면
        // 스프라이트 반환
        if (_sprites != null && index < _sprites.Length && _sprites[index] != null)
            return _sprites[index];

        return null;
    }


    // 플레이어 스탯 적용
    public override void ModifyPlayerBonus(PlayerStatFlat flat, PlayerStatMul multi)
    {
        foreach (var (type, count) in _modifierData.AddedToys)
        {
            switch (type)
            {
                case TOY_TYPE.Train:  flat.MoveSpeed   += _modifierData.TrainSpeed;     break;
                case TOY_TYPE.Moon:   flat.MaxHp       += _modifierData.MoonMaxHp;      break;
                case TOY_TYPE.Shark:  flat.AttackPower += _modifierData.SharkDamage;    break;
                case TOY_TYPE.Pirate: flat.ExpGainRate += _modifierData.PirateExpGain;  break;
            }
        }
    }
}

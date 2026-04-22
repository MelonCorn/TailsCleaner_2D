using System.Collections;
using UnityEngine;

public class BluetoothShowerSkill : ActiveSkill<BluetoothShowerProjectile, BluetoothShowerModifierData>
{
    // 버프 키
    private const string BUFF_KEY_WATERPROOF = "WaterProof";    // 방수코팅
    private const string BUFF_KEY_COLDWATER = "ColdWater";      // 냉수마찰

    [Header("투사체 간격")]
    [SerializeField] private float _projectileSpacing = 0.5f;

    private float _castStartTime = -1f;             // 시전 시작 시간 (-1 비활성)
    private Vector2 _lastMoveDir = Vector2.right;   // 마지막 이동 방향 (키친건용)

    // 수압 최대로 데이터
    private bool _lastRapidFireActive = false;
    private int _rapidFireBonus = 0;


    // 온수샤워 코루틴
    private Coroutine _healCoroutine = null;
    private WaitForSeconds _healDelay;          // 시작 딜레이
    private WaitForSeconds _healInterval;       // 회복 텀

    // 예열 완료 데이터
    private bool _lastKnockbackActive = false;
    // 냉수마찰 데이터
    private bool _lastColdWaterActive = false;

    private readonly PlayerStatFlat _waterproofFlat = new();    // 방수코팅
    private readonly PlayerStatFlat _coldWaterFlat = new();       // 냉수마찰
    private float _lastDefenseBonus;
    private float _lastSpeedBoostAmount;


    protected override void OnActive(int index, int totalCount) { } // 안씀



    protected override void Update()
    {
        // 이동방향
        Vector2 moveDir = AttackDir;

        // 이동방향 갱신
        if (moveDir != Vector2.zero)
            _lastMoveDir = moveDir.normalized;

        // 시전 시작 / 종료 감지
        bool isActive = CanFire();
        bool wasActive = _castStartTime >= 0f;

        // 시전 시작
        if (isActive == true && wasActive == false)
        {
            // 시전 시작 시 castStartTime 갱신
            _castStartTime = Time.time;

            OnCastStart();
        }
        // 시전 종료
        else if (isActive == false && wasActive == true)
        {
            // 비활성화
            _castStartTime = -1f;

            OnCastEnd();
        }

        // 매 프레임 갱신
        if (_modifierData.RapidFire) UpdateRapidFire();             // 수압 최대로
        if (_modifierData.SpeedBoostOnStart) UpdateColdWater();     // 냉수마찰
        if (_modifierData.KnockbackAfterDelay) UpdateKnockback();   // 예열완료

        base.Update();
    }




    // 키친건
    // 정지 시에도 발사 체크
    protected override bool CanFire()
    {
        if (_modifierData.AlwaysFire == true) return true;
        return base.CanFire();
    }

    // 시전 시작 시
    private void OnCastStart()
    {
        // 루프 사운드 재생
        StartLoopSFX();

        // 방수코팅
        if (_modifierData.DefenseOnActive == true)
        {
            // 값 변했으면
            if (_modifierData.DefenseBonus != _lastDefenseBonus)
            {
                // 초기화 후 할당
                _waterproofFlat.Reset();
                _waterproofFlat.DefensePower = _modifierData.DefenseBonus;
                _lastDefenseBonus = _modifierData.DefenseBonus;
            }
            // 런타임 고정 스탯에 추가
            SkillStatHandler.AddRuntimeStat(BUFF_KEY_WATERPROOF, _waterproofFlat);
            Debug.Log($"[BluetoothShower] 방수코팅 ON - 방어력 + {_modifierData.DefenseBonus}");
        }

        // 온수샤워 코루틴 시작
        if (_modifierData.HealAfterDelay == true && _healCoroutine == null)
            _healCoroutine = StartCoroutine(HealCoroutine());
    }

    // 시전 종료 시
    private void OnCastEnd()
    {
        // 루프 사운드 중지
        StopLoopSFX();

        // 방수코팅 해제
        if (_modifierData.DefenseOnActive)
        {
            SkillStatHandler.RemoveRuntime(BUFF_KEY_WATERPROOF);
            Debug.Log($"[BluetoothShower] 방수코팅 OFF - 방어력 - {_modifierData.DefenseBonus}");
        }
        // 냉수마찰 해제
        if (_modifierData.SpeedBoostOnStart)
        {
            SkillStatHandler.RemoveRuntime(BUFF_KEY_COLDWATER);
            Debug.Log($"[BluetoothShower] 냉수마찰 OFF - 이동속도 - {_modifierData.SpeedBoostAmount}");
        }

        // 온수샤워 코루틴 종료
        StopHealCoroutine();
    }






    // 수압 최대로 체크
    private void UpdateRapidFire()
    {
        // 활성화 상태 (시전 시작 시간으로 부터 RapidFireDuration 초 안지났는지 체크)
        bool isActive = IsDuration(_modifierData.RapidFireDuration);

        // 활성화 상태
        if (isActive)
        {
            // 새 보너스 (추가추가피해 없으면 1, 있으면 2)
            int newBonus = _finalStat.ExtraMultiplier;

            // 활성화 상태고
            // 보너스 값도 같으면 스킵 (활성화 중 추가추가피해 습득 시 대비)
            if (_lastRapidFireActive && newBonus == _rapidFireBonus) return;

            // 비활성화 상태거나 보너스 값 달라졌으면 새로 적용

            // 이전 보너스 제거
            _upgradeStat.ProjectileCount -= _rapidFireBonus;

            // 활성화 상태로 갱신
            _lastRapidFireActive = true;
            // 새 보너스 적용
            _rapidFireBonus = newBonus;
            // 업그레이드 스탯 증가
            _upgradeStat.ProjectileCount += _rapidFireBonus;
        }
        // 비활성화 상태
        else
        {
            // 현재 상태도 비활성화라면 무시
            if (_lastRapidFireActive == false) return;

            // 비활성화 상태로 갱신
            _lastRapidFireActive = false;
            // 업그레이드 스탯 감소
            _upgradeStat.ProjectileCount -= _rapidFireBonus;
            // 보너스 스탯 초기화
            _rapidFireBonus = 0;
        }

        // 스탯 재계산
        CalculateStats();
    }


    // 온수샤워 코루틴
    // 2초 대기 후 HealInterval 마다 회복 (키친건이면 대기 스킵) 
    private IEnumerator HealCoroutine()
    {
        // 키친건 비활성화 상태면 대기
        if (_modifierData.AlwaysFire == false)
            yield return _healDelay;

        // 켜진동안 계속
        while (true)
        {
            // 최대 체력 비율 회복
            SkillStatHandler.HealByRatio(_modifierData.HealRatio);
            Debug.Log($"[BluetoothShower] 온수샤워 - 체력 {_modifierData.HealRatio * 100f}% 회복");

            // 회복 텀 대기
            yield return _healInterval;
        }
    }

    // 온수샤워 중지
    private void StopHealCoroutine()
    {
        if (_healCoroutine == null) return;
        StopCoroutine(_healCoroutine);
        _healCoroutine = null;
    }

    // 냉수마찰 체크
    private void UpdateColdWater()
    {
        // 활성화 상태 (시전 시작 시간으로 부터 SpeedBoostDuration 초 안지났는지 체크)
        bool isActive = IsDuration(_modifierData.SpeedBoostDuration);

        if (isActive == _lastColdWaterActive) return;

        // 상태 갱신
        _lastColdWaterActive = isActive;

        // 활성화
        if (isActive)
        {
            // 이동속도 갱신
            if (_modifierData.SpeedBoostAmount != _lastSpeedBoostAmount)
            {
                _coldWaterFlat.Reset();
                _coldWaterFlat.MoveSpeed = _modifierData.SpeedBoostAmount;
                _lastSpeedBoostAmount = _modifierData.SpeedBoostAmount;
            }
            // 런타임 고정 수치 추가
            SkillStatHandler.AddRuntimeStat(BUFF_KEY_COLDWATER, _coldWaterFlat);
        }
        // 비활성화
        else
        {
            // 런타임 고정 수치 제거
            SkillStatHandler.RemoveRuntime(BUFF_KEY_COLDWATER);
        }
    }


    // 예열완료 체크
    private void UpdateKnockback()
    {
        // 활성화 상태 (시전 시작 시간으로 부터 KnockbackDelay 초 지났는지 체크)
        bool isActive = IsAfterDelay(_modifierData.KnockbackDelay);

        // 활성화 상태 똑같으면 무시
        if (isActive == _lastKnockbackActive) return;

        // 활성화 상태 변경되면 갱신
        _lastKnockbackActive = isActive;

        // 활성화 상태
        if (_lastKnockbackActive)
        {
            // 업그레이드 스탯에 추가 넉백 추가
            _upgradeStat.Knockback += _modifierData.KnockbackBonus;
        }
        // 비활성화 상태
        else
        {
            // 업그레이드 스탯에서 추가 넉백 감소
            _upgradeStat.Knockback -= _modifierData.KnockbackBonus;
        }

        // 스탯 재계산
        CalculateStats();
    }



    // 조건 체크용 유틸

    // 수압 / 냉수마찰
    private bool IsDuration(float duration)
    {
        // 키친건 있으면 항상 참
        if (_modifierData.AlwaysFire) return true;

        // 시전 시작 시간부터 duration 초 안지났으면 참
        return _castStartTime >= 0f && Time.time <= _castStartTime + duration;
    }

    // 온수샤워 / 예열완료
    private bool IsAfterDelay(float delay)
    {
        // 키친건 있으면 항상 참
        if (_modifierData.AlwaysFire) return true;

        // 시전 시작 시간부터 delay 초 지났으면 참
        return _castStartTime >= 0f && Time.time >= _castStartTime + delay;
    }


    // 업그레이드 , 패시브 습득 시
    public override void RecheckModifiers()
    {
        base.RecheckModifiers();

        // 온수 샤워
        // 시작 딜레이, 회복 텀
        if (_modifierData.HealAfterDelay)
        {
            _healDelay = new WaitForSeconds(_modifierData.HealDelay);
            _healInterval = new WaitForSeconds(_modifierData.HealInterval);
        }

        // 키친 건 습득 후 비활성화 상태면
        if (_modifierData.AlwaysFire && _castStartTime < 0f)
        {
            // 시전 시작
            _castStartTime = Time.time;
            OnCastStart();
        }
    }


    // 발사 코루틴
    protected override IEnumerator ActiveCoroutine()
    {
        // 이동방향 입력 없으면 마지막 방향 사용
        Vector2 dir = SkillManager.Instance.Player.MoveDir != Vector2.zero
            ? SkillManager.Instance.Player.MoveDir.normalized
            : _lastMoveDir;

        // 발사 방향에 수직인 벡터
        Vector2 perp = new Vector2(-dir.y, dir.x);

        // 투사체 수
        int count = Mathf.Max(1, _finalStat.ProjectileCount);

        // 투사체 수만큼
        for (int i = 0; i < count; i++)
        {
            // 중앙 기준 오프셋
            float offset = (i - (count - 1) / 2f) * _projectileSpacing;
            Vector2 spawnPos = (Vector2)transform.position + perp * offset;

            // 투사체 생성
            SpawnProjectile(spawnPos, dir);
        }

        yield break;
    }


    // 투사체 생성
    private void SpawnProjectile(Vector2 spawnPos, Vector2 dir)
    {
        // 회전 각도
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 투사체 생성
        //BluetoothShowerProjectile projectile = Instantiate(_skillObjectPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));
        BluetoothShowerProjectile projectile = SpawnFromPool<BluetoothShowerProjectile>(_skillObjectPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        // 초기화
        if(projectile != null) projectile.Init(this, _modifierData, dir);
    }
}

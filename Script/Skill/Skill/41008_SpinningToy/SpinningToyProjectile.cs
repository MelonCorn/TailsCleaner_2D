using System.Collections;
using UnityEngine;

public class SpinningToyProjectile : SkillProjectile<SpinningToyModifierData>
{
    // 장난감 타입 (스프라이트 분기용)
    private SpinningToySkill.TOY_TYPE _toyType;

    SpinningToySkill toySkill;

    private bool _isOrbiting = false; // 공전 상태
    private float _orbitAngle;        // 현재 공전 각도
    private float _orbitRadius;       // 공전 반지름

    private int _waveId;            // 시전 ID


    // 공전모드로 초기화
    public void InitOrbit(ActiveSkill owner, SpinningToyModifierData modifierData,
        SpinningToySkill.TOY_TYPE toyType, float angleDeg, float radius, int waveId)
    {
        _toyType = toyType;
        _orbitAngle = angleDeg;
        _orbitRadius = radius;
        _isOrbiting = true;
        _waveId = waveId;

        toySkill = owner as SpinningToySkill;

        base.Init(owner, modifierData, Vector2.zero);
    }

    // 물놀이 끝 복사본 초기화
    public void InitBurst(ActiveSkill owner, SpinningToyModifierData modifierData, Vector2 dir, SpinningToySkill.TOY_TYPE toyType)
    {
        // 복사본도 타입 있어야 스프라이트 설정 가능
        _toyType = toyType;

        // 풀 사용 시
        // 공전상태 남아있는 상황일 수 있으니까
        _isOrbiting = false;

        toySkill = owner as SpinningToySkill;

        base.Init(owner, modifierData, dir);
    }

    protected override bool OnCustomInit()
    {
        // 재계산 여부
        bool recalcul = false;

        // 팽이 장난감이고 추가 넉백 있으면
        if (_toyType == SpinningToySkill.TOY_TYPE.Top && _modifierData.TopKnockback > 0f)
        {
            // 업그레이드 스탯에 넉백 추가
            _runtimeUpgradeStat.Knockback += _modifierData.TopKnockback;
            recalcul = true;
        }

        // 작은 오리일 경우
        if (_toyType == SpinningToySkill.TOY_TYPE.Duck_S)
        {
            // 기본 크기 줄임
            _runtimeBaseStat.Size *= _modifierData.SizeMultiplier;
            recalcul = true;
        }

        return recalcul;
    }

    protected override void OnBeforeStartSequence()
    {
        // 장난감 타입에 맞는 스프라이트 설정
        if (_animator != null && toySkill != null)
            _animator.OverrideMainSprite(toySkill.GetTypeSprite(_toyType));
    }

    protected override void Update()
    {
        // 공전모드일 때는 체크 따로 안함
        if (_isOrbiting)
            return;

        // 비행 모드 그냥 base에서 수명 체크 + 스노우볼링 틱 모두
        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (_expired) return;

        // 비행 모드
        if (_isOrbiting == false)
        {
            base.FixedUpdate();
        }
    }

    protected void LateUpdate()
    {
        if (_expired) return;

        if (_isOrbiting)
        {
            UpdateOrbit();
        }
    }



    // 공전 -> 비행 (물놀이 끝)
    public void TransitionToFly(Vector2 dir)
    {
        _isOrbiting = false;
        _lastDurationTickTime = Time.time; // 스노우볼링용 초기화
        _createTime = Time.time;           // 수명 초기화
        _expired = false;

        // 방향 설정
        SetDirection(dir);
    }

    
    // 강제 만료
    public void ForceExpire()
    {
        if (_expired == false)
            ExpireObject(); 
    }

    // 버스트 실행
    public void TriggerBurst(int requestWaveId)
    {
        // 강제 만료 상태가 아닐 때, 하이어라키에 있을 때, waveID가 같을 때 안전장치
        if (_expired == false && gameObject.activeInHierarchy && _waveId == requestWaveId)
        {
            _waveId = -1;   // 중복 실행 방지
            StartCoroutine(BurstCoroutine());
        }
    }

    private IEnumerator BurstCoroutine()
    {
        // 혹시 만료 상태면 바로 공전 끝
        if (_expired == true) yield break;

        // 물놀이 끝 미적용 시 소멸
        if (_modifierData.BurstOnExpire == false)
        {
            ExpireObject();
            yield break;
        }

        // 스킬 없을 시 소멸
        if (toySkill == null)
        {
            ExpireObject();
            yield break;
        }

        // 버스트 횟수 = ExtraDamageMultiplier (기본 1, 추가추가피해 패시브 2)
        int burstCount = Mathf.Max(1, _runtimeFinalStat.ExtraMultiplier);

        for (int burst = 0; burst < burstCount; burst++)
        {
            // 만료 시 그냥 깨버림
            if (_expired == true) yield break;

            // 마지막 버스트인지
            bool isLastBurst = (burst == burstCount - 1);

            // 방향 (바깥쪽)
            Vector2 dir = (transform.position - SkillManager.Instance.Player.transform.position).normalized;
            if (dir == Vector2.zero) dir = Vector2.right;   // 제로 방지

            // 마지막 버스트
            if (isLastBurst)
            {
                // 이 투사체 자신이 비행 전환
                TransitionToFly(dir);
            }
            // 중간 버스트
            else
            {
                // 복사본 발사
                // 자신은 공전 유지
                toySkill.SpawnBurstCopy(_rigidbody.position, dir, _toyType);

                // 버스트 대기 시간
                yield return _modifierData.BurstDelay;
            }
        }
    }

    // 공전 갱신
    private void UpdateOrbit()
    {
        // 각도 갱신
        // 선속도(ProjectileSpeed)를 반지름으로 나누면 라디안 각속도가 나옴
        // 그걸 일반 각도(Deg)로 변환
        _orbitAngle += (_runtimeFinalStat.ProjectileSpeed / _orbitRadius) * Mathf.Rad2Deg * Time.deltaTime;

        // 플레이어 기준 위치 계산
        Vector2 playerPos = GetPlayerPos();

        float rad = _orbitAngle * Mathf.Deg2Rad;

        // 다음 위치
        //transform.position = playerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _orbitRadius;
        Vector2 nextPos = playerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _orbitRadius;

        // 이동
        transform.position = nextPos;
    }
}
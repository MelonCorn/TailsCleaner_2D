using UnityEngine;

public class WaterBombArea : SkillArea<WaterBombModifierData>
{
    [Header("투사체 상태 오브젝트")]
    [SerializeField] GameObject _fallObject;
    [SerializeField] GameObject _landObject;
    
    // 상태별 애니메이터
    private SkillAnimator _fallAnimator;
    [SerializeField] SkillAnimator _landAnimator;

    private bool _isLanded = false;             // 착지 상태
    private Vector2 _startPos;                  // 낙하 시작 위치
    private Vector2 _targetPos;                 // 목표 지점 위치
    private float _fallStartTime;               // 낙하 시작 시간
    private WaterBombSkill _waterBombSkill;

    protected override void Awake()
    {
        base.Awake();
        _fallAnimator = _fallObject.GetComponent<SkillAnimator>();
    }


    public override void Init(ActiveSkill owner, WaterBombModifierData modifierData, Vector2 targetPos)
    {
        // 형변환
        _waterBombSkill = owner as WaterBombSkill;

        _isLanded = false;
        
        // 애니메이터 리셋
        if (_fallAnimator != null) _fallAnimator.ResetState();
        if (_landAnimator != null) _landAnimator.ResetState();

        // 투사체 상태 오브젝트 변경
        _fallObject.SetActive(true);
        _landObject.SetActive(false);
        
        // 낙하 연출 실행
        if (_fallAnimator != null) _fallAnimator.StartSequence(_waterBombSkill.FallDuration);

        // 목표 지점
        _targetPos = targetPos;
        // 시작 위치 (목표 지점에서 일정 거리 위)
        _startPos = targetPos + Vector2.up * _waterBombSkill.FallHeight;
        // 위치 갱신
        transform.position = _startPos;
        // 낙하 시작 시간
        _fallStartTime = Time.fixedTime;

        base.Init(owner, modifierData, Vector2.zero);
    }


    protected override void Update()
    {
        // 낙하 중
        // 체크 차단
        if (_isLanded == false) return;

        // 착지 후
        // 수명, 틱, 스노우볼링
        base.Update();
    }
    protected override void FixedUpdate()
    {
        if (_expired) return;

        if (_isLanded == false)
            Fall();

        else
            base.FixedUpdate();
    }

    // 수직 낙하
    private void Fall()
    {
        // 시간 기반 Lerp
        float t = (Time.fixedTime - _fallStartTime) / _waterBombSkill.FallDuration;

        // 위치 갱신 (최적화)
        //transform.position = Vector2.Lerp(_startPos, _targetPos, t);
        Vector2 nextPos = Vector2.Lerp(_startPos, _targetPos, t);
        _rigidbody.MovePosition(nextPos);

        // 낙하 완료
        if (t >= 1f)
        {
            _rigidbody.position = _targetPos;
            OnLand();
        }
    }
    
    // 착지 처리
    private void OnLand()
    {
        _isLanded = true;

        // 착지 시점부터
        // 수명, 틱 타이머 시작
        _createTime = Time.time;
        _lastTickTime = Time.time;

        // 착지 사운드 재생
        if (_waterBombSkill != null) _waterBombSkill.PlayLandSFX();

        // 낙하 종료 연출 후 오브젝트 끄기
        if (_fallAnimator != null) _fallAnimator.RequestExpire(() => _fallObject.SetActive(false));
        // 애니메이터 없으면 그냥 끄기
        else _fallObject.SetActive(false); 
       
        // 장판 켜기
        _landObject.SetActive(true);

        // 유지 시간동안 장판 연출 시작
        if (_landAnimator != null) _landAnimator.StartSequence(_runtimeFinalStat.Duration);

        // 물바다
        // 8방향 추가 투사체 발사
        if (_modifierData.Splash)
            if(_waterBombSkill != null) _waterBombSkill.SpawnSplash(_rigidbody.position, _runtimeFinalStat);

        // 소용돌이
        // WaterBombSkill에서 생성
        if (_modifierData.Vortex)
            if (_waterBombSkill != null) _waterBombSkill.SpawnVortexArea(_rigidbody.position, _runtimeFinalStat, _modifierData, _passiveModifiers);
    }


    // 장판에 적 투사체 진입 시
    protected override void OnBulletEnter(PoolObject projectile)
    {
        // 폭발은 예술이다
        if (_modifierData.BulletClear == false) return;

        // 풀 반환
        if (_poolObject != null) projectile.ReturnToPoolAfter(0);

        // 하지만 이렇게 간단하게 피했습니다.
        // 탄환 제거 시 방어막 충전 (패시브 있으면)
        SkillManager.Instance.SkillStatHandler.OnBulletCleared();
    }

    // 만료 오버라이드
    protected override void ExpireObject()
    {
        if (_expired == true) return;
        _expired = true;

        OnExpire();

        // 장판 애니메이터 종료 연출 콜백
        if (_landAnimator != null)
        {
            _landAnimator.RequestExpire(ReturnToPool);
        }
        else
            ReturnToPool();
    }
}

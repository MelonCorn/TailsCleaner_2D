using System;
using System.Collections.Generic;
using UnityEngine;
using static VisualAction;

// 실행 흐름
//   StartPhase 시작
//   _actionIndex = 0, 첫 번째 액션 StartAction()
//   Update()에서 매 프레임 Tick 함수 호출 (스프라이트 교체, 알파값 변경)
//   _curElapsed >= _curDuration 되면 AdvanceAction()
//   _actionIndex++ 해서 다음 액션 StartAction()
//   마지막 액션까지 완료되면 페이즈 종료
//   _isExpirePhase면 _onExpireDone() 콜백 호출 (풀 반환)
//
// 사용 순서
//   ResetState()          풀에서 꺼낼 때 (이전 상태 초기화)
//   SetVisualData()       SO 교체가 필요할 때 (회전 장난감 타입별)
//   OverrideMainSprite()  스프라이트 고정 필요할 때 (비누 덩어리, 세탁 파도 랜덤)
//   PlayActivate()        발동 연출 시작
//   PlayMaintain(float)   유지 연출 시작 (유지시간 전달)
//   RequestExpire(cb)     종료 연출 시작 완료되면 콜백 호출
//
//   적 명중 시: PlayHit(Vector2) 언제든 호출
//

public class SkillAnimator : MonoBehaviour
{
    [Header("기본 데이터")] // 런타임 중 변경 가능
    [SerializeField] private SkillVisualData _data;

    
    private enum PHASE_STATE { None, Activate, Duration, Expire }   // 상태 (발동,유지,종료)
    private PHASE_STATE _currentPhaseState = PHASE_STATE.None;      // 현재 상태
    private float _duration = 0f;                                   // 유지해야할 총 시간
    private float _startTime = 0f;                                  // 활성화 시작 시간

    // 스프라이트, 알파값 설정
    private SpriteRenderer _renderer;
    
    // 원본 로컬 스케일, 스프라이트, 알파값 저장
    private Vector3 _originalScale;
    private Sprite _originalSprite;     
    private float _originalAlpha = 1f;

    // 유지 연출 재생 속도 배율
    private float _durationSpeedMul = 1f;

    // 고정 스프라이트 교체용
    // null 아니면 TickSprites()에서 스프라이트 안바꿈
    // 비누 덩어리처럼 스프라이트 시퀀스 재생 하면서 고정
    private Sprite _overrideSprite = null;

    // 현재 실행 중인 페이즈의 VisualAction 리스트
    private List<VisualAction> _currentPhase = null;

    // 현재 실행 중인 액션의 인덱스
    // AdvanceAction마다 증가 최대 도달 시 페이즈 종료
    private int _actionIndex = 0;

    // false면 Update 안 함
    private bool _phaseRunning = false;

    // duration=-1인 액션에 채워줄 시간
    // PlayDuration 호출될 때 CalcFillDuration 으로 계산해서 저장
    private float _fillDuration = 0f;

    // 현재 액션 상태
    private VISUALACTION_TYPE _curType;      // 현재 액션 타입 (FadeIn, FadeOut)
    private Sprite[]          _curSprites;   // 재생용 스프라이트 배열
    private float             _curDuration;  // 액션 총 재생 시간
    private float             _curElapsed;   // 현재 재생 시간
    private bool              _curReverse;   // 역재생 여부
    private AnimationCurve    _curCurve;     // 커브 (없으면 Linear)
    private bool              _curLoop;      // 루프 여부
    private float             _curFps;       // 루프 속도


    // 종료 연출 관련
    // RequestExpire 호출 상태
    private bool _isExpiring = false;

    // 종료 페이즈 상태
    // AdvanceAction 에서 페이즈가 끝났을 때 콜백 호출용
    private bool _isExpirePhase = false;

    // 종료 연출 완료 콜백 (풀반환용)
    private Action _onExpireDone = null;

    // 이펙트 목록
    private List<PoolObject> _attachEffects = new();



    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        
        if (_renderer != null)
        {
            // 원본 스프라이트 렌더러 저장
            _originalSprite = _renderer.sprite;
            _originalAlpha = _renderer.color.a;
        }

        // 원본 크기 저장
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        // 페이즈 진행 중이 아니면 중단
        if (_phaseRunning == false) return;

        // 재생 시간 누적
        _curElapsed += Time.deltaTime * _durationSpeedMul;

        // 진행률 계산
        // t = 0 시작, t = 1 완료
        // _curDuration이 0 이하면 즉시 완료
        float t = _curDuration > 0f ? Mathf.Clamp01(_curElapsed / _curDuration) : 1f;

        // 액션 타입별 로직
        // 진행률에 따라 갱신
        UpdateVisuals(t);

        // 액션 완료 체크 후 다음 액션으로
        if (_curElapsed >= _curDuration)
            AdvanceAction();
    }

    // 상태별 비주얼 갱신
    private void UpdateVisuals(float t)
    {
        switch (_curType)
        {
            case VISUALACTION_TYPE.PlaySprites:             // 스프라이트 재생
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.FadeIn:                  // 페이드인
                TickFade(0f, _originalAlpha, t);
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.FadeOut:                 // 페이드아웃
                TickFade(_originalAlpha, 0f, t);
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.ScaleUp:                 // 크기 확대
                TickScale(Vector3.zero, _originalScale, t);
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.ScaleDown:               // 크기 축소
                TickScale(_originalScale, Vector3.zero, t);
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.FadeInAndScaleUp:        // 페이드인 + 확대
                TickFade(0f, _originalAlpha, t);
                TickScale(Vector3.zero, _originalScale, t);
                TickSprites(t);
                break;
            case VISUALACTION_TYPE.FadeOutAndScaleDown:     // 페이드아웃 + 축소
                TickFade(_originalAlpha, 0f, t);
                TickScale(_originalScale, Vector3.zero, t);
                TickSprites(t);
                break;
        }
    }

    // 풀에서 꺼낼 때 초기화
    public void ResetState()
    {
        _isExpiring      = false;       // 종료 상태
        _isExpirePhase   = false;       // 종료 페이즈 상태
        _phaseRunning    = false;       // 페이즈 구동 상태
        _durationSpeedMul = 1f;         // 유지 연출 재생 배율
        _overrideSprite  = null;        // 고정 스프라이트
        _onExpireDone    = null;        // 종료 연출 완료 상태

        // 풀 반환
        foreach (var effect in _attachEffects)
        {
            if (effect == null) continue;

            if (effect.TryGetComponent<PoolObject>(out var poolObject))
                poolObject.ReturnToPoolAfter(0);
            else
                Destroy(effect);
        }

        // 청소
        _attachEffects.Clear();

        if (_renderer != null)
        {
            // 스프라이트 복원
            var c = _renderer.color;
            _renderer.color = new Color(c.r, c.g, c.b, _originalAlpha);
            _renderer.sprite = _originalSprite;
        }

        // 스케일 복원
        transform.localScale = _originalScale;
    }


    // SO 런타임에 교체
    // 반드시 ResetState() 이후, PlayActivate() 이전에 호출
    // 그래야 PlayActivate()가 교체된 SO의 onActivate를 실행
    public void SetVisualData(SkillVisualData data) => _data = data;


    // 유지 연출 재생 속도 배율 변경
    public void SetMaintainSpeed(float multiplier) => _durationSpeedMul = Mathf.Max(0.1f, multiplier);


    // 스프라이트를 고정값으로 교체
    // PlaySprites가 바꾸지 못하게 막음
    public void OverrideMainSprite(Sprite sprite)
    {
        _overrideSprite = sprite;
        if (_renderer != null && sprite != null)
            _renderer.sprite = sprite;
    }


    // 연출 시작
    public void StartSequence(float duration)
    {
        // 크기 저장
        _originalScale = transform.localScale;

        // 유지시간 저장
        _duration = duration;

        if (HasActions(_data?.onActivate))
        {
            // 발동 연출이 있으면 먼저 실행
            _currentPhaseState = PHASE_STATE.Activate;
            StartPhase(_data.onActivate, 0f, isExpire: false);
        }
        else if (HasActions(_data?.onDuration))
        {
            // 발동 연출이 아예 없으면 바로 유지 연출 시작
            _currentPhaseState = PHASE_STATE.Duration;
            // -1인 액션 시간 나누기
            float fill = FillDuration(_data.onDuration, _duration); 
            StartPhase(_data.onDuration, fill, isExpire: false);
        }
    }

    // 종료 연출 시작
    // 종료 연출 끝에 풀 반환 콜백
    public void RequestExpire(Action onDone)
    {
        if (_isExpiring) return;

        _isExpiring   = true;
        _onExpireDone = onDone;
        _phaseRunning = false;  // 진행 중이던 페이즈 즉시 중단
        _currentPhaseState = PHASE_STATE.Expire;

        // 액션 없으면 바로 콜백
        if (HasActions(_data?.onExpire) == false)
        {
            onDone?.Invoke();
            return;
        }

        // 종료 연출 시작 
        StartPhase(_data.onExpire, 0f, isExpire: true);
    }

    
    // 적 명중 시 onHit 이펙트 생성
    public void PlayHit(Vector2 enemyPos)
    {
        if (HasActions(_data?.onHit) == false) return;

        foreach (var action in _data.onHit)
        {
            // 이펙트 생성
            if (action.type == VISUALACTION_TYPE.HitEffect)
                SpawnHitEffect(action.prefab, enemyPos);
        }
    }


    // 페이즈 시작
    private void StartPhase(List<VisualAction> phase, float fillDuration, bool isExpire)
    {
        _currentPhase = phase;        // 페이즈
        _actionIndex = 0;             // 액션 배열 번호
        _fillDuration = fillDuration; // 채울 시간
        _phaseRunning = true;         // 페이즈 구동 상태
        _isExpirePhase = isExpire;    // 페이즈 만료 상태
        _startTime = Time.time;       // 시작 시간

        // 페이즈 0번 액션부터 시작
        StartAction(phase[0]);
    }


    // 액션 시작
    private void StartAction(VisualAction action)
    {
        _curType     = action.type;             // 액션 타입
        _curSprites  = action.sprites;          // 재생 스프라이트 배열
        _curCurve    = action.curve;            // 커브
        _curElapsed  = 0f;                      // 진행률
        _curLoop     = action.loop;             // 루프 여부
        _curFps      = action.fps;              // 루프 재생 속도

        // duration 가 -1이면 미리 계산해둔 _fillDuration으로 교체
        _curDuration = action.duration < 0f ? _fillDuration : action.duration;

        if (action.type == VISUALACTION_TYPE.AttachEffect)
        {
            // 이펙트 스폰 후 부착
            AttachEffect(action.prefab);
            // 다음 액션
            AdvanceAction();
            return;
        }

        if (action.type == VISUALACTION_TYPE.HitEffect)
        {
            // 다음 액션
            AdvanceAction();
            return;
        }

        // 다음 프레임까지 기다리지 않고 진행률 0 으로 갱신
        UpdateVisuals(0f);

        // duration이 0 이하이면 즉시 최종 상태 적용
        if (_curDuration <= 0f)
        {
            ApplyFinalState(action);
            AdvanceAction();
        }
    }


    // 현재 액션 완료 후 다음 액션으로
    private void AdvanceAction()
    {
        // 액션 번호
        _actionIndex++;

        // 액션 최대 수 넘어가면
        if (_actionIndex >= _currentPhase.Count)
        {
            // 발동 연출 끝난 상태면
            if (_currentPhaseState == PHASE_STATE.Activate)
            {
                if (HasActions(_data?.onDuration))
                {
                    // 유지 연출 시작
                    _currentPhaseState = PHASE_STATE.Duration;
                    // 발동으로부터 지난 시간
                    float activateElapsed = Time.time - _startTime;
                    // 남은 시간
                    float remaining = Mathf.Max(0f, _duration - activateElapsed);
                    // 남은시간만큼 유지 페이즈
                    float fill = FillDuration(_data.onDuration, remaining);
                    StartPhase(_data.onDuration, fill, isExpire: false);
                    return; // 다음 페이즈로 넘어갔으니 여기서 함수 종료
                }
            }

            // 페이즈 종료
            _phaseRunning = false;
            _currentPhaseState = PHASE_STATE.None;

            // 종료 페이즈면 추가 종료 로직 실행 (풀 반환)
            if (_isExpirePhase == true)
                _onExpireDone?.Invoke();

            return;
        }

        // 아직 액션 남아있으면 다음 액션 시작
        StartAction(_currentPhase[_actionIndex]);
    }


    // 스프라이트 갱신
    private void TickSprites(float t)
    {
        // 스프라이트 배열 비었거나, 렌더러 없거나, 고정 스프라이트 있으면 스킵
        if (_curSprites == null || _curSprites.Length == 0) return;
        if (_renderer == null || _overrideSprite != null) return;

        // 재생 스프라이트 수
        int count = _curSprites.Length;
        int index = 0;

        // 무한 반복
        if (_curLoop)
        {
            // 경과 시간에 fps 곱해서
            int totalFrames = Mathf.FloorToInt(_curElapsed * _curFps);

            // 진행된 총 프레임 계산 후 나머지로 무한 반복
            index = totalFrames % count;

            // 역재생이면 반대로
            if (_curReverse) index = (count - 1) - index;
        }
        else
        {
            // 진행률에 맞는 인덱스
            index = Mathf.Clamp(Mathf.FloorToInt(t * count), 0, count - 1);

            // 역재생이면 반대로
            if (_curReverse) index = (count - 1) - index;
        }
        // 스프라이트 변경
        _renderer.sprite = _curSprites[index];
    }


    // 페이드인아웃
    private void TickFade(float from, float to, float t)
    {
        if (_renderer == null) return;

        // 알파값 설정
        // 커브 null이면 t 그대로 써서 Linear
        // 커브 있으면 커브의 t
        SetAlpha(Mathf.Lerp(from, to, EvalCurve(t)));
    }

    // 크기 보간
    private void TickScale(Vector3 from, Vector3 to, float t)
    {
        // 커브가 1을 초과할 수 있다고 함
        // LerpUnclamped 로 제한 없이 보간
        transform.localScale = Vector3.LerpUnclamped(from, to, EvalCurve(t));
    }



    // duration이 0 초면 바로
    // 최종 상태로 변경
    private void ApplyFinalState(VisualAction action)
    {
        // 액션에 스프라이트 존재하면
        if (action.sprites != null && action.sprites.Length > 0 && _renderer != null && _overrideSprite == null)
        {
            // 마지막 스프라이트로 변경
            int last = action.sprites.Length - 1;
            _renderer.sprite = action.sprites[last];
        }

        switch (action.type)
        {
            case VISUALACTION_TYPE.PlaySprites:
                // 비정상작동 체크
                if (action.sprites != null && action.sprites.Length > 0 && _renderer != null && _overrideSprite == null)
                {
                    // 마지막 스프라이트
                    int last = action.sprites.Length - 1;
                    _renderer.sprite = action.sprites[last];
                }
                break;

            case VISUALACTION_TYPE.FadeIn:    SetAlpha(_originalAlpha);               break;
            case VISUALACTION_TYPE.FadeOut:   SetAlpha(0f);               break;
            case VISUALACTION_TYPE.ScaleUp:   transform.localScale = _originalScale; break;
            case VISUALACTION_TYPE.ScaleDown: transform.localScale = Vector3.zero;   break;
            case VISUALACTION_TYPE.FadeInAndScaleUp:
                SetAlpha(_originalAlpha);
                transform.localScale = _originalScale;
                break;
            case VISUALACTION_TYPE.FadeOutAndScaleDown:
                SetAlpha(0f);
                transform.localScale = Vector3.zero;
                break;
        }
    }


    // duration = -1 일 때 채울 시간 계산
    private float FillDuration(List<VisualAction> actions, float totalDuration)
    {
        float fixedTotal = 0f;      // 총 시간
        int fillCount  = 0;         // -1 인 액션 수

        foreach (var action in actions)
        {
            // 0보다 작으면 카운팅
            if (action.duration < 0f)
                fillCount++;
            // 0 이상이면 합산
            else
                fixedTotal += action.duration;
        }

        // -1 액션 있으면 음수 안되게 계산해서 반환
        return fillCount > 0
            ? Mathf.Max(0f, (totalDuration - fixedTotal) / fillCount)
            : 0f;
    }

    // 특정 위치에 적중 이펙트 생성
    private void SpawnHitEffect(PoolObject prefab, Vector2 pos)
    {
        // null 체크
        if (prefab == null) return;
        if (ObjectPoolManager.Instance == null) return;

        // 생성
        var obj = ObjectPoolManager.Instance.Spawn<PoolObject>(prefab, pos, Quaternion.identity);
    }

    // 이펙트 생성 후 투사체 하위 객체로
    private void AttachEffect(PoolObject prefab)
    {
        // null 체크
        if (prefab == null) return;
        if (ObjectPoolManager.Instance == null) return;

        // 생성
        var obj = ObjectPoolManager.Instance.Spawn<PoolObject>(prefab, transform.position, Quaternion.identity);

        if (obj == null) return;

        // 자식으로 부착
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // 부착 이펙트 목록에 추가
        _attachEffects.Add(obj);
    }

    // 스프라이트 알파값 설정
    private void SetAlpha(float alpha)
    {
        if (_renderer == null) return;
        var c = _renderer.color;
        _renderer.color = new Color(c.r, c.g, c.b, alpha);
    }

    // 커브 있으면 커브
    // 없으면 Linear
    private float EvalCurve(float t)
        => (_curCurve != null && _curCurve.length > 0) ? _curCurve.Evaluate(t) : t;

    // 페이즈에 액션 있는지
    private bool HasActions(List<VisualAction> list)
        => list != null && list.Count > 0;
}


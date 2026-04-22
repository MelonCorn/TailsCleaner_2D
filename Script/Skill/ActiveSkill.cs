using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ActiveSkillData;

public abstract class ActiveSkill : MonoBehaviour
{
    // --- 상수 ---
    public const int MAX_SKILL_LEVEL = 10;                  // 스킬 최대 레벨
    public const float MIN_SKILL_COOLDOWN = 0.1f;           // 스킬 최소 쿨타임

    // --- 인스펙터 ---
    [Header("발사 설정")]
    [Tooltip("스킬 실행 간격")]
    [SerializeField] protected float _fireInterval = 0.1f;  // 여러 투사체 발사 시 텀
    [Tooltip("발동 범위 설정")]
    [SerializeField] protected float _distance = 50f;       // 타겟 탐색 거리


    // --- 식별 정보 ---
    public int MainTag { get; private set; }                // 메인 태그
    public int CurrentLevel { get; private set; } = 0;      // 현재 레벨
    public int CurrentSubTag { get; private set; } = 0;     // 적용 중인 서브 태그

    protected GameObject _skillPrefab;  // 스킬 장판,투사체

    // --- 스킬 타입 ---
    protected ATTACK_TYPE _attackType;
    protected TARGETING_TYPE _targetingType;

    // --- 스탯 ---
    // 외부 참조용 (패시브 모디파이어, 스킬 투사체, 타겟)
    // ((baseStat + 패시브 깡 스탯) * commonStat + upgradeStat) * passiveMulStat * 최종 배율
    // ((baseStat + 깡 추가 패시브 스탯) * 공용 스탯 + (업그레이드 스탯 * 추가추가피해 패시브)) * 패시브 스탯 배율 합 * 최종 배율 (황금왕관, 양손잡이, 냥빨래)
    public SkillStat BaseStat => _baseStat;
    public SkillStat CommonStat => _commonStat;
    public SkillStat UpgradeStat => _upgradeStat;
    public SkillStat PassiveMulStat => _passiveMulStat;
    public SkillStat FinalStat => _finalStat;

    protected SkillStat _baseStat = new();                                  // 기본 스탯
    protected SkillStat _commonStat = SkillStat.CreateMultiplier();         // 공용 스탯    
    protected SkillStat _upgradeStat = new();                               // 업그레이드 스탯
    protected SkillStat _passiveMulStat = SkillStat.CreateMultiplier();     // 패시브 배율 합
    protected SkillStat _finalStat = new();                                 // 최종 스탯

    // 공용 업그레이드 더러움 제거 범위
    private float _pickupRangeBonus = 0f;
    public float PickupRangeBonus => _pickupRangeBonus;


    // --- 모디파이어 목록 ---
    protected List<(ActiveModifier modifier, ActiveUpgradeData upgradeData)> _skillModifiers = new();   // 스킬 전용
    public List<PassiveModifier> PassiveModifiers { get; private set; } = new();                        // 패시브



    // --- 업그레이드 별 레벨 ---
    // Key: active_skill_id, Value: UpgradeLevel
    protected Dictionary<int, int> _upgradeLevels = new();

    // --- 시전 타겟 / 쿨타임 ---
    public MonsterBase CurrentTarget => _currentTarget;
    protected MonsterBase _currentTarget = null;        // 타겟
    protected Coroutine _searchCoroutine = null;        // 탐색 코루틴
    protected float _lastActiveTime = 0f;               // 최근 스킬 실행 시간
    protected WaitForSeconds _fireDelay;                // 순차 발사 딜레이

    // --- 사운드 / 애니메이션 ---
    protected SkillSoundData _soundData;                // 사운드 데이터
    private string _aniName;                            // 애니메이션 이름


    // --- 투사체 런타임 계산용 ---
    protected SkillStatHandler SkillStatHandler => SkillManager.Instance.SkillStatHandler;  // 플레이어 스킬 스탯
    protected Vector2 PlayerPos => SkillManager.Instance.CurrentPlayerPos;                  // 현재 위치
    protected Vector2 AttackDir => GetAttackDir();                                          // 공격 방향


    private void Awake()
    {
        _fireDelay = new WaitForSeconds(_fireInterval);
    }


    // 초기화 (0티어 획득)
    public virtual void Init(ActiveSkillData skillData, ActiveUpgradeData upgradeData, GameObject prefab)
    {   
        // 메인 태그
        MainTag = skillData.MainTag;

        // 서브 태그
        AddSubTag(upgradeData);

        // 타입 설정
        _attackType = skillData.AttackType;
        _targetingType = skillData.TargetingType;

        // 스킬의 사용 프리팹
        _skillPrefab = prefab;

        // 레벨 1 시작
        CurrentLevel = 1;

        // 기본 스탯
        _baseStat = upgradeData.GetSkillStat();
        
        // 사운드 데이터
        _soundData = skillData.SoundData;
        
        // 애니메이션 타입
        _aniName = skillData.PlayerAniType switch
        {
            PLAYERSKILLANI_TYPE.Skill1 => PlayerAnimation.Skill1,
            PLAYERSKILLANI_TYPE.Skill2 => PlayerAnimation.Skill2,
            PLAYERSKILLANI_TYPE.Skill3 => PlayerAnimation.Skill3,
            _ => null
        };

        //Debug.Log($"[ActiveSkill] {upgradeData.Name} 생성 완료.");
    }

    protected virtual void Update()
    {
        // 스킬 선택창 / 보스 연출 / 기타 gameplay block 상태에서는 자동 발동 금지
        if (StageController.Instance != null && StageController.Instance.IsGameplayTemporarilyBlocked)
            return;

        // 쿨타임이 됐고 발동 조건도 맞으면 발동
        if (IsCooldownReady() && CanFire())
        {
            // 시전 애니메이션
            if (_aniName != null) SkillManager.Instance.Player.PlayAni(_aniName);
            
            // 스킬 시전
            Active();

            // 시전 시간 갱신
            _lastActiveTime = Time.time;
        }
    }


    private bool IsCooldownReady()
    {
        float cooldown = (SkillManager.Instance.Player.AttackSpeed / 100f) * _finalStat.Cooldown;
        
        float actualCooldown = Mathf.Max(MIN_SKILL_COOLDOWN, cooldown);
        return Time.time >= _lastActiveTime + actualCooldown;
    }

    protected virtual bool CanFire()
    {
        var player = SkillManager.Instance.Player;

        switch (_targetingType)
        {
            case TARGETING_TYPE.Barrier:                    // 베리어형    항상 발동
                return true;

            case TARGETING_TYPE.Activate:                   // 발동형      범위 내 가장 가까운 적
            case TARGETING_TYPE.Closest:                    // 조준형    
                return TryUpdateTarget();

            case TARGETING_TYPE.Directional:                // 이동방향형  이동 방향 있어야 발동
                return player.MoveDir.sqrMagnitude > 0f;

            default:
                return true;
        }
    }

    // 자식에서 쿨타임, 조건 변경 해야하면 이걸로 오버라이드
    protected virtual bool CanActive()
    {
        return IsCooldownReady() && CanFire();
    }

    // 타겟 갱신 시도
    protected bool TryUpdateTarget()
    {
        // 범위 내 가장 가까운 적 탐색
        MonsterBase target = SkillManager.Instance.FindClosestMonster(SkillManager.Instance.CurrentPlayerPos, _distance);

        if (target != null)
        {
            _currentTarget = target;
            return true;
        }

        _currentTarget = null;
        return false;
    }

    // 스킬 발동 로직
    private void Active()
    {
        // 발동 사운드 재생
        SoundManager.Instance.PlaySkillActiveSFX(_soundData);
        // 발사 코루틴
        StartCoroutine(ActiveCoroutine());
    }

    // 투사체 수만큼 순차 발사
    protected virtual IEnumerator ActiveCoroutine()
    {
        // 투사체 수
        int count = Mathf.Max(1, _finalStat.ProjectileCount);

        for (int i = 0; i < count; i++)
        {
            // 발사 불가 상태면 즉시 종료 (범위 내 적이 아예 없는 경우)
            if (CheckAndRetarget() == false) break;

            // 실제 발사 로직은 자식에서
            OnActive(i, count);

            // 발사 텀
            yield return _fireDelay;
        }
    }
    
    // 자식 개별 
    protected abstract void OnActive(int index, int totalCount);


    // 발동형, 조준형
    // 발동 시 타겟 유효성 검사 후
    // 새 타겟 필요 시 갱신
    protected virtual bool CheckAndRetarget()
    {
        switch (_targetingType)
        {
            case TARGETING_TYPE.Activate:
            case TARGETING_TYPE.Closest:

                // 기존 타겟 살아있으면 바로 리턴
                if (IsValidTarget()) return true;

                // 타겟이 사라지면 새 타겟 갱신 (찾으면 true, 아예 없으면 false)
                return TryUpdateTarget();

            default:
                // 베리어형, 이동방향형은 일단 걍 true
                return true;
        }
    }
    // 타겟이 유효한지 체크
    protected bool IsValidTarget()
    {
        return _currentTarget != null &&
               _currentTarget.gameObject.activeInHierarchy &&
               _currentTarget.hp > 0;
    }


    // 스킬 업그레이드
    public virtual void ApplyUpgrade(ActiveUpgradeData upgradeData)
    {
        AddSubTag(upgradeData);         // 업그레이드의 서브 태그 추가
        LevelUp(upgradeData);           // 스킬 레벨, 업그레이드 레벨 증가
        AddStat(upgradeData);           // 공용, 업그레이드 스탯 누적
        AddModifier(upgradeData);       // 전용 모디파이어 추가

        // 더러움 제거 범위
        if (upgradeData.PickupRange != 0f)
            _pickupRangeBonus += upgradeData.PickupRange;

        // 자식인 GenericActiveSkill에서
        // 전용 모디파이어 다 설정하고
        // 패시브 설정하면서 스탯, 로직 적용(RecheckPassives)
    }


    // 서브 태그 추가

    private void AddSubTag(ActiveUpgradeData upgradeData)
    {
        // 업그레이드의 서브 태그 추가
        if (upgradeData.SubTag1 != 0) CurrentSubTag |= SubTagRegistry.GetFlag(upgradeData.SubTag1);
        if (upgradeData.SubTag2 != 0) CurrentSubTag |= SubTagRegistry.GetFlag(upgradeData.SubTag2);
    }

    // 레벨 증가
    private void LevelUp(ActiveUpgradeData upgradeData)
    {
        // 레벨 증가
        CurrentLevel++;

        // 업그레이드 처음일 시 추가
        if (_upgradeLevels.ContainsKey(upgradeData.Id) == false)
        {
            _upgradeLevels.Add(upgradeData.Id, 0);
        }

        // 업그레이드 레벨 증가
        _upgradeLevels[upgradeData.Id]++;
    }


    // 스탯 누적
    private void AddStat(ActiveUpgradeData upgradeData)
    {
        // 공용
        if (upgradeData.MainTag == SkillDataLoader.COMMON_MAIN_TAG)
        {
            // 곱연산 누적
            SkillStat multiplier = upgradeData.GetSkillStat();
            _commonStat.Multiply(multiplier);
        }
        // 전용
        else
        {
            // 합연산 누적
            _upgradeStat.Add(upgradeData.GetSkillStat());
        }
    }

    // 전용 모디파이어 추가
    private void AddModifier(ActiveUpgradeData upgradeData)
    {
        // id에 맞는 전용 모디파이어 가져오기
        ActiveModifier modifier = SkillDataLoader.GetActiveModifier(upgradeData.Id);
        
        if (modifier != null)
        {
            // 이미 있는 모디파이어는 스킵
            if (_skillModifiers.Exists(pair => pair.upgradeData.Id == upgradeData.Id)) return;

            // 모디파이어 추가
            _skillModifiers.Add((modifier, upgradeData));
        }
    }

    // 초기화, 업그레이드 시 스탯 설정
    protected void CalculateStats()
    {
        _finalStat = GetFinalStat(_baseStat, _commonStat, _upgradeStat, _passiveMulStat);
    }

    // 최종 스탯 계산 후 반환 (초기화, 업그레이드, 투사체 내부 로직)
    public SkillStat GetFinalStat(SkillStat baseStat, SkillStat commonStat, SkillStat upgradeStat, SkillStat passiveMulStat)
    {
        // 기본 스탯 생성
        SkillStat resultStat = baseStat.Clone();

        // 패시브 스탯 (합)
        foreach (var passive in SkillManager.Instance.MyPassiveSkills)
        {
            // 서브 태그 매치 안되면 스킵
            if (IsPassiveMatch(passive) == false) continue;

            // 아니면 기본 스탯에 합
            passive.Modifier.ModifyBaseAdd(resultStat);
        }

        // 기본 관통력 0인데 관통 공용 업그레이드 했을 때
        if (resultStat.PierceCount == 0 && commonStat.PierceCount > 1)
        {
            // 0 * 2 방지 위해서 1 추가
            // 근데 하드코딩임
            resultStat.PierceCount = 1;
        }

        // 공용 스탯 (곱)
        resultStat.Multiply(commonStat);

        // 업그레이드 스탯 (합)
        resultStat.Add(upgradeStat);

        // 패시브 배율 합 (곱)
        resultStat.Multiply(passiveMulStat);

        // 최종 배율 (곱)
        foreach (var passive in SkillManager.Instance.MyPassiveSkills)
        {
            // 서브 태그 매치 안되면 스킵
            if (IsPassiveMatch(passive) == false) continue;

            // 아니면 스탯에 곱
            passive.Modifier.ModifyFinalMultiply(resultStat);
        }

        // 최종 스탯 = ((baseStat + 패시브 깡 스탯) * 공용 스탯 + (업그레이드 스탯 * 추가추가피해 패시브)) * 패시브 스탯 배율 합 * 최종 배율 (황금왕관, 양손잡이, 냥빨래)
        //Debug.Log($"최종 공격력 : {resultStat.Damage} = (( {baseStat.Damage}(기본) + 패시브 깡 스탯) * {commonStat.Damage}(공용)  + ({upgradeStat.Damage}(업그레이드) * 추가추가피해)) * {passiveMulStat.Damage}(패시브 배율) * 패시브 최종 배율");
        //Debug.Log($"최종 지속시간 : {resultStat.Duration} = (( {baseStat.Duration}(기본) + 패시브 깡 스탯) * {commonStat.Duration}(공용)  + ({upgradeStat.Duration}(업그레이드) * 추가추가피해)) * {passiveMulStat.Duration}(패시브 배율) * 패시브 최종 배율");
        //Debug.Log($"최종 틱 주기 : {resultStat.TickRate} = (( {baseStat.TickRate}(기본) + 패시브 깡 스탯) * {commonStat.TickRate}(공용)  + ({upgradeStat.TickRate}(업그레이드) * 추가추가피해)) * {passiveMulStat.TickRate}(패시브 배율) * 패시브 최종 배율");
        //Debug.Log($"최종 투사체 수 : {resultStat.ProjectileCount} = (( {baseStat.ProjectileCount}(기본) + 패시브 깡 스탯) * {commonStat.ProjectileCount}(공용)  + ({upgradeStat.ProjectileCount}(업그레이드) * 추가추가피해)) * {passiveMulStat.ProjectileCount}(패시브 배율) * 패시브 최종 배율");
        //Debug.Log($"최종 넉백 : {resultStat.Knockback} = (( {baseStat.Knockback}(기본) + 패시브 깡 스탯) * {commonStat.Knockback}(공용)  + ({upgradeStat.Knockback}(업그레이드) * 추가추가피해)) * {passiveMulStat.Knockback}(패시브 배율) * 패시브 최종 배율");

        // 최종 결과 스탯 반환
        return resultStat;
    }

    // 패시브 스탯 배율 합 설정
    private void SetPassiveMulStat()
    {
        // 기본 생성
        _passiveMulStat = SkillStat.CreateMultiplier();

        // 패시브 순회하면서
        foreach (var passive in SkillManager.Instance.MyPassiveSkills)
        {
            // 배율 합치기
            if (IsPassiveMatch(passive) == false) continue;
            passive.Modifier.ModifyMul(_passiveMulStat);
        }
    }

    // 패시브 적용
    protected void AddPassiveModifier()
    {
        PassiveModifiers.Clear();

        // 보유 패시브 순회
        foreach (var passive in SkillManager.Instance.MyPassiveSkills)
        {
            if (IsPassiveMatch(passive) == false) continue;

            PassiveModifiers.Add(passive.Modifier);
        }
    }

    // 업그레이드의 현재 레벨
    public int GetUpgradeLevel(int upgradeId)
    {
        // 업그레이드 기록 있으면
        if (_upgradeLevels.TryGetValue(upgradeId, out int level))
        {
            // 현재 레벨 반환
            return level;
        }

        // 없으면 0
        return 0;
    }

    // 패시브 재적용
    // 스킬 업그레이드, 패시브 습득 시 호출
    public virtual void RecheckModifiers()
    {
        SetPassiveMulStat();    // 패시브 배율 합
        AddPassiveModifier();   // 패시브 모디파이어
        CalculateStats();       // finalStat 계산
    }
    
    // 패시브 서브태그가 현재 스킬에 해당하는지
    private bool IsPassiveMatch(PassiveSkillData passive)
    {
        int flag = SubTagRegistry.GetFlag(passive.SubTag);
        return flag != 0 && (CurrentSubTag & flag) != 0;
    }

    private Vector2 GetAttackDir()
    {
        var player = SkillManager.Instance.Player;

        switch (_targetingType)
        {
            case TARGETING_TYPE.Barrier:                    // 베리어형    항상 발동
                return SkillManager.Instance.CurrentPlayerPos;

            case TARGETING_TYPE.Activate:                   // 발동형      타겟 있을 때만
            case TARGETING_TYPE.Closest:                    // 조준형     

                if (_currentTarget != null)
                    return (_currentTarget.Position - SkillManager.Instance.CurrentPlayerPos).normalized;

                // 혹시나 발사하는데 타겟 없으면 아래로
                return Vector2.down;

            case TARGETING_TYPE.Directional:                // 이동방향형  이동 방향
                return player.MoveDir;

            default:
                return SkillManager.Instance.CurrentPlayerPos;
        }
    }


    // 스킬 업그레이드로 플레이어 스탯 영구 증가 (세탁파도, 회전장난감)
    public virtual void ModifyPlayerBonus(PlayerStatFlat flat, PlayerStatMul mul) { }


    // 루프 사운드 재생
    protected void StartLoopSFX()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySkillLoopSFX(MainTag, _soundData);
    }

    // 루프 사운드 중지
    protected void StopLoopSFX()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.StopSkillLoopSFX(MainTag);
    }

    // 만료 사운드 재생
    protected void PlayExpireSFX()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySkillExpireSFX(_soundData);
    }

    // 특수 사운드 재생
    protected void PlaySpecialSFX()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySkillSpecialSFX(_soundData);
    }
}

// 프리팹과 모디파이어 타입 결정
public abstract class ActiveSkill<TSkillObject, TModifierData> : ActiveSkill
    where TSkillObject : PoolObject
    where TModifierData : class, new()
{
    protected TSkillObject _skillObjectPrefab;
    public TModifierData _modifierData = new TModifierData();


    // 프리팹 캐싱
    public override void Init(ActiveSkillData skillData, ActiveUpgradeData upgradeData, GameObject prefab)
    {
        // 부모 Init 먼저
        base.Init(skillData, upgradeData, prefab);

        if (_skillPrefab != null)
        {
            _skillObjectPrefab = _skillPrefab.GetComponent<TSkillObject>();
        }
    }

    // 데이터 갱신
    public override void ApplyUpgrade(ActiveUpgradeData upgradeData)
    {
        base.ApplyUpgrade(upgradeData);

        // 모디파이어 데이터 갱신
        _modifierData = new TModifierData();

        foreach (var pair in _skillModifiers)
        {
            // 모디파이어 적용
            pair.modifier.Apply(this, pair.upgradeData);
        }

        Debug.Log($"[ActiveSkill] 업그레이드 완료: [{SkillDataLoader.GetActiveSkillData(MainTag).SkillName}] (MainTag : {MainTag})\n" +
                  $" - 업그레이드 : {upgradeData.Name} (Active_Skill_ID : {upgradeData.Id})\n" +
                  $" - 업그레이드 Lv : {GetUpgradeLevel(upgradeData.Id)} / {upgradeData.MaxLevel}\n" +
                  $" - 스킬 전체 Lv : {CurrentLevel} / {MAX_SKILL_LEVEL}");
    }


    // 메인 프리팹 풀에서 꺼내기
    protected TSkillObject SpawnFromPool(Vector3 pos, Quaternion rot)
    {
        return SpawnFromPool<TSkillObject>(_skillObjectPrefab, pos, rot);
    }

    // 메인, 서브 프리팹 풀에서 꺼내기
    protected T SpawnFromPool<T>(T prefab, Vector3 pos, Quaternion rot) where T : PoolObject
    {
        // 풀 매니저 체크
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning("[ActiveSkill] ObjectPoolManager 없음");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkill] 풀에 요청한 프리팹이 null입니다.");
            return null;
        }

        return ObjectPoolManager.Instance.Spawn<T>(prefab, pos, rot);
    }
}

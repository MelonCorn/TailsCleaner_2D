using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LevelUpSelect))]
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    public const int MAX_ACTIVE_SLOTS = 6;              // 최대 액티브 스킬 수
    public const int MAX_PASSIVE_SLOTS = 6;             // 최대 패시브 스킬 수
    public const int DEFAULT_ACTIVE_MAIN_TAG = 41007;   // 기본 지급 스킬 메인 태그 (세제 캡슐 41007)
    public const float DEFAULT_SEARCH_RADIUS = 100f;    // 가장 가까운 적 탐색용 범위
    public const float SEARCH_INTERVAL = 0.1f;          // 탐색 주기
    public const int MONSTER_BUFFER_COUNT = 150;        // 몬스터 콜라이더 버퍼 크기

    // 탐색 간격
    public WaitForSeconds SearchInterval { get; } = new WaitForSeconds(SEARCH_INTERVAL);

    // 플레이어 보유 스킬 리스트
    public List<ActiveSkill> MyActiveSkills { get; private set; } = new();
    public List<PassiveSkillData> MyPassiveSkills { get; private set; } = new();


    // 스킬 슬롯 체크
    public bool IsActiveSlotFull => MyActiveSkills.Count >= MAX_ACTIVE_SLOTS;
    public bool IsPassiveSlotFull => MyPassiveSkills.Count >= MAX_PASSIVE_SLOTS;


    public PlayerBase Player { get; private set; }                  // 플레이어
    public Vector2 CurrentPlayerPos => Player.transform.position;   // 플레이어 위치
    public TargetingSystem TargetingSystem { get; private set; }    // 타겟 시스템 (사용 안 할 예정)
    public SkillStatHandler SkillStatHandler { get; private set; }
    public LayerMask MonsterLayer => _monsterLayer;
    
    
    // 몬스터 콜라이더 버퍼
    private Collider2D[] _monsterBuffer = new Collider2D[MONSTER_BUFFER_COUNT];
    // 버퍼의 실 몬스터 수
    private int _monsterHitCount = 0;
    // 콜라이더 필터
    private ContactFilter2D _monsterFilter = new();


    [Header("적 레이어")]
    [SerializeField] LayerMask _monsterLayer; 


    private void Awake()
    {
        Instance = this;
        Player = GetComponent<PlayerBase>();
        SkillStatHandler = GetComponent<SkillStatHandler>();
        TargetingSystem = new TargetingSystem(Player.transform, _monsterLayer);

        // 필터 설정
        _monsterFilter.useLayerMask = true;         // 레이어마스크 설정
        _monsterFilter.layerMask = _monsterLayer;   // 설정 레이어
        _monsterFilter.useTriggers = true;          // 트리거 콜라이더 사용
    }

    private void Start()
    {
        // 스킬 데이터 불러오기
        SkillDataLoader.Init();

        // 근접 몬스터 콜라이더 배열 탐색
        StartCoroutine(MonsterSearchCoroutine());

        // 기본 스킬 추가
        // 기획서 상에서는 못봤는데 기본 공격이 없으면 공격 못하니까 일단 추가
        AddDefaultSkill();


        // 테스트용 업그레이드 
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42001]); // 라쿤
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42004]); // 추가추가피해
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42006]); // 객기
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42007]); // 청소용 비닐옷
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42008]); // 고전비급
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42010]); // 황금왕관
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42012]); // 스노우볼링
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42013]); // 양손잡이
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42016]); // 냥빨래
        //ApplyPassiveOption(SkillDataLoader.PassiveSkillMap[42017]); // 쉽게 피함

        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40001));  // 비누 거품
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40002));  // 자동 추적 비누 지우개
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40003));  // 버블버블
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40004));  // 빨래당함
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40005));  // 흐르는 거품
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40006));  // 세제온더락
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40007));  // 슬랩스틱

        //ApplyActiveOption(41002, SkillDataLoader.GetActiveUpgradeData(40009));  // 비누 던지기
        //ApplyActiveOption(41002, SkillDataLoader.GetActiveUpgradeData(40011));  // 감나빗!
        //ApplyActiveOption(41002, SkillDataLoader.GetActiveUpgradeData(40016));  // 비누덩어리

        //ApplyActiveOption(41003, SkillDataLoader.GetActiveUpgradeData(40018));  // 물바다
        //ApplyActiveOption(41003, SkillDataLoader.GetActiveUpgradeData(40019));  // 소용돌이

        //ApplyActiveOption(41004, SkillDataLoader.GetActiveUpgradeData(40021));  // 타올 휘두르기
        //ApplyActiveOption(41004, SkillDataLoader.GetActiveUpgradeData(40022));  // 타올 리사이클 
        //ApplyActiveOption(41004, SkillDataLoader.GetActiveUpgradeData(40025));  // 타올 휘두르며

        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40026));  // 걸레 휘두르기
        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40027));  // 걸레 리사이클
        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40030));  // 걸레 휘두르며

        //ApplyActiveOption(41006, SkillDataLoader.GetActiveUpgradeData(40031));  // 세탁 파도
        //ApplyActiveOption(41006, SkillDataLoader.GetActiveUpgradeData(40033));  // 밀물 썰물
        //ApplyActiveOption(41006, SkillDataLoader.GetActiveUpgradeData(40034));  // 탈수
        //ApplyActiveOption(41006, SkillDataLoader.GetActiveUpgradeData(40035));  // 차오르는 체력

        //ApplyActiveOption(41007, SkillDataLoader.GetActiveUpgradeData(40037));  // 상큼하게 터져볼래?
        //ApplyActiveOption(41007, SkillDataLoader.GetActiveUpgradeData(40040));  // 1만 시간의 법칙

        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40041));  // 회전 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40042));  // 기차 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40043));  // 팽이 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40044));  // 달 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40045));  // 오리 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40046));  // 해적선 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40047));  // 상어 장난감
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40048));  // 물놀이 끝

        //ApplyActiveOption(41009, SkillDataLoader.GetActiveUpgradeData(40049));  // 일광건조
        //ApplyActiveOption(41009, SkillDataLoader.GetActiveUpgradeData(40050));  // 따스한 태양
        //ApplyActiveOption(41009, SkillDataLoader.GetActiveUpgradeData(40053));  // 이불 두르기
        //ApplyActiveOption(41009, SkillDataLoader.GetActiveUpgradeData(40054));  // 두꺼운 이불
        //ApplyActiveOption(41009, SkillDataLoader.GetActiveUpgradeData(40055));  // 기상!

        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40056));  // 블루투스 샤워기
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40057));  // 수압 최대로!
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40058));  // 온수샤워
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40059));  // 냉수마찰
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40060));  // 예열완료
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40061));  // 방수코팅
        //ApplyActiveOption(41010, SkillDataLoader.GetActiveUpgradeData(40062));  // 키친건!

        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40065));  // 쿨타임
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40065));  // 쿨타임
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40065));  // 쿨타임
        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41005, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40066));  // 지속 시간
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41002, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41003, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41004, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41001, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41002, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41003, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41004, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
        //ApplyActiveOption(41008, SkillDataLoader.GetActiveUpgradeData(40067));  // 연사
    }


    // 보유중인 메인 태그의 액티브 스킬 반환
    public ActiveSkill GetActiveSkill(int mainTag)
    {
        // 보유 중인 액티브 스킬 중
        for (int i = 0; i < MyActiveSkills.Count; i++)
        {
            // 입력 태그와 같은 스킬 있으면 반환
            if (MyActiveSkills[i].MainTag == mainTag) return MyActiveSkills[i];
        }

        // 없으면 null 반환
        return null;
    }

    // 메인 태그의 0티어 반환
    public ActiveUpgradeData GetTierZeroData(int mainTag)
    {
        // 해당 스킬의 업그레이드 리스트 가져오기
        var list = SkillDataLoader.GetActiveUpgradeDatas(mainTag);

        // 리스트에서 0티어 찾아 반환
        return list.Find(data => data.Tier == 0);
    }

    // 모든 스킬 만렙인지 확인
    public bool IsAllActiveMaxLevel()
    {
        // 액티브 스킬 없으면
        if (MyActiveSkills.Count == 0) return false;

        // 활성화된 액티브 스킬 순회
        for (int i = 0; i < MyActiveSkills.Count; i++)
        {
            // 액티브 스킬이 최대 레벨 보다 낮으면
            if (MyActiveSkills[i].CurrentLevel < ActiveSkill.MAX_SKILL_LEVEL) return false;
        }

        // 전부 다 만렙이라면
        return true;
    }


    // 보유 패시브 중 해당 패시브 있는지
    public bool HasPassive<T>(out T modifier) where T : PassiveModifier
    {
        modifier = null;

        foreach (var passive in MyPassiveSkills)
        {
            if (passive.Modifier is T mod)
            {
                modifier = mod;
                return true;
            }
        }

        return false;
    }

    #region 선택지 적용

    // 액티브 선택지 적용
    public void ApplyActiveOption(int targetMainTag, ActiveUpgradeData upgradeData)
    {
        // 0티어 신규 생성
        if (upgradeData.Tier == 0)
        {
            // 액티브 데이터
            ActiveSkillData skillData = SkillDataLoader.GetActiveSkillData(targetMainTag);

            // 스킬 프리팹을 자식으로 생성
            ActiveSkill newSkill = Instantiate(skillData.SkillPrefab, transform);

            // 스킬 투사체 프리팹
            GameObject projectile = skillData.SkillProjectilePrefab;

            // 위치 초기화
            newSkill.transform.localPosition = Vector2.zero;

            // 초기화
            newSkill.Init(skillData, upgradeData, projectile);

            // 보유 스킬에 추가
            if (MyActiveSkills.Contains(newSkill) == false)
            {
                MyActiveSkills.Add(newSkill);
            }

            // 모든 스킬 모디파이어 갱신
            RecheckAllModifiers();
        }
        // 1티어 이상 스킬 업그레이드
        else
        {
            // 보유 중인 액티브 스킬인지 체크
            ActiveSkill skill = GetActiveSkill(targetMainTag);

            // 유효하면 업그레이드 데이터 적용
            if (skill != null)
            {
                skill.ApplyUpgrade(upgradeData);

                // 모든 스킬 모디파이어 갱신
                RecheckAllModifiers();
            }
        }
    }

    // 모든 스킬 모디파이어 갱신 (전용, 패시브)
    public void RecheckAllModifiers()
    {
        foreach (var skill in MyActiveSkills)
            skill.RecheckModifiers();

        // 영구 플레이어 보너스 재계산
        SkillStatHandler.RecheckPermanent();
    }

    // 패시브 선택지 적용
    public void ApplyPassiveOption(PassiveSkillData passiveData)
    {
        if (passiveData == null) return;

        // 패시브에 추가
        MyPassiveSkills.Add(passiveData);

        // 모든 액티브 스킬에 모디파이어 갱신
        RecheckAllModifiers();

        Debug.Log($"[SkillManager] 패시브 획득: {passiveData.PassiveName} (SubTag: {passiveData.SubTag})");
    }

    #endregion


    #region 초기화

    // 시작 기본 스킬
    private void AddDefaultSkill()
    {
        // 메인태그의 0번 업그레이드 가져오기 (0티어 스킬 그자체)
        ActiveUpgradeData defualtSkillData = SkillDataLoader.GetActiveUpgradeDatas(DEFAULT_ACTIVE_MAIN_TAG)[0];

        if (defualtSkillData != null)
        {
            //Debug.Log($"기본 스킬 {defualtSkillData.Name} ({DEFAULT_ACTIVE_MAIN_TAG}) 지급");
            ApplyActiveOption(DEFAULT_ACTIVE_MAIN_TAG, defualtSkillData);
        }
        else
        {
            Debug.Log($"{DEFAULT_ACTIVE_MAIN_TAG}의 0티어가 존재하지 않음");
        }
    }

    #endregion


    #region 몬스터 탐색

    // 몬스터 탐색
    private IEnumerator MonsterSearchCoroutine()
    {
        while (true)
        {
            // 플레이어 중심화면 덮고도 남을 정도로 넓게 탐색
            // OverlapCircleNonAlloc가 유니티 6 오면서 OverlapCircle에서 필터 사용하게 변경됨
            _monsterHitCount = Physics2D.OverlapCircle(CurrentPlayerPos, DEFAULT_SEARCH_RADIUS, _monsterFilter, _monsterBuffer);

            yield return SearchInterval;
        }
    }

    // 특정 위치 기준 가장 가까운 몬스터 탐색
    public MonsterBase FindClosestMonster(Vector2 origin, float radius = DEFAULT_SEARCH_RADIUS, MonsterBase ignoreTarget = null)
    {
        // 반환할 몬스터
        MonsterBase closest = null;

        // 탐색 반경 내
        float minSqrDist = radius * radius;

        // 버퍼 수 만큼 탐색
        for (int i = 0; i < _monsterHitCount; i++)
        {
            Collider2D collider = _monsterBuffer[i];

            // 유효성 체크 (죽었거나 꺼진 거 무시)
            if (collider == null || collider.gameObject.activeInHierarchy == false) continue;

            // 몬스터 컴포넌트 참조 시도
            if (collider.TryGetComponent<MonsterBase>(out var monster))
            {
                // 체력이 0 이하면 스킵
                if (monster.hp <= 0) continue;
                // 무시할 타겟 있고, 현재 체크 대상이면 스킵
                if (ignoreTarget != null && monster == ignoreTarget) continue;

                // 거리 (몬스터에 Rigidbody 붙어있다는 가정하에 attachedRigidbody 사용)
                float sqrDist = (origin - collider.attachedRigidbody.position).sqrMagnitude;

                // 최소 거리, 몬스터 갱신
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = monster;
                }
            }
        }

        return closest;
    }
    #endregion
}

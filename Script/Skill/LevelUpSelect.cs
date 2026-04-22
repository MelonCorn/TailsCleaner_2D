using System.Collections.Generic;
using UnityEngine;

public class LevelUpSelect : MonoBehaviour
{
    public enum OPTION_TYPE  // 선택지 타입
    {
        Active,
        Passive,
        Heal
    }

    public class SelectOptionInfo             // 선택한 선택지의 정보
    {
        public OPTION_TYPE Type;              // 선택지 타입
        public int TargetMainTag;             // 목표 메인 태그

        // 액티브
        public ActiveUpgradeData ActiveData;  // 업그레이드 데이터
        // 패시브
        public PassiveSkillData PassiveData;
        // 회복
        public FloatEventChannelSO OnHealSelect;   // 회복 이벤트

        // 액티브 생성자
        public SelectOptionInfo(int mainTag, ActiveUpgradeData data)
        {
            Type = OPTION_TYPE.Active;
            TargetMainTag = mainTag;
            ActiveData = data;
        }

        // 패시브 생성자
        public SelectOptionInfo(PassiveSkillData data)
        {
            Type = OPTION_TYPE.Passive;
            PassiveData = data;
        }

        // 회복 생성자
        public SelectOptionInfo(FloatEventChannelSO healEvent)
        {
            Type = OPTION_TYPE.Heal;
            OnHealSelect = healEvent;
        }
    }


    public const int MAX_SELECT_OPTIONS = 3;      // 최대 선택지 수
    public const int PASSIVE_SELECT_LEVEL = 5;    // 패시브 선택 레벨
    public const int ACTIVE_TIER_TWO_LEVEL = 4;   // 티어 2 레벨
    public const int ACTIVE_TIER_THREE_LEVEL = 7; // 티어 3 레벨
    public const int HEAL_OPTION_RATIO = 30;      // 체력 회복 비율

    // 현재 선택지
    private List<SelectOptionInfo> _currentOptions = new List<SelectOptionInfo>();

    public IReadOnlyList<SelectOptionInfo> CurrentOptions => _currentOptions;

    [Header("체력 회복 선택지 이벤트 채널")]
    [SerializeField] FloatEventChannelSO _onHealSelect;   // 회복 이벤트

    // 선택지 옵션 설정
    public void GenerateOptions(int level)
    {
        _currentOptions.Clear();

        var skillManager = SkillManager.Instance;

        // 패시브 선택 레벨 아니면
        if (level % PASSIVE_SELECT_LEVEL != 0)
        {
            // 액티브 선택지 옵션 추가
            GenerateActiveOption(skillManager);

            // 선택지 칸 남아있으면
            if (_currentOptions.Count < MAX_SELECT_OPTIONS)
                // 패시브 선택지 옵션 추가
                GeneratePassiveOption(skillManager);
        }
        // 패시브 선택 레벨
        else
        {
            // 패시브 선택지 옵션 추가
            GeneratePassiveOption(skillManager);

            // 선택지 칸 남아있으면
            if (_currentOptions.Count < MAX_SELECT_OPTIONS)
                // 액티브 선택지 옵션 추가
                GenerateActiveOption(skillManager);
        }

        // 그래도 다 못 채웠을 경우엔
        // 다 채울 때까지
        while (_currentOptions.Count < MAX_SELECT_OPTIONS)
        {
            // 회복 추가 로직
            _currentOptions.Add(new SelectOptionInfo(_onHealSelect));
        }
    }

    // 액티브 선택지 옵션
    private void GenerateActiveOption(SkillManager skillManager)
    {
        // 상태 체크 (액티브 최대, 레벨 최대)
        bool isActiveFull = skillManager.IsActiveSlotFull;
        bool isAllLevelMax = skillManager.IsAllActiveMaxLevel();

        // 두 조건 중 하나라도 아닐 때
        // 액티브 스킬 후보 생성
        if (isActiveFull == false || isAllLevelMax == false)
        {
            // 메인 태그 후보 추리기 (0티어)
            List<int> candidateMainTags = GetCandidateMainTags(skillManager);

            // 추려진 후보 셔플
            Shuffle(candidateMainTags);

            // 섞은 메인 태그 순회
            foreach (int mainTag in candidateMainTags)
            {
                // 선택지 꽉 찼으면 중단
                if (_currentOptions.Count >= MAX_SELECT_OPTIONS) break;

                // 메인 태그의 랜덤 업그레이드 뽑기
                var upgradeData = GetRandomSkillUpgrade(skillManager, mainTag);

                // 유효하면 선택지에 추가
                if (upgradeData != null)
                    _currentOptions.Add(new SelectOptionInfo(mainTag, upgradeData));
            }
        }
    }

    // 패시브 선택지 옵션
    private void GeneratePassiveOption(SkillManager skillManager)
    {
        // 패시브 추가 로직
        // 패시브 슬롯 체크
        bool isPassiveFull = skillManager.IsPassiveSlotFull;

        // 패시브 자리 남으면
        if (isPassiveFull == false)
        {
            // 패시브 후보 목록
            List<PassiveSkillData> candidatePassives = GetCandidatePassives(skillManager);
            // 셔플
            Shuffle(candidatePassives);

            foreach (var passive in candidatePassives)
            {
                if (_currentOptions.Count >= MAX_SELECT_OPTIONS) break;
                _currentOptions.Add(new SelectOptionInfo(passive));
            }
        }
    }


    // 메인 태그 후보 추리기
    private List<int> GetCandidateMainTags(SkillManager skillManager)
    {
        // 임시 태그 후보 리스트
        List<int> tempMainTags = new List<int>();

        // 보유 중인 액티브 스킬 중
        foreach (ActiveSkill skill in skillManager.MyActiveSkills)
        {
            // 최대 레벨 아니면 후보에 추가
            if (skill.CurrentLevel < ActiveSkill.MAX_SKILL_LEVEL)
                tempMainTags.Add(skill.MainTag);
        }

        // 신규 스킬 (액티브 스킬 슬롯 남으면)
        if (skillManager.MyActiveSkills.Count < SkillManager.MAX_ACTIVE_SLOTS)
        {
            // 스킬 맵 순회 하면서
            foreach (int mainTag in SkillDataLoader.UpgradeMap.Keys)
            {
                // 액티브 스킬 등록 안되어있으면 스킵
                if (SkillDataLoader.GetActiveSkillData(mainTag) == null) continue;

                // 미습득 스킬 후보에 추가
                if (skillManager.GetActiveSkill(mainTag) == null)
                    tempMainTags.Add(mainTag);
            }
        }

        // 메인 태그 후보 반환
        return tempMainTags;
    }


    // 메인 태그의 랜덤 업그레이드 뽑기
    private ActiveUpgradeData GetRandomSkillUpgrade(SkillManager skillManager, int mainTag)
    {
        // 업그레이드 맵에 메인 태그가 등록되어있지 않다면 null 반환
        if (SkillDataLoader.UpgradeMap.TryGetValue(mainTag, out List<ActiveUpgradeData> upgradeList) == false)
        {
            Debug.LogError($"[UpgradeSelect] {mainTag} 가 업그레이드 맵에 등록되지 않았음");
            return null;
        }

        // 보유한 메인 태그 스킬 가져오기
        ActiveSkill mySkill = skillManager.GetActiveSkill(mainTag);

        // 없으면 신규 스킬 바로 후보에 추가 (0티어)
        if (mySkill == null)
        {
            return skillManager.GetTierZeroData(mainTag);
        }

        // 보유 스킬 (티어 계산)
        int level = mySkill.CurrentLevel;
        List<int> tiers = new List<int>();

        // 레벨 별 티어
        tiers.Add(1);
        if (level >= ACTIVE_TIER_TWO_LEVEL) tiers.Add(2);
        if (level >= ACTIVE_TIER_THREE_LEVEL) tiers.Add(3);

        // 임시 업그레이드 후보
        List<ActiveUpgradeData> candidates = new List<ActiveUpgradeData>();

        // 업그레이드 리스트에서 조건에 맞는 데이터 필터링
        foreach (var upgradeData in upgradeList)
        {
            // 티어 체크 (현재 레벨에서 나올 수 있는지)
            if (tiers.Contains(upgradeData.Tier) == false) continue;
            // 만렙 체크 (이미 업그레이드를 다 했는지)
            if (mySkill.GetUpgradeLevel(upgradeData.Id) >= upgradeData.MaxLevel) continue;

            // 통과하면 후보에 추가
            candidates.Add(upgradeData);
        }

        // 하나라도 있으면 그 중 랜덤
        if (candidates.Count > 0)
        {
            int rand = Random.Range(0, candidates.Count);
            return candidates[rand];
        }

        // 조건에 맞는 업그레이드 없음
        return null;
    }


    // 셔플 (Fisher-Yates)
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    // 패시브 후보 목록
    private List<PassiveSkillData> GetCandidatePassives(SkillManager skillManager)
    {
        List<PassiveSkillData> result = new();

        // 패시브 전체 가져와서 순회
        foreach (var pair in SkillDataLoader.PassiveSkillMap)
        {
            // 패시브 데이터
            PassiveSkillData passive = pair.Value;

            // 이미 보유 중이면 제외
            if (skillManager.MyPassiveSkills.Exists(p => p.PassiveId == passive.PassiveId)) continue;

            // 서브태그 플래그
            int subTagFlag = SubTagRegistry.GetFlag(passive.SubTag);

            // 매칭되는 액티브 스킬 수 카운트
            int matchCount = 0;
            foreach (var skill in skillManager.MyActiveSkills)
            {
                if (subTagFlag != 0 && (skill.CurrentSubTag & subTagFlag) != 0)
                    matchCount++;
            }

            // 2개 이상이면 후보에 추가
            if (matchCount >= 2)
                result.Add(passive);
        }

        return result;
    }


    // 선택지 선택
    public void SelectOption(int index)
    {
        // 비정상 범위 체크
        if (index < 0 || index >= _currentOptions.Count) return;

        // 선택지 정보
        var option = _currentOptions[index];

        // 선택지 타입에 맞게 적용
        switch (option.Type)
        {
            case OPTION_TYPE.Active:    // 액티브
                SkillManager.Instance.ApplyActiveOption(option.TargetMainTag, option.ActiveData);
                break;

            case OPTION_TYPE.Passive:   // 패시브
                SkillManager.Instance.ApplyPassiveOption(option.PassiveData);
                break;

            case OPTION_TYPE.Heal:      // 회복
                option.OnHealSelect.OnStartEvent(HEAL_OPTION_RATIO);
                break;
            }

        // 선택지 정리
        _currentOptions.Clear();
    }
}

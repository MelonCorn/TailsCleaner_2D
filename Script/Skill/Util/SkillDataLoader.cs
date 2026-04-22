using System.Collections.Generic;
using UnityEngine;
using static ActiveSkillData;


public static class SkillDataLoader     // 사실상 스킬 데이터 매니저 (지금 스태틱인데 싱글톤으로 변경해도 됨)
{
    // SO 경로
    private const string ACTIVE_PATH = "Active";
    private const string PASSIVE_PATH = "Passive";
    private const string UPGRADE_PATH = "UpgradeStat";  // 하위 Common 포함
    // 공유 업그레이드 메인 태그
    public const int COMMON_MAIN_TAG = 41000;           

    // SO
    private static Dictionary<int, ActiveSkillData> _activeSkillMap = new();
    private static Dictionary<int, PassiveSkillData> _passiveSkillMap = new();
    private static Dictionary<int, ActiveUpgradeData> _upgradeDataMap = new();

    // 액티브 SO에서 뽑은 id 별 모디파이어 설정
    private static Dictionary<int, ActiveModifier> _upgradeModifierMap = new();

    // 메인태그의 업그레이드 묶음
    private static Dictionary<int, List<ActiveUpgradeData>> _upgradeMap = new();


    // 선택지 후보용 읽기 전용
    public static IReadOnlyDictionary<int, PassiveSkillData> PassiveSkillMap => _passiveSkillMap;
    public static IReadOnlyDictionary<int, List<ActiveUpgradeData>> UpgradeMap => _upgradeMap;




    // 스킬 전체 불러오기
    public static void Init()
    {
        LoadActiveSkills();     // 액티브
        LoadPassiveSkills();    // 패시브
        LoadActiveUpgrades();   // 액티브 업그레이드
        //Debug.Log($"[SkillDataLoader] 액티브 {_activeSkillMap.Count}개, 패시브 {_passiveSkillMap.Count}개 로드 완료.");
    }
    
    // 액티브 스킬 불러오기
    private static void LoadActiveSkills()
    {
        _activeSkillMap.Clear();

        // 액티브 스킬 경로에 모든 SO 불러오기
        foreach (var so in Resources.LoadAll<ActiveSkillData>(ACTIVE_PATH))
        {
            // 메인태그에 SO 등록
            if (_activeSkillMap.TryAdd(so.MainTag, so) == false)
            {
                // 혹시나 중복이면 스킵
                Debug.LogWarning($"[SkillDataLoader] 중복 MainTag: {so.MainTag} ({so.name})");
                continue;
            }

            // 스킬의 업그레이드 모디파이어 데이터 순회
            foreach (var upgradeConfig in so.UpgradeModifierDatas)
            {
                // 업그레이드 ID 에 모디파이어 추가
                if (upgradeConfig.Modifier != null)
                    _upgradeModifierMap.TryAdd(upgradeConfig.UpgradeId, upgradeConfig.Modifier);
            }
        }
    }

    // 패시브 스킬 불러오기
    private static void LoadPassiveSkills()
    {
        _passiveSkillMap.Clear();

        // 패시브 스킬 경로에 모든 SO 불러오기
        foreach (var so in Resources.LoadAll<PassiveSkillData>(PASSIVE_PATH))
        {
            // 패시브 ID 에 SO 등록
            if (_passiveSkillMap.TryAdd(so.PassiveId, so) == false)
            {
                // 혹시나 중복이면 스킵
                Debug.LogWarning($"[SkillDataLoader] 중복 PassiveId: {so.PassiveId} ({so.name})");
                continue;
            }

            // 서브태그 레지스트리에 등록
            if (so.SubTag != 0) SubTagRegistry.Register(so.SubTag);
        }
    }


    // 액티브 업그레이드 스탯 불러오기
    private static void LoadActiveUpgrades()
    {
        _upgradeDataMap.Clear();
        _upgradeMap.Clear();

        // 액티브 업그레이드 스탯 경로에 모든 SO 불러오기
        // 하위 Common 포함
        foreach (var so in Resources.LoadAll<ActiveUpgradeData>(UPGRADE_PATH))
        {
            if (_upgradeDataMap.TryAdd(so.Id, so) == false)
            {
                Debug.LogWarning($"[SkillDataLoader] 중복 upgradeId: {so.Id} ({so.name})");
                continue;
            }
            // true면 등록
        }

        // upgradeMap
        foreach (var pair in _upgradeDataMap)
        {
            // 업그레이드 SO
            ActiveUpgradeData upgrade = pair.Value;

            // 공용
            if (upgrade.MainTag == COMMON_MAIN_TAG)
            {
                // 거름망 통과 후 등록
                foreach (var skillPair in _activeSkillMap)
                {
                    int mainTag = skillPair.Key;
                    ActiveSkillData skillSo = skillPair.Value;

                    if (CanUpgrade(skillSo.AttackType, upgrade) == true)
                        AddToUpgradeMap(mainTag, upgrade);
                }
            }
            // 전용
            else
            {
                AddToUpgradeMap(upgrade.MainTag, upgrade);
            }
        }

        //Debug.Log($"[SkillDataLoader] ActiveUpgradeData {_upgradeDataMap.Count}개 로드, " +
                //   $"upgradeMap {_upgradeMap.Count}개 스킬 빌드 완료.");
    }

    // 업그레이드 거름망 
    private static bool CanUpgrade(ATTACK_TYPE attackType, ActiveUpgradeData upgrade)
    {
        if (attackType == ATTACK_TYPE.Projectile && upgrade.TickRate != 0) return false;    // 투사체는 틱 X
        if (attackType == ATTACK_TYPE.Area && upgrade.PierceCount != 0) return false;       // 장판은 관통 X
        return true;
    }

    // 메인태그에 업그레이드 추가
    private static void AddToUpgradeMap(int mainTag, ActiveUpgradeData upgrade)
    {
        // 메인 태그 미등록이면 리스트 추가
        if (_upgradeMap.ContainsKey(mainTag) == false)
            _upgradeMap[mainTag] = new List<ActiveUpgradeData>();

        // 메인 태그의 업그레이드 리스트에 없으면 업그레이드 추가
        if (_upgradeMap[mainTag].Contains(upgrade) == false)
            _upgradeMap[mainTag].Add(upgrade);
    }


    #region 데이터

    // 스킬 데이터
    public static ActiveSkillData GetActiveSkillData(int mainTag)
        => _activeSkillMap.TryGetValue(mainTag, out var data) ? data : null;
    public static ActiveUpgradeData GetActiveUpgradeData(int upgradeId)
        => _upgradeDataMap.TryGetValue(upgradeId, out var data) ? data : null;


    // Modifier
    public static ActiveModifier GetActiveModifier(int upgradeId)
        => _upgradeModifierMap.TryGetValue(upgradeId, out var modifier) ? modifier : null;


    // 스킬 업그레이드 리스트
    public static List<ActiveUpgradeData> GetActiveUpgradeDatas(int mainTag)
        => _upgradeMap.TryGetValue(mainTag, out var datas) ? datas : null;


    #endregion

    #region 스트링
    public static string GetString(string stringId)
    {
        // stringSO 가져오기
        var stringSO = DataManager.Instance.GetSOData<StringSO>();

        if (stringSO == null)
        {
            Debug.LogWarning("[SkillDataLoader] DataManager에서 StringSO를 찾을 수 없음");
            return string.Empty;
        }

        // SO에서 Id로 찾아오기
        var entry = stringSO.GetById(stringId);

        if (entry == null)
        {
            Debug.LogWarning($"[SkillDataLoader] StringId {stringId} 를 찾을 수 없음");
            return string.Empty;
        }

        // 언어 설정따라 분기하도록 해야함
        return entry.kr ?? string.Empty;
    }

    // 액티브 스킬 이름
    public static string GetActiveSkillName(int mainTag)
    {
        var data = GetActiveSkillData(mainTag);
        return data != null ? GetString(data.NameStringId) : string.Empty;
    }

    // 업그레이드 설명
    public static string GetUpgradeDesc(int upgradeId)
    {
        var data = GetActiveUpgradeData(upgradeId);
        return data != null ? GetString(data.DescStringId) : string.Empty;
    }

    // 패시브 이름
    public static string GetPassiveSkillName(int passiveId)
    {
        if (_passiveSkillMap.TryGetValue(passiveId, out var data))
            return GetString(data.NameStringId);
        return string.Empty;
    }

    // 패시브 설명
    public static string GetPassiveSkillDesc(int passiveId)
    {
        if (_passiveSkillMap.TryGetValue(passiveId, out var data))
            return GetString(data.DescStringId);
        return string.Empty;
    }
    #endregion

}
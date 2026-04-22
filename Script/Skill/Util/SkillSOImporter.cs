#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ActiveSkillData;


// CSV -> ScriptableObject 
public class SkillSOImporter : EditorWindow
{
    private string _activeSkillCsvPath = "Assets/00.Resources/Resources/Data/CSV/Skill/active_skill.csv";          // 액티브 베이스
    private string _activeUpgradeCsvPath = "Assets/00.Resources/Resources/Data/CSV/Skill/active_upgrade.csv";      // 액티브 업그레이드
    private string _activeStatusCsvPath = "Assets/00.Resources/Resources/Data/CSV/Skill/active_status.csv";        // 업그레이드 스탯
    private string _passiveSkillCsvPath = "Assets/00.Resources/Resources/Data/CSV/Skill/passive_skill.csv";        // 패시브
    private string _stringCsvPath = "Assets/00.Resources/Resources/Data/CSV/String/string.csv";                    // 스트링 테이블

    // 저장 경로
    private string _activeSavePath = "Assets/00.Resources/Resources/Data/ScriptableObjects/Skill/Resources/Active";                     // 액티브
    private string _passiveSavePath = "Assets/00.Resources/Resources/Data/ScriptableObjects/Skill/Resources/Passive";                   // 패시브  
    private string _upgradeSavePath = "Assets/00.Resources/Resources/Data/ScriptableObjects/Skill/Resources/UpgradeStat";               // 업그레이드 
    private string _commonUpgradeSavePath = "Assets/00.Resources/Resources/Data/ScriptableObjects/Skill/Resources/UpgradeStat/Common";  // 공용 업그레이드

    // 스킬 프리팹 경로
    private string _skillPrefabPath = "Assets/04.Prefabs/Skills/Skill";
    private string _skillProjectilePrefabPath = "Assets/04.Prefabs/Skills/Projectile";

    // 스킬 사운드 데이터 경로
    private string _skillSoundDataPath = "Assets/02.Scripts/Skill/SkillSoundData";

    [MenuItem("Tools/Skill SO Importer")]   // 메뉴창
    public static void Open() => GetWindow<SkillSOImporter>("Skill SO Importer");

    private void OnGUI()
    {
        GUILayout.Label("CSV 검색 경로", EditorStyles.boldLabel);
        _activeSkillCsvPath = EditorGUILayout.TextField("active_skill", _activeSkillCsvPath);
        _activeUpgradeCsvPath = EditorGUILayout.TextField("active_upgrade", _activeUpgradeCsvPath);
        _activeStatusCsvPath = EditorGUILayout.TextField("active_status", _activeStatusCsvPath);
        _passiveSkillCsvPath = EditorGUILayout.TextField("passive_skill", _passiveSkillCsvPath);
        _stringCsvPath = EditorGUILayout.TextField("string", _stringCsvPath);

        EditorGUILayout.Space(6);
        GUILayout.Label("SO 저장 경로", EditorStyles.boldLabel);
        _activeSavePath = EditorGUILayout.TextField("Active", _activeSavePath);
        _passiveSavePath = EditorGUILayout.TextField("Passive", _passiveSavePath);
        _upgradeSavePath = EditorGUILayout.TextField("Upgrade", _upgradeSavePath);
        _commonUpgradeSavePath = EditorGUILayout.TextField("CommonUpgrade", _commonUpgradeSavePath);

        EditorGUILayout.Space(6);
        GUILayout.Label("프리팹 검색 경로", EditorStyles.boldLabel);
        _skillPrefabPath = EditorGUILayout.TextField("Skill Prefabs", _skillPrefabPath);
        _skillProjectilePrefabPath = EditorGUILayout.TextField("Skill Projectile Prefabs", _skillProjectilePrefabPath);

        EditorGUILayout.Space(6);
        GUILayout.Label("사운드 데이터 검색 경로", EditorStyles.boldLabel);
        _skillSoundDataPath = EditorGUILayout.TextField("Skill Sound Data", _skillSoundDataPath);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Import Active Skills", GUILayout.Height(20))) ImportActive();
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Import Passive Skills", GUILayout.Height(20))) ImportPassive();
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Import Active Upgrades", GUILayout.Height(20))) ImportActiveUpgrade();
    }


    #region Active

    // 액티브 CSV -> SO
    private void ImportActive()
    {
        // 폴더 보장해주기 (상위 없으면 생성)
        EnsureFolder(_activeSavePath);

        // csv 읽기
        var skills = ReadActiveSkills();
        var upgrades = ReadActiveUpgrades();
        var stringMap = ReadString();

        // mainTag -> 업그레이드 리스트
        var upgradeMap = new Dictionary<int, List<UpgradeRow>>();
        foreach (var upgrade in upgrades)
        {
            if (upgradeMap.ContainsKey(upgrade.MainTag) == false)
                upgradeMap[upgrade.MainTag] = new();

            upgradeMap[upgrade.MainTag].Add(upgrade);
        }

        // 스킬 전부
        foreach (var skill in skills)
        {
            // 저장 경로에 이름
            string path = $"{_activeSavePath}/ActiveData_{skill.MainTag}.asset";

            // 경로에 SO 있으면 가져오고 없으면 새로만들기
            var so = AssetDatabase.LoadAssetAtPath<ActiveSkillData>(path)
                ?? CreateInstance<ActiveSkillData>();

            // 기본 데이터 설정
            so.MainTag = skill.MainTag;
            so.NameStringId = skill.NameStringId;
            so.SkillName = GetKorean(stringMap, skill.NameStringId);
            so.AttackType = skill.AttackType;
            so.TargetingType = skill.TargetingType;

            // 스킬 프리팹 연결
            ActiveSkill skillPrefab = FindSkillPrefabById(skill.MainTag);
            if (skillPrefab != null) so.SkillPrefab = skillPrefab;
            else Debug.LogWarning($"[SkillSOImporter] {skill.MainTag} ID를 포함한 스킬 프리팹 찾지 못함. (경로: {_skillPrefabPath})");

            // 스킬 투사체 프리팹 연결
            GameObject skillProjectilePrefab = FindSkillProjectilePrefabById(skill.MainTag);
            if (skillProjectilePrefab != null) so.SkillProjectilePrefab = skillProjectilePrefab;
            else Debug.LogWarning($"[SkillSOImporter] {skill.MainTag} ID를 포함한 투사체 프리팹 찾지 못함. (경로: {_skillProjectilePrefabPath})");

            // 스킬 사운드 데이터 연결
            SkillSoundData soundData = FindSoundDataById(skill.MainTag);
            if (soundData != null) so.SoundData = soundData;
            else Debug.LogWarning($"[SkillSOImporter] {skill.MainTag} ID를 포함한 사운드 데이터 찾지 못함. (경로: {_skillSoundDataPath})");

            // 기존 Modifier 보존
            var existingModifiers = new Dictionary<int, ActiveModifier>();
            foreach (var upgradeModifierData in so.UpgradeModifierDatas)
            {
                if (upgradeModifierData.Modifier != null)    // Modifier
                    existingModifiers[upgradeModifierData.UpgradeId] = upgradeModifierData.Modifier;
            }

            // 모디파이어 데이터 싹 청소
            so.UpgradeModifierDatas.Clear();

            // 스킬 전용 업그레이드 모디파이어 설정 추가
            if (upgradeMap.TryGetValue(skill.MainTag, out var myUpgrades))
                AddUpgradeModifiers(so, myUpgrades, existingModifiers, stringMap);

            // 경로에 SO 저장, 생성
            SaveOrCreate(so, path);
        }

        // 에셋 저장
        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSOImporter] Active Import 완료.");
    }


    // 스킬 프리팹 찾기
    private ActiveSkill FindSkillPrefabById(int id)
    {
        // 폴더 경로 유효한지 체크
        if (AssetDatabase.IsValidFolder(_skillPrefabPath) == false) return null;

        // 해당 폴더 내의 모든 프리팹 검색 후 GUID 반환
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _skillPrefabPath });

        // 모든 GUID 순회
        foreach (string guid in guids)
        {
            // GUID를 읽을 수 있는 경로로 변경
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 경로에서 확장자 떼고 파일 이름만 추출
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // 파일 이름에 ID가 포함되어 있다면 (예: 40101_SoapBubble)
            if (fileName.Contains(id.ToString()) == true)
            {
                // 경로의 프리팹 오브젝트의 ActiveSkill 반환
                GameObject skillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                return skillPrefab.GetComponent<ActiveSkill>();
            }
        }
        return null; // 못 찾으면 깡통 반환
    }
    
    // 스킬 투사체 프리팹 찾기
    private GameObject FindSkillProjectilePrefabById(int id)
    {
        // 폴더 경로 유효한지 체크
        if (AssetDatabase.IsValidFolder(_skillProjectilePrefabPath) == false) return null;

        // 해당 폴더 내의 모든 프리팹 검색 후 GUID 반환
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _skillProjectilePrefabPath });

        // 모든 GUID 순회
        foreach (string guid in guids)
        {
            // GUID를 읽을 수 있는 경로로 변경
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 경로에서 확장자 떼고 파일 이름만 추출
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // 파일 이름에 ID가 포함되어 있다면 (예: 40102_SoapThrowProjectile)
            if (fileName.Contains(id.ToString()) == true)
            {
                // 경로의 투사체 프리팹반환
                return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
        }
        return null; // 못 찾으면 깡통 반환
    }

    // 스킬 사운드 데이터 찾기
    private SkillSoundData FindSoundDataById(int id)
    {
        // 폴더 경로 유효한지 체크
        if (AssetDatabase.IsValidFolder(_skillSoundDataPath) == false) return null;

        // 해당 폴더 내의 모든 프리팹 검색 후 GUID 반환
        string[] guids = AssetDatabase.FindAssets("t:SkillSoundData", new[] { _skillSoundDataPath });

        // 모든 GUID 순회
        foreach (string guid in guids)
        {
            // GUID를 읽을 수 있는 경로로 변경
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 경로에서 확장자 떼고 파일 이름만 추출
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // 파일 이름에 ID가 포함되어 있으면 (예: 41001_BubbleAreaSoundData)
            if (fileName.Contains(id.ToString()))
            {
                // 경로의 사운드 데이터 반환
                return AssetDatabase.LoadAssetAtPath<SkillSoundData>(assetPath);
            }
        }
        return null; // 못 찾으면 깡통 반환
    }

    // 전용 업그레이드 설정 추가
    private void AddUpgradeModifiers(ActiveSkillData so, List<UpgradeRow> upgrades,
        Dictionary<int, ActiveModifier> existingModifiers, Dictionary<string, string> stringMap)
    {
        // 업그레이드마다
        foreach (var upgrade in upgrades)
        {
            // 설정 추가
            so.UpgradeModifierDatas.Add(new UpgradeModifierData
            {
                UpgradeId = upgrade.Id,                                                  // active_skill_id
                Name = upgrade.Name,                                                     // active_upgrade_name
                DescStringId = upgrade.DescStringId,                                                     // effect
                Desc = GetKorean(stringMap, upgrade.DescStringId),                                                     // effect

                Modifier = existingModifiers.TryGetValue(upgrade.Id, out var modifier)   // 모디파이어
                ? modifier : CreateActiveModifier(upgrade.Id),                           // 기존 설정 있으면 기존 사용, 없으면 생성                
            });
        }
    }

    // 액티브 모디파이어 생성
    private ActiveModifier CreateActiveModifier(int upgradeId) => upgradeId switch
    {
        // 비누 거품
        40002 => new SoapBubbleTrackingModifier(),        // 자동 추척 비누 지우개
        40003 => new SoapBubblePlayerDefenseModifier(),   // 버블버블
        40004 => new SoapBubbleSlowModifier(),            // 빨래당함
        40007 => new SoapBubbleStunModifier(),            // 슬랩스틱
        40008 => new SoapBubbleBurstModifier(),           // 거품 펑!


        // 비누 던지기
        40011 => new SoapRetargetModifier(),              // 감나빗!
        40012 => new SoapPierceDamageModifier(),          // 거품내기
        40014 => new SoapPierceSpeedModifier(),           // 거품 가속
        40016 => new SoapRemovalPierceModifier(),         // 비누 덩어리


        // 물폭탄
        40018 => new WaterBombSplashModifier(),           // 물바다
        40019 => new WaterBombVortexModifier(),           // 소용돌이
        40020 => new WaterBombBulletClearModifier(),      // 폭발은 예술이다!


        // 타올 휘두르기
        40022 => new TowelRecycleModifier(),              // 타올 리사이클
        40024 => new TowelParryModifier(),                // 패링
        40025 => new TowelSyncModifier(),                 // 타올 휘날리며
                 
        
        // 걸레 휘두르기                                   
        40027 => new MopRecycleModifier(),                // 걸레 리사이클
        40028 => new MopSlowModifier(),                   // 젖은 걸레
        40029 => new MopStunModifier(),                   // 어질어질
        40030 => new MopSyncModifier(),                   // 걸레 휘날리며


        // 세탁 파도
        40032 => new WashWaveCurseModifier(),             // 끌고오는 파도
        40033 => new WashWaveReturnModifier(),            // 밀물 썰물
        40034 => new WashWaveDrainModifier(),             // 탈수
        40035 => new WashWaveHealModifier(),              // 차오르는 체력


        // 세제 캡슐
        40040 => new DetergentRapidFireModifier(),        // 1만 시간의 법칙


        // 회전 장난감
        40042 => new SpinningToyTrainModifier(),          // 기차 장난감
        40043 => new SpinningToyTopModifier(),            // 팽이 장난감
        40044 => new SpinningToyMoonModifier(),           // 달 장난감
        40045 => new SpinningToyDuckModifier(),           // 오리 장난감
        40046 => new SpinningToyPirateModifier(),         // 해적선 장난감
        40047 => new SpinningToySharkModifier(),          // 상어 장난감
        40048 => new SpinningToyBurstModifier(),          // 물놀이 끝!


        // 일광건조
        40050 => new SunDryingDamagePerTickModifier(),    // 따스한 태양
        40051 => new SunDryingKnockbackModifier(),        // 이불 털기
        40052 => new SunDryingSlowModifier(),             // 으슬으슬
        40053 => new SunDryingDefenseModifier(),          // 이불 두르기
        40054 => new SunDryingShieldModifier(),           // 두꺼운 이불
        40055 => new SunDryingHealModifier(),             // 기상!


        // 블루투스 샤워기
        40057 => new BluetoothRapidFireModifier(),        // 수압 최대로!
        40058 => new BluetoothHealModifier(),             // 온수샤워
        40059 => new BluetoothSpeedBoostModifier(),       // 냉수마찰
        40060 => new BluetoothKnockbackModifier(),        // 예열완료
        40061 => new BluetoothDefenseModifier(),          // 방수코팅
        40062 => new BluetoothAlwaysFireModifier(),       // 키친건!


        _ => null  // 매핑된거 없으면 깡통
    };
    #endregion

    #region Passive

    // 패시브 CSV -> SO
    private void ImportPassive()
    {
        // 폴더 보장 (상위 없으면 생성)
        EnsureFolder(_passiveSavePath);

        // 스트링 CSV
        var stringMap = ReadString();

        // 패시브 CSV 읽고 순회
        foreach (var passive in ReadPassiveSkills())
        {
            // 저장 경로에 이름
            string path = $"{_passiveSavePath}/PassiveData_{passive.Id}.asset";

            // 경로에 SO 있으면 가져오고 없으면 새로만들기
            var so = AssetDatabase.LoadAssetAtPath<PassiveSkillData>(path)
                     ?? CreateInstance<PassiveSkillData>();

            // 데이터
            so.PassiveId = passive.Id;
            so.NameStringId = passive.NameStringId;
            so.PassiveName = GetKorean(stringMap, so.NameStringId);
            so.SubTag = passive.SubTag;
            so.DescStringId = passive.DescStringId;
            so.Desc = GetKorean(stringMap, so.DescStringId);


            // 처음 생성 때만 기본
            // 업데이트시 수동 수치 보존
            if (so.Modifier == null)
                so.Modifier = CreatePassiveModifier(passive.Id);

            // 경로에 SO 저장, 생성
            SaveOrCreate(so, path);
        }

        // 에셋 저장
        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSOImporter] Passive Import 완료.");
    }


    // 패시브 모디파이어 생성
    private PassiveModifier CreatePassiveModifier(int passiveId) => passiveId switch
    {
        42001 => new RaccoonCrateModifier(),        // 매이크 라쿤 크레이트 어겐!
        42002 => new CenterSwitchModifier(),        // 목표를 중앙에 두고 스위치
        42003 => new FocusAttackModifier(),         // 집중공략
        42004 => new DoubleExtraDamageModifier(),   // 추가 추가 피해
        42005 => new SuperCleanModifier(),          // SuperClean
        42006 => new BravadoModifier(),             // 객기
        42007 => new CleanerVinylSuitModifier(),    // 청소용 비닐옷
        42008 => new ClassicSecretModifier(),       // 고전비급
        42009 => new BiggerBetterModifier(),        // 더 크게! 더! 더더! 크고 아름답게!
        42010 => new GoldCrownModifier(),           // 황금왕관
        42011 => new ADCarryModifier(),             // 원딜의 정석
        42012 => new SnowballingModifier(),         // 스노우볼링
        42013 => new AmbiModifier(),                // 양손잡이
        42014 => new ImplantModifier(),             // 기초적인 임플란트입니다
        42015 => new SodaWaterModifier(),           // 탄산수
        42016 => new CatLaundryModifier(),          // 냥빨래
        42017 => new NimbleBlockModifier(),         // 하지만 이렇게 간단하게 피했습니다

        _ => null
    };
    #endregion

    #region Upgrade
    private void ImportActiveUpgrade()
    {
        // 폴더 보장 (없으면 생성)
        EnsureFolder(_commonUpgradeSavePath);

        var upgrades = ReadActiveUpgrades();     // 기본 정보
        var statusMap = ReadActiveStatus();      // ID → 스탯 행
        var stringMap = ReadString();            // 스트링

        // 업그레이드 순회
        foreach (var upgrade in upgrades)
        {
            // 업그레이드의 메인 태그가 공용인지 체크 
            bool isCommon = (upgrade.MainTag == SkillDataLoader.COMMON_MAIN_TAG);

            // 전용, 공용 업그레이드 경로
            string path = isCommon
                ? $"{_commonUpgradeSavePath}/ActiveUpgradeStat_{upgrade.Id}.asset"
                : $"{_upgradeSavePath}/ActiveUpgradeStat_{upgrade.Id}.asset";


            // 경로에 SO 있으면 가져오고 없으면 새로만들기
            var so = AssetDatabase.LoadAssetAtPath<ActiveUpgradeData>(path)
                     ?? CreateInstance<ActiveUpgradeData>();

            // 기본 정보 (active_upgrade.csv)
            so.Id = upgrade.Id;
            so.Name = upgrade.Name;
            so.DescStringId = upgrade.DescStringId;
            so.Desc = GetKorean(stringMap, so.DescStringId);
            so.Tier = upgrade.Tier;
            so.MaxLevel = upgrade.MaxLev;
            so.MainTag = upgrade.MainTag;
            so.SubTag1 = upgrade.SubTag1;
            so.SubTag2 = upgrade.SubTag2;

            // 스탯 정보 (active_status.csv)
            if (statusMap.TryGetValue(upgrade.Id, out var upgradeStat))
            {
                so.Size = upgradeStat.Size;
                so.Damage = upgradeStat.Damage;
                so.Cooldown = upgradeStat.Cooldown;
                so.Duration = upgradeStat.Duration;
                so.ProjectileSpeed = upgradeStat.Speed;
                so.ProjectileCount = upgradeStat.Projectiles;
                so.PierceCount = upgradeStat.Piercing;
                so.TickRate = upgradeStat.Tick;
                so.Knockback = upgradeStat.Knockback;
                so.HasBarrier = upgradeStat.Barrier;
            }

            SaveOrCreate(so, path);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SkillSOImporter] Active Upgrade Import 완료.");
    }

    #endregion

    #region CSV 파싱

    // 액티브 스킬 기본 정보 CSV 읽기
    private List<ActiveSkillRow> ReadActiveSkills()
    {
        // 액티브 스킬 기본 정보 목록
        var result = new List<ActiveSkillRow>();

        // 항목들
        var lines = File.ReadAllLines(_activeSkillCsvPath);

        // 항목 순회 (액티브 스킬 순회)
        for (int i = 3; i < lines.Length; i++)   // 0,1 (헤더,타입) 제외
        {
            // 분해
            var items = Split(lines[i]);

            // 목록에 추가
            result.Add(new ActiveSkillRow
            {
                NameStringId = items[0].Trim(),                         // active_name (string id)
                MainTag = int.Parse(items[1]),                          // main_tag
                AttackType = (ATTACK_TYPE)int.Parse(items[2]),          // attack_type
                TargetingType = (TARGETING_TYPE)int.Parse(items[3])     // targeting_type
            });
        }
        return result;
    }


    // 액티브 스킬 업그레이드 정보 CSV 읽기
    private List<UpgradeRow> ReadActiveUpgrades()
    {
        // 스킬 업그레이드 정보 목록
        var result = new List<UpgradeRow>();

        // 항목들 
        var lines = File.ReadAllLines(_activeUpgradeCsvPath);

        // 항목 순회 (업그레이드 순회)
        for (int i = 3; i < lines.Length; i++)  // 0,1 (헤더,타입) 제외
        {
            // 분해
            var items = Split(lines[i]);

            // 목록에 추가
            result.Add(new UpgradeRow
            {
                Id = int.Parse(items[0]),                               // active_skill_id
                Name = items[1].Trim(),                                 // active_upgrade_name
                Tier = int.Parse(items[2]),                             // upgrade_tier
                MaxLev = int.Parse(items[3]),                           // upgrade_maxlev
                MainTag = int.Parse(items[4]),                          // main_tag
                SubTag1 = ParseIntSafe(items, 5),                       // sub_tag_1
                SubTag2 = ParseIntSafe(items, 6),                       // sub_tag _2
                DescStringId = items[7].Trim(),                         // effect (string id)
            });
        }
        return result;
    }

    // 업그레이드 스탯 정보 CSV 읽기
    private Dictionary<int, StatusRow> ReadActiveStatus()
    {
        var result = new Dictionary<int, StatusRow>();
        var lines = File.ReadAllLines(_activeStatusCsvPath);

        for (int i = 3; i < lines.Length; i++)
        {
            var items = Split(lines[i]);

            if (int.TryParse(items[0], out int id) == false) continue;

            result[id] = new StatusRow
            {
                Id = id,                                                 // active_skill_id
                Size = ParseFloatSafe(items, 1),                         // skill_size
                Damage = ParseFloatSafe(items, 2),                       // skill_damage
                Cooldown = ParseFloatSafe(items, 3),                     // skill_cooldown
                Duration = ParseFloatSafe(items, 4),                     // skill_duration
                Speed = ParseFloatSafe(items, 5),                        // skill_speed
                Projectiles = ParseIntSafe(items, 6),                    // skill_projectiles
                Piercing = ParseIntSafe(items, 7),                       // skill_piercing
                Tick = ParseFloatSafe(items, 8),                         // skill_tick
                Knockback = ParseFloatSafe(items, 9),                    // skill_knockback
                Barrier = ParseBoolSafe(items, 10)                       // skill_barrier
            };
        }
        return result;
    }


    // 패시브 스킬 정보 CSV 읽기
    private List<PassiveRow> ReadPassiveSkills()
    {
        // 패시브 스킬 정보 목록
        var result = new List<PassiveRow>();

        // 항목들
        var lines = File.ReadAllLines(_passiveSkillCsvPath);

        // 항목 순회 (패시브 순회)
        for (int i = 3; i < lines.Length; i++)  // 0,1 (헤더,타입) 제외
        {
            // 분해
            var items = Split(lines[i]);

            // 목록에 추가
            result.Add(new PassiveRow
            {
                Id = int.Parse(items[0]),               // passive_skill_id
                NameStringId = items[1].Trim(),         // passive_name      (string id)
                SubTag = int.Parse(items[2]),           // sub_tag
                DescStringId = items[3].Trim(),         // effect            (string id)
            });
        }
        return result;
    }

    // 스트링 CSV 읽기
    private Dictionary<string, string> ReadString()
    {
        // 스트링 정보 목록
        var result = new Dictionary<string, string>();
        // 항목들
        var lines = File.ReadAllLines(_stringCsvPath);

        // 항목 순회
        for (int i = 3; i < lines.Length; i++)
        {
            // 분해
            var items = Split(lines[i]);

            string id = items[0].Trim();  // id
            result[id] = items[3].Trim(); // kr
        }
        return result;
    }

    // 스트링에서 kr 조회 (없으면 id 문자열 반환)
    private string GetKorean(Dictionary<string, string> stringMap, string stringId)
    {
        if (string.IsNullOrEmpty(stringId)) return string.Empty;
        return stringMap.TryGetValue(stringId, out var text) ? text : $"{stringId}";
    }
    #endregion

    #region Util

    // 경로에 SO 저장, 생성
    private void SaveOrCreate(ScriptableObject so, string path)
    {
        // 없으면 생성
        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
            AssetDatabase.CreateAsset(so, path);
        // 있으면 플래그 꽂아두고 업데이트 요청
        else
            EditorUtility.SetDirty(so);
    }

    // 폴더 경로 보장
    private void EnsureFolder(string path)
    {
        // ex) "Assets/A/B/C"

        // 경로 비정상이면
        if (AssetDatabase.IsValidFolder(path) == false)
        {
            // 상위 폴더 경로 역슬래시로 바뀐거 변경        "Assets/A/B"
            string parent = Path.GetDirectoryName(path).Replace(@"\", "/");
            // 현재 폴더 이름                             "C"
            string folder = Path.GetFileName(path);
            // 상위 폴더 경로 체크 (재귀)
            // "Assets/A/B" 확인 후 B 없으면 만들러감 (내부에서 또 A 가 또 없다면 또 재귀)
            EnsureFolder(parent);
            // B 만들고와서 C 만듬
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    /// 따옴표 내부 쉼표 처리, CSV 분리
    private string[] Split(string line)
    {
        var result = new List<string>();                // 분리된 항목 리스트
        var current = new System.Text.StringBuilder();  // 현재 내용
        bool inQuote = false;                           // 따옴표 내부인지 

        // 항목의 문자마다 체크
        foreach (char c in line)
        {
            // 따옴표 만나면 상태 전환
            if (c == '"') inQuote = !inQuote;
            // 쉼표 만나면 따옴표 밖일 때 칸 나누기
            else if (c == ',' && inQuote == false) { result.Add(current.ToString()); current.Clear(); }
            // 따옴표 안에서 만난 문자는 이어 붙이기
            else current.Append(c);
        }

        // 마지막 항목은 쉼표 없으니까 추가
        result.Add(current.ToString());

        // 배열로 반환
        return result.ToArray();
    }

    // 업그레이드 데이터용 값 체크
    private int ParseIntSafe(string[] columns, int index)
    {
        if (index >= columns.Length) return 0;              // 정상 범위인지 체크
        string s = columns[index].Trim();                   // 앞뒤 공백 제거
        return s == "-" || string.IsNullOrEmpty(s) ? 0      // -나 공백,null 은 0 아니면 int Parse 시도 후 성공 하면 value반환
             : int.TryParse(s, out int value) ? value : 0;
    }
    private float ParseFloatSafe(string[] columns, int index)
    {
        if (index >= columns.Length) return 0f;
        string s = columns[index].Trim();
        return s == "-" || string.IsNullOrEmpty(s) ? 0f
             : float.TryParse(s, out float value) ? value : 0f;
    }

    private bool ParseBoolSafe(string[] columns, int index)
    {
        if (index >= columns.Length) return false;
        string s = columns[index].Trim();
        if (s == "-" || string.IsNullOrEmpty(s)) return false;
        return s != "0" && s.ToLower() != "false";
    }
    #endregion


    // 행 정보
    private struct ActiveSkillRow { public string NameStringId; public int MainTag; public ATTACK_TYPE AttackType; public TARGETING_TYPE TargetingType; }
    private struct UpgradeRow { public int Id, Tier, MaxLev, MainTag, SubTag1, SubTag2; public string Name, DescStringId; }
    private struct PassiveRow { public int Id, SubTag; public string NameStringId, DescStringId; }
    private struct StatusRow
    {
        public int Id, Projectiles, Piercing;
        public float Size, Damage, Cooldown, Duration, Speed, Tick, Knockback;
        public bool Barrier;
    }
}

#endif
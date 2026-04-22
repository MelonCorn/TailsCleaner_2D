using System;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SkillManager))]
public class SkillStatHandler : MonoBehaviour
{
    // 플레이어 참조
    private PlayerBase _player;
    private ISkillStat _playerSkillStat;

    // 영구 (패시브, 장난감)
    private PlayerStatFlat _permanentFlat = new();
    private PlayerStatMul _permanentMul = new();

    // 런타임 (장판, 샤워기)
    private Dictionary<string, PlayerStatFlat> _runtimeFlats = new();
    private Dictionary<string, PlayerStatMul> _runtimeMuls = new();

    // 총합
    private PlayerStatFlat _totalFlat = new();
    private PlayerStatMul _totalMulti = new();
    
    // 런타임 버프 중첩 수 (효과 중첩 아님 장판같은거 여러 개 밟았을 때 체크용)

    private Dictionary<string, int> _runtimeCounts = new();

    // 적 강화 수치 
    public float TotalMonsterStrengthBonus { get; private set; }
    public event Action<float> OnMonsterStrengthChanged;            // 수치 변경 이벤트


    private void Awake()
    {
        _player = GetComponent<PlayerBase>();
        _playerSkillStat = _player.GetComponent<ISkillStat>();
    }


    // 영구 스탯 재계산
    // SkillManager.RecheckAllModifiers() 맨 끝에서 호출
    public void RecheckPermanent()
    {
        // 초기화
        _permanentFlat.Reset();
        _permanentMul.Reset();

        var skillManager = SkillManager.Instance;

        // 액티브 스킬 영구 보너스 (기차, 달, 상어, 해적선, 세탁파도 등)
        foreach (var skill in skillManager.MyActiveSkills)
        {
            skill.ModifyPlayerBonus(_permanentFlat, _permanentMul);

            // 더러움 제거 범위
            _permanentFlat.PickupRange += skill.PickupRangeBonus;
        }

        // 패시브 영구 보너스
        foreach (var passive in skillManager.MyPassiveSkills)
            passive.Modifier?.ModifyPlayerPermanent(_permanentFlat, _permanentMul, skillManager, passive.SubTag);

        // 적 강화 수치
        float prevStrength = TotalMonsterStrengthBonus;
        TotalMonsterStrengthBonus = 0f;

        // IEnemyStrengthBonus 스킬의 적 강화 수치 수집
        foreach (var skill in SkillManager.Instance.MyActiveSkills)
        {
            if (skill is IMonsterStrengthBonus bonus)
                TotalMonsterStrengthBonus += bonus.MonsterStrengthBonus;
        }

        // 수치 달라졌으면 소환된 몬스터들 스탯 갱신
        if (MonsterManager.Instance != null && Mathf.Abs(prevStrength - TotalMonsterStrengthBonus) > 0.001f)
        {
            MonsterManager.Instance.ApplyAllEnemyEnhance(TotalMonsterStrengthBonus);
        }

        // 방어막 최대치 갱신 (NimbleBlockModifier)
        int newMax = 1;
        foreach (var passive in skillManager.MyPassiveSkills)
        {
            if (passive.Modifier is NimbleBlockModifier nimble)
            {
                newMax = nimble.MaxShieldStack;
                break;
            }
        }

        // 방어막 최대치 설정
        SetMaxShield(newMax);

        // 설정 완료 후 주입
        Apply();
    }

    // 런타임 고정, 계수 스탯 적용
    public void AddRuntimeStat(string key, PlayerStatFlat flat = null, PlayerStatMul multi = null)
    {
        // 카운트 없으면 생성
        if (_runtimeCounts.ContainsKey(key) == false)
            _runtimeCounts[key] = 0;

        // 카운트 추가
        _runtimeCounts[key]++;

        // 들어온 스탯만 덮어쓰기
        if (flat != null) _runtimeFlats[key] = flat;
        if (multi != null) _runtimeMuls[key] = multi;

        // 스탯 재계산 후 적용
        Apply();
    }

    // 런타임 스탯 제거 (flat랑 multi 같은 key 로 관리)
    public void RemoveRuntime(string key)
    {
        // 카운트 없으면 돌아갓
        if (_runtimeCounts.ContainsKey(key) == false) return;

        // 카운트 감소
        _runtimeCounts[key]--;

        // 카운트 싹 사라지면 스탯 제거
        if (_runtimeCounts[key] <= 0)
        {
            _runtimeCounts.Remove(key);
            _runtimeFlats.Remove(key);
            _runtimeMuls.Remove(key);
        }

        // 플레이어에 적용
        Apply();
    }


    // 회복
    // 최대 체력 비율 회복
    // 초과 회복 시 청소용 비닐옷 방어막 충전 처리
    public void HealByRatio(float ratio)
    {
        float maxHp = _player.MaxHp;                        // 최대 체력
        float currentHp = _player.CurrentHp;                       // 현재 체력
        float amount = maxHp * ratio;                       // 회복량
        float overflow = (currentHp + amount) - maxHp;      // 초과 회복

        Debug.Log($"[SkillStatHandler] 최대 체력 비율 회복 - 최대 체력 : {maxHp} / 현재 체력 : {currentHp} / 회복량 : {amount} / 초과회복 : {overflow > 0f}");

        // 회복
        _player.Heal(amount);

        // 초과 회복에 비닐옷 있으면 방어막
        if (overflow > 0f && SkillManager.Instance.HasPassive<CleanerVinylSuitModifier>(out var modifier))
            TryAddShield(1);
    }

    // 최대 체력 비율 피해 (탈수 등)
    public void TakeDamageByRatio(float ratio)
    {
        _player.TakeDamage(_player.MaxHp * ratio);
    }


    // 최대 방어막 설정
    public void SetMaxShield(int max)
    {
        _playerSkillStat.SetMaxShield(max);
    }

    // 방어막 추가
    public void TryAddShield(int count)
    {
        _playerSkillStat.AddShield(count);
    }

    // 탄환 제거 시 호출
    public void OnBulletCleared()
    {
        // 패시브 있으면 방어막 추가 시도
        if (SkillManager.Instance.HasPassive<NimbleBlockModifier>(out var modifier))
            TryAddShield(1);
    }

    // 지속 + 런타임 합산해서 StatCalculator 주입
    // 버퍼 재사용
    private void Apply()
    {
        // 초기화
        _totalFlat.Reset();
        _totalMulti.Reset();

        // 총합에 영구 스탯 추가
        _totalFlat.Add(_permanentFlat);
        _totalMulti.AddMultiplier(_permanentMul);

        // 총합에 런타임 스탯 추가
        foreach (var flat in _runtimeFlats.Values) _totalFlat.Add(flat);
        foreach (var multi in _runtimeMuls.Values) _totalMulti.AddMultiplier(multi);

        //Debug.Log($"플레이어 스탯 추가 최대체력    : {_totalFlat.MaxHp}");
        //Debug.Log($"플레이어 스탯 추가 공격력      : {_totalFlat.AttackPower}");
        //Debug.Log($"플레이어 스탯 추가 방어력      : {_totalFlat.DefensePower}");
        //Debug.Log($"플레이어 스탯 추가 이동속도    : {_totalFlat.MoveSpeed}");
        //Debug.Log($"플레이어 스탯 추가 경험치 획득 : {_totalFlat.ExpGainRate} %");
        //Debug.Log($"플레이어 스탯 추가 회피율      : {_totalFlat.EvasionChance}");
        //Debug.Log($"플레이어 스탯 추가 치명타율    : {_totalFlat.CriticalChance} %");
        //Debug.Log($"플레이어 스탯 추가 치명타계수  : {_totalFlat.CriticalDamageMultiplier} %");

        //Debug.Log($"플레이어 스탯 이동속도 계수  : {_totalMulti.MoveSpeed * 100f}");

        // 스킬 스탯 주입
        _playerSkillStat.SetSkillStat(_totalFlat, _totalMulti);
    }
}
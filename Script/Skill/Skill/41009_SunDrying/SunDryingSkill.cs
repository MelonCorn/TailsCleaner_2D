using UnityEngine;

public class SunDryingSkill : ActiveSkill<SunDryingArea, SunDryingModifierData>
{
    private const string BUFF_KEY_BLANKEY = "BlanketWrap";      // 이불두르기
    private const string DEBUFF_KEY_SLOW = "SunDryingSlow";     // 으슬으슬


    // 현재 활성화된 일광건조 수 (0 이면 꺼진 상태)
    private int _activeAreaCount = 0;

    public bool IsAreaActive => _activeAreaCount > 0;

    // 이불 두르기 스탯
    private readonly PlayerStatFlat _blanketFlat = new();
    private int _lastDefenseBonus;


    protected override void OnActive(int index, int totalCount)
    {
        // 일광건조 장판 생성
        SunDryingArea area = SpawnFromPool(_skillObjectPrefab, transform.position, Quaternion.identity);

        // 초기화
        if(area != null) area.Init(this, _modifierData);

        // 시전 시 효과
        OnCastEffect();

        // 완전 비활성에서 활성상태로 변경 시
        if (_activeAreaCount == 0)
        {
            OnStateChangedToActive();
        }

        // 활성화 일광건조 수 증가
        _activeAreaCount++;
    }


    // 시전 시 효과
    private void OnCastEffect()
    {
        // 기상!
        // 시전 시 체력 5% 회복
        if (_modifierData.HealOnActivate)
        {
            SkillStatHandler.HealByRatio(_modifierData.HealRatio);
        }
    }

    // 시전 상태로 변환 효과
    private void OnStateChangedToActive()
    {
        // 이불 두르기
        if (_modifierData.DefenseOnInactive)
        {
            SkillStatHandler.RemoveRuntime(BUFF_KEY_BLANKEY);
        }

        // 으슬으슬
        if (_modifierData.SlowOnArea)
        {
            ApplySlowAllMonster();
            MonsterManager.Instance.OnMonsterSpawned += SlowSpawnMonster;
        }
    }


    // SunDryingArea.OnExpire에서 호출
    // 일광건조 만료 시
    public void OnAreaExpired()
    {
        // 만료 사운드 재생
        PlayExpireSFX();

        // 두꺼운 이불
        // 꺼질 때 방어막 생성
        if (_modifierData.ShieldOnDeactivate)
        {
            SkillStatHandler.TryAddShield(1);
        }

        // 활성화 일광건조 감소
        _activeAreaCount--;

        // 마지막 일광건조가 꺼지면
        // 비활성화 모디파이어 실행
        if (_activeAreaCount == 0)
            OnDeactivate();
    }

    // 비활성 상태로 변경 효과
    private void OnDeactivate()
    {
        // 이불 두르기
        // 꺼진 동안 방어력 증가
        if (_modifierData.DefenseOnInactive)
        {
            // DefenseBonus 값 바뀐 경우에만
            if (_modifierData.DefenseBonus != _lastDefenseBonus)
            {
                // 리셋 후 할당, 저장
                _blanketFlat.Reset();
                _blanketFlat.DefensePower = _modifierData.DefenseBonus;
                _lastDefenseBonus = _modifierData.DefenseBonus;
            }
            SkillStatHandler.AddRuntimeStat(BUFF_KEY_BLANKEY, _blanketFlat);
        }

        // 으슬으슬
        // 맵 전체 적 슬로우 해제
        if (_modifierData.SlowOnArea)
        {
            MonsterManager.Instance.OnMonsterSpawned -= SlowSpawnMonster;
            RemoveSlowAllMonster();
        }
    }
    
    // 전체 슬로우 적용
    private void ApplySlowAllMonster()
    {
        foreach (var monster in MonsterManager.Instance.activeMonsters)
        {
            if (monster == null) continue;
            monster.EnterSlowArea(DEBUFF_KEY_SLOW, _modifierData.SlowAmount);
        }
    }

    // 전체 슬로우 해제
    private void RemoveSlowAllMonster()
    {
        foreach (var monster in MonsterManager.Instance.activeMonsters)
        {
            if (monster == null) continue;
            monster.ExitSlowArea(DEBUFF_KEY_SLOW);
        }
    }

    // 새로 스폰된 몬스터에게도 슬로우
    private void SlowSpawnMonster(MonsterBase monster)
    {
        monster.EnterSlowArea(DEBUFF_KEY_SLOW, _modifierData.SlowAmount);
    }
}

using System.Collections;
using UnityEngine;

public class MopSwingSkill : ActiveSkill<MopSwingArea, SwingModifierData>, ISwingSkill
{
    private const int OTHER_MAIN_TAG = 41004;   // 타올 휘두르기
    public SwingModifierData SwingModifier => _modifierData;            // ISwingSkill

    [Header("리사이클 초승달 프리팹")]
    [SerializeField] private CrescentSwingArea _crescentPrefab;

    private bool _fusionApplied = false;                                // 고전비급 융화 상
                                                                        //
                                                                        //
                                                                        // 태

    protected override void OnActive(int index, int totalCount) { }     // 안씀

    protected override IEnumerator ActiveCoroutine()
    {
        if (AttackDir == Vector2.zero) yield break;

        Vector2 mainDir = -AttackDir;     // 뒤
        Vector2 oppDir = -mainDir;        // 앞

        // 리사이클 체크
        bool hasOwn = _modifierData.HasOwnRecycle;
        bool hasOther = _modifierData.HasOtherRecycle;

        // 메인 기본 장판 항상 1번
        SpawnMainArea(mainDir);

        // 리사이클 없으면 종료
        if (hasOwn == false && hasOther == false) yield break;

        // 추가추가피해 횟수
        int crescentCount = Mathf.Max(1, _finalStat.ExtraMultiplier);

        for (int i = 0; i < crescentCount; i++)
        {
            // 본인 리사이클 (앞)
            // 상대 리사이클 (뒤)
            if (hasOwn) SpawnCrescent(oppDir);
            if (hasOther) SpawnCrescent(mainDir);

            // 생성 텀
            yield return _fireDelay;
        }
    }

    // 메인 장판 생성
    private void SpawnMainArea(Vector2 dir)
    {
        // 걸레 장판 생성
        MopSwingArea area = SpawnFromPool<MopSwingArea>(_skillObjectPrefab, transform.position, Quaternion.identity);

        // 초기화
        if(area != null) area.Init(this, _modifierData, dir);
    }


    // 초승달 장판 생성 (앞, 뒤)
    private void SpawnCrescent(Vector2 dir)
    {
        if (_crescentPrefab == null)
        {
            Debug.LogWarning("[TowelSwingSkill] CrescentPrefab 미설정");
            return;
        }

        // 초승달 장판 생성
        CrescentSwingArea crescent = SpawnFromPool<CrescentSwingArea>(_crescentPrefab, transform.position, Quaternion.identity);

        // 초기화
        if(crescent != null) crescent.Init(this, _modifierData, dir);
    }

    // 패시브 갱신
    // 휘두르며, 고전비급 동기화
    public override void RecheckModifiers()
    {
        base.RecheckModifiers();

        // 타올 휘두르며
        // 걸레 업그레이드 효과 동기화
        if (_modifierData.HasSyncUpgrade)
            SyncOtherSkill();

        // 고전비급
        // 걸레 0티어 baseStat 합산 (1회)
        ApplyFusionStat();
    }

    // 상대 효과 복사
    private void SyncOtherSkill()
    {
        // 활성화된 액티브중 상대 메인태그 찾아와서 ISwingSkill 상속받았는지 체크
        // 참이면 모디파이어 동기화
        if (SkillManager.Instance.GetActiveSkill(OTHER_MAIN_TAG) is ISwingSkill other)
            _modifierData.SyncEffect(other.SwingModifier);
    }

    private void ApplyFusionStat()
    {
        // 합산된 상태면 스킵
        if (_fusionApplied == true) return;

        // 보유 패시브 중 고전비급 포함여부
        bool hasFusion = PassiveModifiers.Exists(modifier => modifier is ClassicSecretModifier);

        // 고전비급 없으면 패스
        if (hasFusion == false) return;

        // 상대 메인태그의 업그레이드 가져오기
        var upgrades = SkillDataLoader.GetActiveUpgradeDatas(OTHER_MAIN_TAG);

        // 0티어 등록 안되어있으면 스킵
        var tierZero = upgrades?.Find(data => data.Tier == 0);
        if (tierZero == null) return;

        // 기본 스탯에 상대 기본 스탯 추가
        _baseStat.Add(tierZero.GetSkillStat());

        // 고전비급 적용
        _fusionApplied = true;

        // 스탯 재계산
        CalculateStats();

        Debug.Log("[TowelSwingSkill] 고전비급: 걸레 0티어 스탯 합산 완료");
    }
}

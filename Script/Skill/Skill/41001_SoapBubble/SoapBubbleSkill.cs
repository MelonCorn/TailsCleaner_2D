using UnityEngine;

public class SoapBubbleSkill : ActiveSkill<SoapBubbleArea, SoapBubbleModifierData>
{
    public PlayerStatFlat BubbleBonus { get; } = new();




    // 스킬 발동

    protected override void OnActive(int index, int totalCount)
    {
        // 장판 플레이어 위치에 생성
        //SoapBubbleArea area = Instantiate(_skillObjectPrefab, transform.position, Quaternion.identity);
        SoapBubbleArea area = SpawnFromPool<SoapBubbleArea>(_skillObjectPrefab, transform.position, Quaternion.identity);

        // 초기화
        if(area != null) area.Init(this, _modifierData);

        // 루프 사운드 재생
        StartLoopSFX();
    }

    public override void RecheckModifiers()
    {
        base.RecheckModifiers();

        // 버블버블 수치 갱신
        if (_modifierData.PlayerDefenseBoost)
        {
            BubbleBonus.Reset();
            BubbleBonus.DefensePower = _modifierData.PlayerDefenseBonus;
        }
    }

    // 장판 만료 시
    public void OnAreaExpired()
    {
        // 루프 사운드 중지
        StopLoopSFX();
    }
}

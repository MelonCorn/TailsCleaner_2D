using UnityEngine;

public class SoapThrowSkill : ActiveSkill<SoapThrowProjectile, SoapThrowModifierData>
{
    [Header("강철 스프라이트")]
    [SerializeField] Sprite _metalSprite;
    public Sprite MetalSprite => _metalSprite;

    // 스킬 발동
    protected override void OnActive(int index, int totalCount)
    {
        // 방지
        if (AttackDir == Vector2.zero) return;

        // 발사
        SpawnSoap(AttackDir);
    }

    // 비누 투사체 생성
    private void SpawnSoap(Vector2 dir)
    {
        // 바라볼 각도
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 비누 생성
        //SoapThrowProjectile soap = Instantiate(_skillObjectPrefab, transform.position, Quaternion.Euler(0, 0, angle));
        SoapThrowProjectile soap = SpawnFromPool<SoapThrowProjectile>(_skillObjectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));

        // 초기화
        if(soap != null) soap.Init(this, _modifierData, dir);
    }
}

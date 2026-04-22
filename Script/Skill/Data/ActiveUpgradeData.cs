

using UnityEngine;

[CreateAssetMenu(fileName = "ActiveUpgradeData", menuName = "Skill/ActiveUpgradeData")]
public class ActiveUpgradeData : ScriptableObject // 액티브 스킬 업그레이드 데이터
{
    // 기본 정보
    public int Id;                  // active_skill_id     액티브 스킬 ID 40001
    public string Name;             // active_upgrade_name 액티브 스킬 이름 비누 거품
    public string Desc;             // effect              설명
    public string DescStringId;     // effect              설명 string id
    public int Tier;                // upgrade_tier        0 기본, 1 ~ 3: 강화
    public int MaxLevel;            // upgrade_maxlev      최대 레벨

    // 태그 정보
    public int MainTag;             // main_tag            메인 태그 41001
    public int SubTag1;             // sub_tag_1           서브 태그 40102
    public int SubTag2;             // sub_tag_2           없으면 0

    // 스탯 정보
    public float Size;              // skill_size          크기 비율
    public float Damage;            // skill_damage        공격력 배율
    public float Cooldown;          // skill_cooldown      쿨타임 감소
    public float Duration;          // skill_duration      지속 시간
    public float ProjectileSpeed;   // skill_speed         투사체 속도
    public int ProjectileCount;     // skill_projectiles   투사체 수 추가
    public int PierceCount;         // skill_piercing      관통 횟수
    public float TickRate;          // skill_tick          장판 피해 주기
    public float Knockback;         // skill_knockback     넉백 수치
    public bool HasBarrier;         // skill_barrier       0,1 bool 변환
    public float PickupRange;       //                     더러움 제거 범위


    // 업그레이드의 서브 태그 id를 플래그로 변환 후 반환
    public int GetSubTag()
    {
        int mask = 0;

        if (SubTag1 != 0) mask |= SubTagRegistry.GetFlag(SubTag1);
        if (SubTag2 != 0) mask |= SubTagRegistry.GetFlag(SubTag2);

        return mask;
    }

    // 스킬 스탯 반환
    public SkillStat GetSkillStat()
    {
        SkillStat bonus = new SkillStat();
        bonus.Size = Size;
        bonus.Damage = Damage;
        bonus.Cooldown = Cooldown;
        bonus.Duration = Duration;
        bonus.ProjectileSpeed = ProjectileSpeed;
        bonus.ProjectileCount = ProjectileCount;
        bonus.PierceCount = PierceCount;
        bonus.TickRate = TickRate;
        bonus.Knockback = Knockback;
        return bonus;
    }
}

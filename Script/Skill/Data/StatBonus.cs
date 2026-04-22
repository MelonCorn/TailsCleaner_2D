using System.Drawing;

public class SkillStat         // 추가 스킬 스탯
{
    public float Damage;            // 공격력 배율
    public float Cooldown;          // 쿨타임 감소
    public float Duration;          // 지속 시간
    public float ProjectileSpeed;   // 투사체 속도
    public int ProjectileCount;     // 투사체 수 추가
    public int PierceCount;         // 관통 횟수
    public float TickRate;          // 장판 피해 주기
    public float Knockback;         // 넉백 수치
    public float Size;              // 크기 비율

    // 패시브용
    public int ExtraMultiplier = 1;          // 추가추가피해 추가 횟수
    public float DurationTickInterval = 0f;  // 스노우볼링 지속시간 체크 간격

    // 합
    public void Add(SkillStat other)
    {
        Damage          += other.Damage;
        Cooldown        += other.Cooldown;
        Duration        += other.Duration;
        ProjectileSpeed += other.ProjectileSpeed;
        ProjectileCount += other.ProjectileCount;
        PierceCount     += other.PierceCount;
        TickRate        += other.TickRate;
        Knockback       += other.Knockback;
        Size            += other.Size;
    }

    // 곱 (공용)
    // 현재 공용 업그레이드는 특정 수치를 제외하고 전부 0 이기 때문에 0 을 체크하고 있는데
    // 만약 의도적으로 0 으로 만드는 공용 업그레이드가 있다면 수정 필요함...
    public void Multiply(SkillStat multiplier)
    {
        if (multiplier.Damage != 0)          Damage *= multiplier.Damage;
        if (multiplier.Cooldown != 0)        Cooldown *= multiplier.Cooldown;
        if (multiplier.Duration != 0)        Duration *= multiplier.Duration;
        if (multiplier.ProjectileSpeed != 0) ProjectileSpeed *= multiplier.ProjectileSpeed;
        if (multiplier.ProjectileCount != 0) ProjectileCount *= multiplier.ProjectileCount;
        if (multiplier.PierceCount != 0)     PierceCount *= multiplier.PierceCount;
        if (multiplier.TickRate != 0)        TickRate *= multiplier.TickRate;
        if (multiplier.Knockback != 0)       Knockback *= multiplier.Knockback;
        if (multiplier.Size != 0)            Size *= multiplier.Size;
    }

    // 공용 스탯용 생성
    public static SkillStat CreateMultiplier()
    {
        return new SkillStat
        {
            Damage          = 1f,
            Cooldown        = 1f,
            Duration        = 1f,
            ProjectileSpeed = 1f,
            ProjectileCount = 1,
            PierceCount     = 1,
            TickRate        = 1f,
            Knockback       = 1f,
            Size            = 1f,
        };
    }

    // new 없이 기존 객체에 덮어쓰기
    // CalculateStat GC 방지용
    public void CopyFrom(SkillStat sc)
    {
        Damage                = sc.Damage;
        Cooldown              = sc.Cooldown;
        Duration              = sc.Duration;
        ProjectileSpeed       = sc.ProjectileSpeed;
        ProjectileCount       = sc.ProjectileCount;
        PierceCount           = sc.PierceCount;
        TickRate              = sc.TickRate;
        Knockback             = sc.Knockback;
        Size                  = sc.Size;
        ExtraMultiplier       = sc.ExtraMultiplier;
        DurationTickInterval  = sc.DurationTickInterval;
    }

    // 복제
    public SkillStat Clone()
    {
        SkillStat clone = new SkillStat();

        clone.CopyFrom(this);

        return clone;
    }
}



public class PlayerStatFlat        // 추가 플레이어 스탯
{
    public float MaxHp;                     // 최대 체력
    public float AttackPower;               // 공격력
    public float DefensePower;              // 방어력
    public float MoveSpeed;                 // 이동 속도 
    public float CriticalChance;            // 치명타 확률
    public float CriticalDamageMultiplier;  // 치명타 피해 계수
    public float EvasionChance;             // 회피율
    public float PickupRange;               // 경험치 획득 범위
    public float ExpGainRate;               // 획득 경험치 증가량
    public float MoveSpeedMultiplier;       // 이동 속도 배율

    // 스탯 더하기
    public void Add(PlayerStatFlat other)
    {
        MaxHp                    += other.MaxHp;
        AttackPower              += other.AttackPower;
        DefensePower             += other.DefensePower;
        MoveSpeed                += other.MoveSpeed;
        CriticalChance           += other.CriticalChance;
        CriticalDamageMultiplier += other.CriticalDamageMultiplier;
        EvasionChance            += other.EvasionChance;
        PickupRange              += other.PickupRange;
        ExpGainRate              += other.ExpGainRate;
        MoveSpeedMultiplier      += other.MoveSpeedMultiplier;

    }

    // 초기화
    public void Reset()
    {
        MaxHp                    = 0f;
        AttackPower              = 0f;
        DefensePower             = 0f;
        MoveSpeed                = 0f;
        CriticalChance           = 0f;
        CriticalDamageMultiplier = 0f;
        EvasionChance            = 0f;
        PickupRange              = 0f;
        ExpGainRate              = 0f;
        MoveSpeedMultiplier      = 0f;
    }

    public void CopyFrom(PlayerStatFlat other)
    {
        MaxHp                    = other.MaxHp;
        AttackPower              = other.AttackPower;
        DefensePower             = other.DefensePower;
        MoveSpeed                = other.MoveSpeed;
        CriticalChance           = other.CriticalChance;
        CriticalDamageMultiplier = other.CriticalDamageMultiplier;
        EvasionChance            = other.EvasionChance;
        PickupRange              = other.PickupRange;
        ExpGainRate              = other.ExpGainRate;
        MoveSpeedMultiplier      = other.MoveSpeedMultiplier;
    }
}



public class PlayerStatMul      // 플레이어 스탯 곱산용
{
    public float MaxHp                      = 0f;
    public float AttackPower                = 0f;
    public float DefensePower               = 0f;
    public float MoveSpeed                  = 0f;
    public float CriticalChance             = 0f;
    public float CriticalDamageMultiplier   = 0f;
    public float EvasionChance              = 0f;
    public float PickupRange                = 0f;
    public float ExpGainRate                = 0f;
    public float MoveSpeedMultiplier        = 0f;  

    // 단리 
    public void AddMultiplier(PlayerStatMul other)
    {
        MaxHp                    += other.MaxHp;
        AttackPower              += other.AttackPower;
        DefensePower             += other.DefensePower;
        MoveSpeed                += other.MoveSpeed;
        CriticalChance           += other.CriticalChance;
        CriticalDamageMultiplier += other.CriticalDamageMultiplier;
        EvasionChance            += other.EvasionChance;
        PickupRange              += other.PickupRange;
        ExpGainRate              += other.ExpGainRate;
        MoveSpeedMultiplier      += other.MoveSpeedMultiplier;
    }

    public void Reset()
    {
        MaxHp                    = 0f;
        AttackPower              = 0f;
        DefensePower             = 0f;
        MoveSpeed                = 0f;
        CriticalChance           = 0f;
        CriticalDamageMultiplier = 0f;
        EvasionChance            = 0f;
        PickupRange              = 0f;
        ExpGainRate              = 0f;
        MoveSpeedMultiplier      = 0f;
    }

    public void CopyFrom(PlayerStatMul other)
    {
        MaxHp                    = other.MaxHp;
        AttackPower              = other.AttackPower;
        DefensePower             = other.DefensePower;
        MoveSpeed                = other.MoveSpeed;
        CriticalChance           = other.CriticalChance;
        CriticalDamageMultiplier = other.CriticalDamageMultiplier;
        EvasionChance            = other.EvasionChance;
        PickupRange              = other.PickupRange;
        ExpGainRate              = other.ExpGainRate;
        MoveSpeedMultiplier      = other.MoveSpeedMultiplier;
    }
}

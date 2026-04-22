﻿using UnityEngine;

public interface ISkillable
{
    public float AttackPower { get; }
    public float DefensePower { get; }
    public float MoveSpeed { get; }
    public float CriticalChance { get; }
    public float CriticalDamageMultiplier { get; }    // 치명타 피해 계수
    public float EvasionChance { get; }               // 회피율
    public float PickupRange { get; }               // 경험치 획득 범위
    public float ExpGainRate { get; }        // 경헝치 획득량
    public float AttackSpeed { get; }        // 공격 속도

    public Vector2 MoveDir { get; }
    public Vector2 AttackDir { get; }
}


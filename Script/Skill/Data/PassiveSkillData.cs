using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PassiveData", menuName = "Skill/PassiveData")]
public class PassiveSkillData : ScriptableObject
{
    [Header("기본 정보")]
    public int PassiveId;
    public string PassiveName;
    public string NameStringId;        // 스트링 테이블 ID
    public int SubTag;

    [TextArea(2, 4)]
    public string Desc;
    public string DescStringId;        // 스트링 테이블 ID

    [Header("아이콘")]
    public Sprite Icon;

    [Header("패시브 모디파이어")]
    [SerializeReference] public PassiveModifier Modifier;
}

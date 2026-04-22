using UnityEngine;

[CreateAssetMenu(menuName = "Skill/SkillSoundData")]
public class SkillSoundData : ScriptableObject
{
    [Header("시전 클립")]
    public AudioClip[] onCast;

    [Header("만료 클립")]
    public AudioClip[] onExpire;

    [Header("특수 클립")]
    public AudioClip[] onSpecial;

    [Header("루프 클립")]
    [Tooltip("없으면 비워두면 됨")]
    public AudioClip loopClip;
}

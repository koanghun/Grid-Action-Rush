using UnityEngine;

/// <summary>
/// 全スキルの基底クラス
/// 各スキルタイプ（移動、攻撃、バフなど）が継承
/// </summary>
public abstract class SkillData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("スキル名（UI表示用）")]
    public string skillName;

    [Tooltip("スキルアイコン（UI表示用）")]
    public Sprite skillIcon;

    [Header("クールタイム")]
    [Tooltip("クールタイム（秒）")]
    [Min(0f)]
    public float cooldownDuration = 1f;
}
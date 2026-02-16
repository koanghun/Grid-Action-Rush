using UnityEngine;

/// <summary>
/// スキルデータ
/// </summary>
public abstract class SkillData : ScriptableObject
{
    [Header("スキル設定")]
    [Tooltip("スキル名")]
    public string skillName;

    [Tooltip("スキルアイコン")]
    public Sprite skillIcon;

    [Header("冷却設定")]
    [Tooltip("冷却時間")]
    [Min(0f)]
    public float cooldownDuration = 1f;
}
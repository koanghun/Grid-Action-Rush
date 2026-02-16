using UnityEngine;

/// <summary>
/// 移動スキルデータ
/// </summary>
[CreateAssetMenu(fileName = "MovementSkillData", menuName = "Skills/Movement Skill Data")]
public class MovementSkillData : SkillData
{
    [Header("移動設定")]
    [Tooltip("ダッシュ距離")]
    [Min(1)]
    public int dashDistance = 2;

    [Tooltip("移動速度倍率")]
    [Min(1f)]
    public float speedMultiplier = 2.0f;

    [Header("移動範囲設定")]
    [Tooltip("壁を通過するか")]
    public bool canPassWall = false;

    [Tooltip("モンスターを通過するか")]
    public bool canPassMonster = false;

    [Tooltip("崖を通過するか")]
    public bool canPassCliff = false;
}
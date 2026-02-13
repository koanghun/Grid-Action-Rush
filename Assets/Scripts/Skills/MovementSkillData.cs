using UnityEngine;

/// <summary>
/// 移動系スキルデータ（回避、ダッシュなど）
/// </summary>
[CreateAssetMenu(fileName = "MovementSkillData", menuName = "Skills/Movement Skill Data")]
public class MovementSkillData : SkillData
{
    [Header("移動設定")]
    [Tooltip("ダッシュ距離（マス数）")]
    [Min(1)]
    public int dashDistance = 2;

    [Tooltip("通常移動に対する速度倍率")]
    [Min(1f)]
    public float speedMultiplier = 2.0f;

    [Header("障害物処理")]
    [Tooltip("壁を通過可能か（例: 幽霊化スキル）")]
    public bool canPassWall = false;

    [Tooltip("モンスターを通過可能か（例: すり抜けスキル）")]
    public bool canPassMonster = false;

    [Tooltip("崖を通過可能か（将来実装）")]
    public bool canPassCliff = false;
}
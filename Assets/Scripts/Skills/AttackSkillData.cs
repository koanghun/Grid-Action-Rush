using UnityEngine;

/// <summary>
/// 攻撃範囲パターンの列挙型
/// </summary>
public enum AttackRangePattern
{
    Single,      // 単一グリッド（1マス）
    Cross,       // 十字（上下左右）
    Square3x3,   // 3x3範囲
    Line         // 直線（向いている方向）
}

/// <summary>
/// 攻撃系スキルデータ
/// </summary>
[CreateAssetMenu(fileName = "AttackSkillData", menuName = "Skills/Attack Skill Data")]
public class AttackSkillData : SkillData
{
    [Header("攻撃設定")]
    [Tooltip("ダメージ量")]
    [Min(1)]
    public int damage = 10;

    [Tooltip("攻撃範囲パターン")]
    public AttackRangePattern rangePattern = AttackRangePattern.Single;

    // TODO (将来実装): グリッド形状を視覚的にデザイン
    // public List<Vector2Int> attackGridPattern;
}
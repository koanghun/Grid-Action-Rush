using UnityEngine;

/// <summary>
/// 攻撃範囲パターン
/// </summary>
public enum AttackRangePattern
{
    Single,      // 1マス
    Cross,       // 十字
    Square3x3,   // 3x3範囲
    Line         // 線
}

/// <summary>
/// 攻撃スキルデータ
/// </summary>
[CreateAssetMenu(fileName = "AttackSkillData", menuName = "Skills/Attack Skill Data")]
public class AttackSkillData : SkillData
{
    [Header("攻撃設定")]
    [Tooltip("ダメージ")]
    [Min(1)]
    public int damage = 10;

    [Tooltip("攻撃範囲パターン")]
    public AttackRangePattern rangePattern = AttackRangePattern.Single;

    // TODO (未実装): 攻撃範囲パターン
    // public List<Vector2Int> attackGridPattern;
}
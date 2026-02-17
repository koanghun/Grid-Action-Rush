using UnityEngine;
using System.Collections.Generic;

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

    [Tooltip("攻撃範囲パターン（後方互換用、実際はattackGridPatternを使用）")]
    public AttackRangePattern rangePattern = AttackRangePattern.Single;

    [Header("攻撃範囲設定")]
    [Tooltip("攻撃範囲グリッド座標リスト（プレイヤー基準の相対座標）")]
    public List<Vector2Int> attackGridPattern = new List<Vector2Int>();

    [Header("視覚エフェクト設定")]
    [Tooltip("攻撃範囲表示用エフェクトプレハブ")]
    public GameObject rangeEffectPrefab;

    [Tooltip("エフェクト表示時間")]
    [Min(0.1f)]
    public float effectDuration = 0.5f;
}
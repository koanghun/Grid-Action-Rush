using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッド上に配置可能なエンティティの共通インターフェース
/// プレイヤー、モンスター、ボス等がこれを実装
/// </summary>
public interface IGridEntity
{
    /// <summary>
    /// エンティティの種類
    /// </summary>
    GridEntityType EntityType { get; }

    /// <summary>
    /// 現在占有しているグリッド座標リスト
    /// 通常のエンティティは1つ、大型ボスは複数（2x2なら4つ）
    /// </summary>
    List<Vector2Int> OccupiedGrids { get; }

    /// <summary>
    /// エンティティのルート座標（基準点、通常は左下）
    /// </summary>
    Vector2Int RootGridPosition { get; }

    /// <summary>
    /// GameObject参照（ダメージ適用やコンポーネント取得用）
    /// </summary>
    GameObject GameObjectReference { get; }
}

/// <summary>
/// エンティティの種類を表す列挙型
/// </summary>
public enum GridEntityType
{
    Player,     // プレイヤー
    Enemy,      // 通常の敵
    Boss,       // ボス（大型モンスター）
    NPC         // NPC（将来実装用）
}

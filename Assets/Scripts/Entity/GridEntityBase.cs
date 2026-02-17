using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IGridEntityの基本実装を提供する抽象クラス
/// 全エンティティ（Player/Enemy/Boss）はこれを継承することを推奨
/// </summary>
public abstract class GridEntityBase : MonoBehaviour, IGridEntity
{
    #region IGridEntity実装

    /// <summary>
    /// エンティティタイプ（派生クラスで設定）
    /// </summary>
    public abstract GridEntityType EntityType { get; }

    /// <summary>
    /// 占有グリッドリスト（内部管理、外部から直接変更禁止）
    /// </summary>
    public List<Vector2Int> OccupiedGrids { get; private set; } = new List<Vector2Int>();

    /// <summary>
    /// ルート座標（通常は左下、単一グリッドなら自身の座標）
    /// </summary>
    public Vector2Int RootGridPosition { get; protected set; }

    /// <summary>
    /// GameObject参照（自分自身）
    /// </summary>
    public GameObject GameObjectReference => gameObject;

    #endregion

    #region Unity Lifecycle

    protected virtual void OnEnable()
    {
        // MapManagerに登録（Lazy Initializationのおかげで安全）
        // オブジェクトプーリング使用時にも対応
        if (MapManager.Instance != null && OccupiedGrids.Count > 0)
        {
            MapManager.Instance.RegisterEntity(this);
        }
    }

    protected virtual void OnDisable()
    {
        // MapManagerから削除
        if (MapManager.Instance != null)
        {
            MapManager.Instance.UnregisterEntity(this);
        }
    }

    #endregion

    #region 保護メソッド（派生クラス用）

    /// <summary>
    /// 占有グリッドリストを更新（派生クラスから呼び出し）
    /// 移動時やサイズ変更時に使用
    /// </summary>
    /// <param name="newOccupiedGrids">新しい占有グリッドリスト</param>
    protected void UpdateOccupiedGrids(List<Vector2Int> newOccupiedGrids)
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.UpdateEntityPosition(this, newOccupiedGrids);
        }
        else
        {
            // MapManagerがない場合は内部だけ更新
            OccupiedGrids.Clear();
            OccupiedGrids.AddRange(newOccupiedGrids);
        }
    }

    /// <summary>
    /// 単一グリッド用の初期化ヘルパー
    /// </summary>
    /// <param name="gridPosition">グリッド座標</param>
    protected void InitializeSingleGridEntity(Vector2Int gridPosition)
    {
        RootGridPosition = gridPosition;
        OccupiedGrids.Clear();
        OccupiedGrids.Add(gridPosition);
    }

    /// <summary>
    /// 矩形エリア用の初期化ヘルパー（大型モンスター用）
    /// </summary>
    /// <param name="rootPosition">左下座標</param>
    /// <param name="width">幅（グリッド数）</param>
    /// <param name="height">高さ（グリッド数）</param>
    protected void InitializeMultiGridEntity(Vector2Int rootPosition, int width, int height)
    {
        RootGridPosition = rootPosition;
        OccupiedGrids.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                OccupiedGrids.Add(rootPosition + new Vector2Int(x, y));
            }
        }
    }

    #endregion
}

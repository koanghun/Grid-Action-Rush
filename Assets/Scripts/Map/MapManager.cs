using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// マップ全体を管理するシングルトンマネージャー
/// Unity Tilemapからグリッドデータを読み込み、障害物判定APIを提供
/// </summary>
public class MapManager : MonoBehaviour
{
    #region シングルトンパターン

    private static MapManager instance;

    /// <summary>
    /// シングルトンインスタンス（Lazy Initialization）
    /// </summary>
    public static MapManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Sceneから既存のMapManagerを検索
                instance = FindAnyObjectByType<MapManager>();

                // 見つからない場合は自動生成
                if (instance == null)
                {
                    GameObject obj = new GameObject("MapManager");
                    instance = obj.AddComponent<MapManager>();
                    Debug.LogWarning("[MapManager] Sceneに存在しないため自動生成しました。Tilemapの設定を忘れずに。");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 重複インスタンスチェック
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[MapManager] 既にインスタンスが存在します。このオブジェクトを破棄します。");
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    #endregion

    #region Inspector設定

    [Header("Tilemap参照")]
    [Tooltip("レベルデザインで使用するTilemap（Inspector上でアサイン）")]
    [SerializeField] private Tilemap tilemap;

    [Header("タイルデータマッピング")]
    [Tooltip("Tileに対応するTileDataのリスト（順番は問わない）")]
    [SerializeField] private List<TileMapping> tileMappings = new List<TileMapping>();

    #endregion

    #region 内部データ構造

    /// <summary>
    /// グリッド座標 → TileDataのマッピング（O(1)アクセス）
    /// </summary>
    private Dictionary<Vector2Int, TileData> tileDataMap = new Dictionary<Vector2Int, TileData>();

    /// <summary>
    /// Unity Tile → TileDataのマッピング（Tilemap解析用）
    /// </summary>
    private Dictionary<TileBase, TileData> tileToDataMap = new Dictionary<TileBase, TileData>();

    /// <summary>
    /// グリッド座標 → エンティティリストのマッピング（エンティティ追跡用）
    /// </summary>
    private Dictionary<Vector2Int, List<IGridEntity>> gridEntityMap = new Dictionary<Vector2Int, List<IGridEntity>>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeTileDataMap();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// Tilemapからマップデータを読み込み、Dictionaryを構築
    /// </summary>
    private void InitializeTileDataMap()
    {
        // Tilemap参照チェック
        if (tilemap == null)
        {
            Debug.LogError("[MapManager] Tilemap参照がnullです。Inspectorで設定してください。");
            return;
        }

        // TileMappingsチェック
        if (tileMappings == null)
        {
            Debug.LogWarning("[MapManager] TileMappingsがnullです。空のリストとして扱います。");
            tileMappings = new List<TileMapping>();
        }

        // TileMappingからTile→TileDataのマッピングを構築
        foreach (var mapping in tileMappings)
        {
            if (mapping.tile != null && mapping.tileData != null)
            {
                tileToDataMap[mapping.tile] = mapping.tileData;
            }
        }

        // Tilemapの範囲を取得
        BoundsInt bounds = tilemap.cellBounds;

        // 全セルを走査してDictionaryに登録
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
            TileBase tile = tilemap.GetTile(pos);

            if (tile != null && tileToDataMap.ContainsKey(tile))
            {
                tileDataMap[gridPos] = tileToDataMap[tile];
            }
        }

        Debug.Log($"[MapManager] マップデータ初期化完了: {tileDataMap.Count}タイル登録");
    }

    #endregion

    #region 公開API

    /// <summary>
    /// 指定座標の障害物タイプを取得
    /// </summary>
    /// <param name="gridPos">グリッド座標</param>
    /// <returns>障害物タイプ（タイルが存在しない場合はNone）</returns>
    public ObstacleType GetObstacleType(Vector2Int gridPos)
    {
        if (tileDataMap.TryGetValue(gridPos, out TileData data))
        {
            return data.ObstacleType;
        }

        // タイルが存在しない座標（マップ外）
        return ObstacleType.None;
    }

    /// <summary>
    /// 指定座標が通行可能か判定
    /// </summary>
    /// <param name="gridPos">グリッド座標</param>
    /// <returns>通行可能ならtrue、不可能またはマップ外ならfalse</returns>
    public bool IsWalkable(Vector2Int gridPos)
    {
        if (tileDataMap.TryGetValue(gridPos, out TileData data))
        {
            return data.IsWalkable;
        }

        // タイルが存在しない座標（マップ外）は通行不可
        return false;
    }

    /// <summary>
    /// 指定座標のTileDataを取得（null許容）
    /// </summary>
    /// <param name="gridPos">グリッド座標</param>
    /// <returns>TileData（存在しない場合はnull）</returns>
    public TileData GetTileData(Vector2Int gridPos)
    {
        tileDataMap.TryGetValue(gridPos, out TileData data);
        return data;
    }

    #endregion

    #region エンティティ管理API

    /// <summary>
    /// エンティティをグリッドに登録
    /// </summary>
    /// <param name="entity">登録するエンティティ</param>
    public void RegisterEntity(IGridEntity entity)
    {
        if (entity == null || entity.OccupiedGrids == null)
        {
            Debug.LogWarning("[MapManager] エンティティまたはOccupiedGridsがnullです。登録をスキップします。");
            return;
        }

        foreach (Vector2Int gridPos in entity.OccupiedGrids)
        {
            if (!gridEntityMap.ContainsKey(gridPos))
            {
                gridEntityMap[gridPos] = new List<IGridEntity>();
            }

            gridEntityMap[gridPos].Add(entity);
        }

        Debug.Log($"[MapManager] エンティティ登録: {entity.EntityType} at {entity.RootGridPosition}");
    }

    /// <summary>
    /// エンティティをグリッドから削除
    /// </summary>
    /// <param name="entity">削除するエンティティ</param>
    public void UnregisterEntity(IGridEntity entity)
    {
        if (entity == null || entity.OccupiedGrids == null)
        {
            return;
        }

        foreach (Vector2Int gridPos in entity.OccupiedGrids)
        {
            if (gridEntityMap.TryGetValue(gridPos, out List<IGridEntity> list))
            {
                list.Remove(entity);

                // リストが空になったらディクショナリからも削除（メモリ節約）
                if (list.Count == 0)
                {
                    gridEntityMap.Remove(gridPos);
                }
            }
        }

        Debug.Log($"[MapManager] エンティティ削除: {entity.EntityType} at {entity.RootGridPosition}");
    }

    /// <summary>
    /// エンティティの位置を更新（移動時に使用）
    /// アルゴリズム: Simple & Safe（全削除 → 再登録）
    /// </summary>
    /// <param name="entity">更新するエンティティ</param>
    /// <param name="newOccupiedGrids">新しい占有グリッドリスト</param>
    public void UpdateEntityPosition(IGridEntity entity, List<Vector2Int> newOccupiedGrids)
    {
        if (entity == null || newOccupiedGrids == null)
        {
            Debug.LogWarning("[MapManager] エンティティまたは新座標リストがnullです。");
            return;
        }

        // Step 1: 既存位置から完全に削除
        foreach (Vector2Int oldGrid in entity.OccupiedGrids)
        {
            if (gridEntityMap.TryGetValue(oldGrid, out List<IGridEntity> list))
            {
                list.Remove(entity);
                if (list.Count == 0)
                {
                    gridEntityMap.Remove(oldGrid);
                }
            }
        }

        // Step 2: 新位置に登録
        entity.OccupiedGrids.Clear();
        entity.OccupiedGrids.AddRange(newOccupiedGrids);

        foreach (Vector2Int newGrid in newOccupiedGrids)
        {
            if (!gridEntityMap.ContainsKey(newGrid))
            {
                gridEntityMap[newGrid] = new List<IGridEntity>();
            }
            gridEntityMap[newGrid].Add(entity);
        }
    }

    /// <summary>
    /// 指定グリッド座標のエンティティを取得
    /// </summary>
    /// <param name="gridPos">グリッド座標</param>
    /// <returns>エンティティリスト（存在しない場合は空リスト）</returns>
    public List<IGridEntity> GetEntitiesAt(Vector2Int gridPos)
    {
        if (gridEntityMap.TryGetValue(gridPos, out List<IGridEntity> entities))
        {
            return entities;
        }

        return new List<IGridEntity>();
    }

    /// <summary>
    /// 指定グリッド座標の特定タイプのエンティティを取得
    /// </summary>
    /// <param name="gridPos">グリッド座標</param>
    /// <param name="type">エンティティタイプ</param>
    /// <returns>フィルタリングされたエンティティリスト</returns>
    public List<IGridEntity> GetEntitiesAt(Vector2Int gridPos, GridEntityType type)
    {
        List<IGridEntity> result = new List<IGridEntity>();

        if (gridEntityMap.TryGetValue(gridPos, out List<IGridEntity> entities))
        {
            foreach (IGridEntity entity in entities)
            {
                if (entity.EntityType == type)
                {
                    result.Add(entity);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 複数グリッド範囲のエンティティを取得（攻撃範囲用、重複除去済み）
    /// </summary>
    /// <param name="gridPositions">対象グリッド座標リスト</param>
    /// <returns>重複のないエンティティセット</returns>
    public HashSet<IGridEntity> GetEntitiesInArea(List<Vector2Int> gridPositions)
    {
        HashSet<IGridEntity> result = new HashSet<IGridEntity>();

        foreach (Vector2Int gridPos in gridPositions)
        {
            if (gridEntityMap.TryGetValue(gridPos, out List<IGridEntity> entities))
            {
                foreach (IGridEntity entity in entities)
                {
                    result.Add(entity);
                }
            }
        }

        return result;
    }

    #endregion

    #region デバッグ用

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 初期化前は描画しない
        if (tileDataMap == null || tileDataMap.Count == 0)
            return;

        if (tilemap == null)
            return;

        // 各タイルの通行可否を可視化
        foreach (var kvp in tileDataMap)
        {
            Vector2Int gridPos = kvp.Key;
            TileData data = kvp.Value;

            Vector3Int cellPos = new Vector3Int(gridPos.x, gridPos.y, 0);
            Vector3 worldPos = tilemap.CellToWorld(cellPos) + tilemap.cellSize / 2f;

            // 通行可能 = 緑、通行不可 = 赤
            Gizmos.color = data.IsWalkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(worldPos, tilemap.cellSize * 0.9f);
        }
    }
#endif

    #endregion
}

/// <summary>
/// Inspector上でTileとTileDataをマッピングするためのデータ構造
/// </summary>
[System.Serializable]
public class TileMapping
{
    [Tooltip("Unity Tilemap上のタイル")]
    public TileBase tile;

    [Tooltip("対応するTileData ScriptableObject")]
    public TileData tileData;
}
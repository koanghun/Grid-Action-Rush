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

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static MapManager Instance { get; private set; }

    private void Awake()
    {
        // シングルトンチェック（同じSceneに複数存在する場合は破棄）
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MapManager] 既にインスタンスが存在します。このオブジェクトを破棄します。");
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (tilemap == null)
        {
            Debug.LogError("[MapManager] Tilemap参照がnullです。Inspectorで設定してください。");
            return;
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
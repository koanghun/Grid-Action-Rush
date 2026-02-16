using UnityEngine;

/// <summary>
/// タイルの属性データを定義するScriptableObject
/// Unity Editorで各タイルタイプ（壁、床、崖など）を作成
/// </summary>
[CreateAssetMenu(fileName = "NewTileData", menuName = "Map/TileData", order = 0)]
public class TileData : ScriptableObject
{
    #region Inspector設定

    [Header("基本情報")]
    [Tooltip("タイル名（Inspector表示用）")]
    [SerializeField] private string tileName = "Default Tile";

    [Header("通行可否")]
    [Tooltip("このタイルを通行可能にする")]
    [SerializeField] private bool isWalkable = true;

    [Header("障害物タイプ")]
    [Tooltip("障害物の種類（スキル判定で使用）")]
    [SerializeField] private ObstacleType obstacleType = ObstacleType.None;

    [Header("移動コスト（将来実装）")]
    [Tooltip("移動速度の倍率（1.0が通常、2.0で半分の速度になる）")]
    [SerializeField] private float moveCostMultiplier = 1.0f;

    #endregion

    #region 公開プロパティ

    /// <summary>
    /// タイル名
    /// </summary>
    public string TileName => tileName;

    /// <summary>
    /// 通行可能かどうか
    /// </summary>
    public bool IsWalkable => isWalkable;

    /// <summary>
    /// 障害物タイプ
    /// </summary>
    public ObstacleType ObstacleType => obstacleType;

    /// <summary>
    /// 移動コスト倍率（将来実装：沼地や氷など）
    /// </summary>
    public float MoveCostMultiplier => moveCostMultiplier;

    #endregion
}
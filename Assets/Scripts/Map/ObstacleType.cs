/// <summary>
/// グリッド上の障害物タイプの列挙型
/// </summary>
public enum ObstacleType
{
    None = 0,        // 障害物なし
    Wall = 1,        // 壁
    Monster = 2,     // モンスター
    Cliff = 3,       // 崖（将来実装）
    Hazard = 4       // ハザード
}
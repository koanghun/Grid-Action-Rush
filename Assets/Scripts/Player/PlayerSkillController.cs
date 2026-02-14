using UnityEngine;

/// <summary>
/// プレイヤースキル管理コントローラー
/// 全スキル（回避、ダッシュ等）のロジックを担当
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    #region Inspector設定

    [Header("回避スキル設定")]
    [SerializeField] private MovementSkillData dodgeSkillData;

    #endregion

    #region 依存コンポーネント

    private PlayerController playerController;
    // TODO: MapManager実装後に追加
    // private MapManager mapManager;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 依存コンポーネント取得
        playerController = GetComponent<PlayerController>();

        // コンポーネント存在確認
        if (playerController == null)
        {
            Debug.LogError("[PlayerSkillController] PlayerControllerが見つかりません。");
        }

        // TODO: MapManager実装後に追加
        // mapManager = FindObjectOfType<MapManager>();
        // if (mapManager == null)
        // {
        //     Debug.LogError("[PlayerSkillController] MapManagerが見つかりません。");
        // }
    }

    #endregion

    #region 回避スキル

    /// <summary>
    /// 回避スキルの実行
    /// PlayerControllerから呼び出される
    /// </summary>
    public void PerformDodge()
    {
        // クールタイム中 or スキルデータが未設定の場合は無視
        if (playerController.IsMoving || dodgeSkillData == null)
        {
            return;
        }

        // 現在の状態を取得
        Vector2Int currentPos = playerController.CurrentGridPosition;
        Vector2Int facingDir = playerController.FacingDirection;

        // 回避経路を計算
        Vector2Int targetPos = CalculateDodgePath(currentPos, facingDir, dodgeSkillData.dashDistance);

        // 移動先が現在位置と同じ場合は何もしない（壁があるなど）
        if (targetPos == currentPos)
        {
            return;
        }

        // PlayerControllerを通じて移動リクエスト（Facadeパターン）
        // GridMovementControllerを直接参照しない
        playerController.RequestMoveToPosition(targetPos, dodgeSkillData.speedMultiplier);
    }

    /// <summary>
    /// 回避経路を計算（障害物判定含む）
    /// </summary>
    /// <param name="startPos">開始位置</param>
    /// <param name="direction">移動方向</param>
    /// <param name="distance">ダッシュ距離（マス数）</param>
    /// <returns>実際に移動可能な最終座標</returns>
    private Vector2Int CalculateDodgePath(Vector2Int startPos, Vector2Int direction, int distance)
    {
        Vector2Int finalPos = startPos;

        for (int i = 1; i <= distance; i++)
        {
            Vector2Int nextPos = startPos + (direction * i);
            
            // TODO: MapManager実装後に変更
            // ObstacleType obstacle = mapManager.GetObstacleType(nextPos);
            ObstacleType obstacle = ObstacleType.None; // 仮実装：障害物なしとして扱う

            // 壁やモンスターの場合は手前で停止
            if (obstacle == ObstacleType.Wall && !dodgeSkillData.canPassWall)
            {
                break;
            }
            if (obstacle == ObstacleType.Monster && !dodgeSkillData.canPassMonster)
            {
                break;
            }

            // TODO (将来実装): 崖のスキップ処理
            // if (obstacle == ObstacleType.Cliff && !dodgeSkillData.canPassCliff)
            // {
            //     continue; // この座標はスキップして次を確認
            // }

            // 移動可能なら位置を更新
            finalPos = nextPos;
        }

        return finalPos;
    }

    #endregion
}


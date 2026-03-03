using UnityEngine;

/// <summary>
/// グリッド回避（ダッシュ）スキル実装
/// PlayerSkillControllerから移行したロジックをSkillBaseに準拠した形で再実装
/// </summary>
public class DodgeSkill : SkillBase
{
    #region データ参照

    private readonly MovementSkillData moveData;

    #endregion

    #region コンストラクタ

    public DodgeSkill(MovementSkillData data, PlayerController controller) : base(data, controller)
    {
        moveData = data;
    }

    #endregion

    #region SkillBase 実装

    protected override void OnExecute()
    {
        Vector2Int currentPos = playerController.CurrentGridPosition;
        Vector2Int facing = playerController.FacingDirection;
        Vector2Int targetPos = CalculateDodgePath(currentPos, facing, moveData.dashDistance);

        // 移動先が変わらない場合（壁など）はキャンセル
        if (targetPos == currentPos) return;

        playerController.RequestMoveToPosition(targetPos, moveData.speedMultiplier);
        Debug.Log($"[DodgeSkill] 回避実行: {currentPos} → {targetPos}");
    }

    #endregion

    #region 経路計算

    /// <summary>
    /// 障害物を考慮しながら最終到達地点を計算
    /// </summary>
    private Vector2Int CalculateDodgePath(Vector2Int start, Vector2Int dir, int distance)
    {
        Vector2Int finalPos = start;

        for (int i = 1; i <= distance; i++)
        {
            Vector2Int next = start + dir * i;
            ObstacleType obstacle = MapManager.Instance != null
                ? MapManager.Instance.GetObstacleType(next)
                : ObstacleType.None;

            if (obstacle == ObstacleType.Wall && !moveData.canPassWall) break;
            if (obstacle == ObstacleType.Monster && !moveData.canPassMonster) break;

            finalPos = next;
        }

        return finalPos;
    }

    #endregion
}

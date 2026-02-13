using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// グリッドベースのプレイヤー移動コントローラー（戦闘シーン専用）
/// タイル単位での移動とクールタイムベース入力管理を実装
/// </summary>
public class GridMovementController : MonoBehaviour
{
    #region Inspector設定

    [Header("Grid設定")]
    [SerializeField] private Grid grid;
    [Tooltip("1秒あたりに移動するタイル数")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("回避スキル設定")]
    [SerializeField] private MovementSkillData dodgeSkill;
    [Tooltip("障害物判定用のレイヤーマスク")]
    [SerializeField] private LayerMask obstacleLayerMask;

    #endregion

    #region 内部状態

    // Input System
    private InputSystem_Actions inputActions;

    // 移動状態
    private Vector2Int currentGridPosition;
    private Vector2Int facingDirection = Vector2Int.down;  // 現在の向き（初期: 下向き）
    private bool isMoving = false;  // true = クールタイム中（入力受付不可）

    // キャンセルトークン
    private CancellationTokenSource moveCts;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Input Actionsの初期化
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        
        // 移動入力イベントを購読
        inputActions.Player.Move.performed += OnMovePerformed;
        
        // 回避スキル入力イベントを購読
        inputActions.Player.Dodge.performed += OnDodgePerformed;
    }

    private void OnDisable()
    {
        // イベント購読解除
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Dodge.performed -= OnDodgePerformed;
        inputActions.Disable();
    }

    private void Start()
    {
        InitializeGridPosition();
    }

    private void OnDestroy()
    {
        // 移動中のタスクをキャンセル
        moveCts?.Cancel();
        moveCts?.Dispose();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// グリッド位置の初期化（現在のワールド座標から算出）
    /// </summary>
    private void InitializeGridPosition()
    {
        if (grid == null)
        {
            Debug.LogError("[GridMovementController] Grid参照がnullです。Inspectorで設定してください。");
            return;
        }

        // 現在のワールド座標からグリッド座標を取得
        Vector3Int cellPosition = grid.WorldToCell(transform.position);
        currentGridPosition = new Vector2Int(cellPosition.x, cellPosition.y);

        // タイル中央にスナップ
        Vector3 centerPosition = grid.CellToWorld(cellPosition) + grid.cellSize / 2f;
        transform.position = centerPosition;
    }

    #endregion

    #region 入力処理

    /// <summary>
    /// 移動入力イベントハンドラー（方向キーのみ対応）
    /// QWERAF はスキル/攻撃用に予約済み
    /// </summary>
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // クールタイム中は入力を無視
        if (isMoving)
        {
            return;
        }

        // Move アクションから入力を取得
        Vector2 input = context.ReadValue<Vector2>();

        // 方向キーのみを受け付ける（デジタル入力として扱う）
        // アナログスティックの場合は閾値を設定
        Vector2Int direction = Vector2Int.zero;

        if (Mathf.Abs(input.x) > 0.5f)
        {
            direction.x = input.x > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(input.y) > 0.5f)
        {
            direction.y = input.y > 0 ? 1 : -1;
        }

        // 入力がない場合は処理しない
        if (direction == Vector2Int.zero)
        {
            return;
        }

        // 向きを更新
        UpdateFacingDirection(direction);
        
        // 移動開始
        TryMove(direction);
    }

    /// <summary>
    /// 指定方向への移動を試行
    /// </summary>
    /// <param name="direction">移動方向（-1, 0, 1 のみ）</param>
    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGridPos = currentGridPosition + direction;

        // TODO: 将来的にここで衝突判定や移動可能判定を追加
        // 例: if (!IsWalkable(targetGridPos)) return;

        // 移動タスク開始
        moveCts?.Cancel();
        moveCts?.Dispose();
        moveCts = new CancellationTokenSource();

        // isMovingをフラグにして管理するのでawaitしない
        MoveToGridPositionAsync(targetGridPos, moveCts.Token).Forget();
    }

    #endregion

    #region 移動処理

    /// <summary>
    /// 指定グリッド座標への移動（UniTask使用）
    /// Vector3.Lerpで滑らかに補間
    /// </summary>
    /// <param name="targetGridPos">目標グリッド座標</param>
    /// <param name="ct">キャンセルトークン</param>
    private async UniTask MoveToGridPositionAsync(Vector2Int targetGridPos, CancellationToken ct)
    {
        isMoving = true;

        // 開始位置と目標位置を計算
        Vector3 startPos = transform.position;
        Vector3Int targetCell = new Vector3Int(targetGridPos.x, targetGridPos.y, 0);
        Vector3 targetPos = grid.CellToWorld(targetCell) + grid.cellSize / 2f;

        // 移動時間を算出
        float duration = 1f / moveSpeed;
        float elapsed = 0f;

        // Lerpで補間しながら移動
        while (elapsed < duration)
        {
            // キャンセル確認
            ct.ThrowIfCancellationRequested();

            // 線形補間
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 最終位置を正確に設定（浮動小数点誤差を排除）
        transform.position = targetPos;
        currentGridPosition = targetGridPos;
        
        // クールタイム終了、次の入力を受け付け可能に
        isMoving = false;
    }

    #endregion

    #region 回避スキル

    /// <summary>
    /// 回避スキル入力イベントハンドラー
    /// </summary>
    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
        TryDodge();
    }

    /// <summary>
    /// 回避スキルの実行を試行
    /// </summary>
    private void TryDodge()
    {
        // クールタイム中 or スキルデータが未設定の場合は無視
        if (isMoving || dodgeSkill == null)
        {
            return;
        }

        // 回避経路を計算
        Vector2Int targetPos = CalculateDodgePath(facingDirection, dodgeSkill.dashDistance);

        // 移動先が現在位置と同じ場合は向きのみ更新（壁があるなど）
        if (targetPos == currentGridPosition)
        {
            return;
        }

        // 回避タスク開始
        moveCts?.Cancel();
        moveCts?.Dispose();
        moveCts = new CancellationTokenSource();

        // 通常移動より高速なダッシュ
        DodgeAsync(targetPos, dodgeSkill.speedMultiplier, moveCts.Token).Forget();
    }

    /// <summary>
    /// 回避経路を計算（障害物判定含む）
    /// </summary>
    /// <param name="direction">移動方向</param>
    /// <param name="distance">ダッシュ距離（マス数）</param>
    /// <returns>実際に移動可能な最終座標</returns>
    private Vector2Int CalculateDodgePath(Vector2Int direction, int distance)
    {
        Vector2Int finalPos = currentGridPosition;

        for (int i = 1; i <= distance; i++)
        {
            Vector2Int nextPos = currentGridPosition + (direction * i);
            ObstacleType obstacle = GetObstacleType(nextPos);

            // 壁やモンスターの場合は手前で停止
            if (obstacle == ObstacleType.Wall && !dodgeSkill.canPassWall)
            {
                break;
            }
            if (obstacle == ObstacleType.Monster && !dodgeSkill.canPassMonster)
            {
                break;
            }

            // TODO (将来実装): 崖のスキップ処理
            // if (obstacle == ObstacleType.Cliff && !dodgeSkill.canPassCliff)
            // {
            //     continue; // この座標はスキップして次を確認
            // }

            // 移動可能なら位置を更新
            finalPos = nextPos;
        }

        return finalPos;
    }

    /// <summary>
    /// 高速ダッシュ移動（UniTask使用）
    /// </summary>
    /// <param name="targetGridPos">目標グリッド座標</param>
    /// <param name="speedMultiplier">速度倍率</param>
    /// <param name="ct">キャンセルトークン</param>
    private async UniTask DodgeAsync(Vector2Int targetGridPos, float speedMultiplier, CancellationToken ct)
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3Int targetCell = new Vector3Int(targetGridPos.x, targetGridPos.y, 0);
        Vector3 targetPos = grid.CellToWorld(targetCell) + grid.cellSize / 2f;

        // 通常移動より高速（speedMultiplier倍）
        float duration = 1f / (moveSpeed * speedMultiplier);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        transform.position = targetPos;
        currentGridPosition = targetGridPos;
        isMoving = false;
    }

    #endregion

    #region 障害物判定

    /// <summary>
    /// 指定グリッド座標の障害物タイプを判定
    /// </summary>
    /// <param name="gridPos">チェックするグリッド座標</param>
    /// <returns>障害物タイプ</returns>
    private ObstacleType GetObstacleType(Vector2Int gridPos)
    {
        // グリッド座標をワールド座標に変換
        Vector3Int cellPos = new Vector3Int(gridPos.x, gridPos.y, 0);
        Vector3 worldPos = grid.CellToWorld(cellPos) + grid.cellSize / 2f;

        // Physics2Dで障害物を検出
        Collider2D hit = Physics2D.OverlapCircle(worldPos, grid.cellSize.x * 0.4f, obstacleLayerMask);

        if (hit == null)
        {
            return ObstacleType.None;
        }

        // タグで障害物タイプを判定
        // TODO: 将来的にはTilemapやマップデータから判定する方式に変更
        if (hit.CompareTag("Wall"))
        {
            return ObstacleType.Wall;
        }
        if (hit.CompareTag("Enemy"))
        {
            return ObstacleType.Monster;
        }

        return ObstacleType.None;
    }

    #endregion

    #region 向き管理

    /// <summary>
    /// 移動方向に応じて向きを更新
    /// </summary>
    /// <param name="direction">移動方向</param>
    private void UpdateFacingDirection(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
        {
            facingDirection = direction;
        }
    }

    #endregion

    #region デバッグ用

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (grid == null) return;

        // 現在のグリッド位置を可視化
        Gizmos.color = Color.green;
        Vector3Int cellPos = new Vector3Int(currentGridPosition.x, currentGridPosition.y, 0);
        Vector3 center = grid.CellToWorld(cellPos) + grid.cellSize / 2f;
        Gizmos.DrawWireCube(center, grid.cellSize * 0.9f);

        // 向きを矢印で表示
        Gizmos.color = Color.yellow;
        Vector3 arrowEnd = center + new Vector3(facingDirection.x, facingDirection.y, 0) * grid.cellSize.x * 0.5f;
        DrawArrow(center, arrowEnd);
    }

    private void DrawArrow(Vector3 start, Vector3 end)
    {
        Gizmos.DrawLine(start, end);
        Vector3 direction = (end - start).normalized;
        Vector3 right = new Vector3(-direction.y, direction.x, 0);
        Vector3 arrowHead1 = end - direction * 0.2f + right * 0.1f;
        Vector3 arrowHead2 = end - direction * 0.2f - right * 0.1f;
        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
    }

#endif

    #endregion
}

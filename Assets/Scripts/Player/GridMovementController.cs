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

    #endregion

    #region 内部状態

    // Input System
    private InputSystem_Actions inputActions;

    // 移動状態
    private Vector2Int currentGridPosition;
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
    }

    private void OnDisable()
    {
        // イベント購読解除
        inputActions.Player.Move.performed -= OnMovePerformed;
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
    }

#endif

    #endregion
}

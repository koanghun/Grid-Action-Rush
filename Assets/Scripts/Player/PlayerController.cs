using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの全体的な動作を管理するメインコントローラー
/// Input Systemの中央ハブとして機能し、各種コンポーネントに入力を配信
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region コンポーネント参照

    private GridMovementController movementController;
    private PlayerSkillController skillController;
    // TODO: 将来的に追加予定
    // private PlayerCombatController combatController;

    #endregion

    #region Input System

    private InputSystem_Actions inputActions;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 各種コンポーネントの取得
        movementController = GetComponent<GridMovementController>();
        skillController = GetComponent<PlayerSkillController>();

        // コンポーネントの存在確認
        if (movementController == null)
        {
            Debug.LogError("[PlayerController] GridMovementControllerが見つかりません。");
        }
        if (skillController == null)
        {
            Debug.LogWarning("[PlayerController] PlayerSkillControllerが見つかりません。スキル機能が無効です。");
        }

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

        // TODO: 将来的に追加予定
        // inputActions.Player.Attack.performed += OnAttackPerformed;
        // inputActions.Player.Skill1.performed += OnSkill1Performed;
    }

    private void OnDisable()
    {
        // イベント購読解除
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Dodge.performed -= OnDodgePerformed;

        inputActions.Disable();
    }

    #endregion

    #region 入力イベントハンドラー

    /// <summary>
    /// 移動入力イベントハンドラー
    /// GridMovementControllerに処理を委譲
    /// </summary>
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (movementController == null) return;

        Vector2 input = context.ReadValue<Vector2>();
        Vector2Int direction = ConvertInputToDirection(input);

        if (direction == Vector2Int.zero) return;

        // Facadeメソッドを使用
        RequestMove(direction);
    }

    /// <summary>
    /// 回避スキル入力イベントハンドラー
    /// GridMovementControllerに処理を委譲
    /// </summary>
    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
        if (skillController == null) return;

        // PlayerSkillControllerに回避を指示
        skillController.PerformDodge();
    }

    // TODO: 将来的に追加予定
    // private void OnAttackPerformed(InputAction.CallbackContext context)
    // {
    //     if (IsMoving) return;  // 移動中は攻撃不可
    //     combatController?.PerformAttack(movementController.FacingDirection);
    // }

    #endregion

    #region Facadeメソッド（移動リクエストの中央集約）

    /// <summary>
    /// 一般移動リクエスト（Facadeメソッド）
    /// 入力システムや他のコンポーネントから呼び出される
    /// </summary>
    /// <param name="direction">移動方向</param>
    public void RequestMove(Vector2Int direction)
    {
        movementController?.Move(direction);
    }

    /// <summary>
    /// 指定座標への移動リクエスト（スキル用、Facadeメソッド）
    /// スキルシステムから呼び出され、GridMovementControllerを直接参照する必要を無くす
    /// </summary>
    /// <param name="targetPos">目標グリッド座標</param>
    /// <param name="speedMultiplier">速度倍率（1.0が通常速度）</param>
    public void RequestMoveToPosition(Vector2Int targetPos, float speedMultiplier = 1.0f)
    {
        movementController?.MoveToPosition(targetPos, speedMultiplier);
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// Vector2入力をグリッド方向(Vector2Int)に変換
    /// アナログ入力を4方向のデジタル入力として扱う
    /// </summary>
    /// <param name="input">Input Systemから取得した入力ベクトル</param>
    /// <returns>グリッド方向（-1, 0, 1のみ）</returns>
    private Vector2Int ConvertInputToDirection(Vector2 input)
    {
        Vector2Int direction = Vector2Int.zero;

        // X軸とY軸で閾値を超えた方を優先
        // X軸を優先することで、斜め入力時は横移動になる
        if (Mathf.Abs(input.x) > 0.5f)
        {
            direction.x = input.x > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(input.y) > 0.5f)
        {
            direction.y = input.y > 0 ? 1 : -1;
        }

        return direction;
    }

    #endregion

    #region 公開プロパティ（状態照会用）

    /// <summary>
    /// プレイヤーが移動中かどうか
    /// 他のシステムが状態を確認するために使用
    /// </summary>
    public bool IsMoving => movementController != null && movementController.IsMoving;

    /// <summary>
    /// プレイヤーの現在のグリッド座標
    /// スキルシステムが参照
    /// </summary>
    public Vector2Int CurrentGridPosition => movementController != null ? movementController.CurrentGridPosition : Vector2Int.zero;

    /// <summary>
    /// プレイヤーの現在の向き
    /// 攻撃システムなどで使用
    /// </summary>
    public Vector2Int FacingDirection => movementController != null ? movementController.FacingDirection : Vector2Int.down;

    #endregion
}

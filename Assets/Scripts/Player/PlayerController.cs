using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの全体的な動作を管理するメインコントローラー
/// スキルスロット方式を採用し、新スキル追加時にこのクラスを変更しない設計
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Inspector設定

    [Header("移動設定")]
    [SerializeField] private GridMovementController movementController;

    [Header("スキルスロット")]
    [Tooltip("攻撃スキルデータ")]
    [SerializeField] private AttackSkillData attackSkillData;

    [Tooltip("回避スキルデータ")]
    [SerializeField] private MovementSkillData dodgeSkillData;

    [Header("固有スキルスロット (Q/W/E/R)")]
    [Tooltip("スロット0=Q, 1=W, 2=E, 3=R")]
    [SerializeField] private SkillData[] uniqueSkillDatas = new SkillData[4];

    #endregion

    #region スキルスロット

    // ISkillインターフェースで統一管理することで、スキル追加時にこのクラスを変更不要
    private ISkill attackSkill;
    private ISkill dodgeSkill;
    private readonly ISkill[] uniqueSkills = new ISkill[4];

    #endregion

    #region Input System

    private InputSystem_Actions inputActions;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // GridMovementControllerがInspectorで未設定の場合はGetComponentで取得
        if (movementController == null)
        {
            movementController = GetComponent<GridMovementController>();
        }

        if (movementController == null)
        {
            Debug.LogError("[PlayerController] GridMovementControllerが見つかりません。");
        }

        // スキルインスタンスをここで生成（MonoBehaviourではなく純粋なC#クラス）
        // →Inspectorのデータを注入するだけで、スキルの追加・変更が容易
        attackSkill = attackSkillData != null ? new AttackSkill(attackSkillData, this) : null;
        dodgeSkill = dodgeSkillData != null ? new DodgeSkill(dodgeSkillData, this) : null;

        if (attackSkill == null) Debug.LogWarning("[PlayerController] AttackSkillDataが未設定です。攻撃機能が無効です。");
        if (dodgeSkill == null) Debug.LogWarning("[PlayerController] MovementSkillDataが未設定です。回避機能が無効です。");

        // 固有スキルスロットの初期化（Q/W/E/R）
        // SkillDataのサブタイプを判定してISkillを生成するFactoryパターン
        for (int i = 0; i < uniqueSkillDatas.Length; i++)
        {
            uniqueSkills[i] = CreateSkillFromData(uniqueSkillDatas[i]);
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Dodge.performed += OnDodgePerformed;
        inputActions.Player.Attack.performed += OnAttackPerformed;
        inputActions.Player.Skill1.performed += OnSkill1Performed;
        inputActions.Player.Skill2.performed += OnSkill2Performed;
        inputActions.Player.Skill3.performed += OnSkill3Performed;
        inputActions.Player.Skill4.performed += OnSkill4Performed;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Dodge.performed -= OnDodgePerformed;
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Skill1.performed -= OnSkill1Performed;
        inputActions.Player.Skill2.performed -= OnSkill2Performed;
        inputActions.Player.Skill3.performed -= OnSkill3Performed;
        inputActions.Player.Skill4.performed -= OnSkill4Performed;
        inputActions.Disable();
    }

    #endregion

    #region 入力イベントハンドラー

    /// <summary>
    /// 移動入力ハンドラー
    /// </summary>
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        Vector2Int direction = ConvertInputToDirection(input);
        if (direction != Vector2Int.zero) RequestMove(direction);
    }

    /// <summary>
    /// 回避スキル入力ハンドラー
    /// スロット経由で実行することで、スキルの差し替えがここに影響しない
    /// </summary>
    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
        dodgeSkill?.Execute();
    }

    /// <summary>
    /// 攻撃入力ハンドラー
    /// スロット経由で実行することで、スキルの差し替えがここに影響しない
    /// </summary>
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        attackSkill?.Execute();
    }

    /// <summary>
    /// 固有スキル入力ハンドラー（Q/W/E/R）
    /// スロットインデックスで統一管理し、ハンドラーの重複を最小化
    /// </summary>
    private void OnSkill1Performed(InputAction.CallbackContext context) => uniqueSkills[0]?.Execute();
    private void OnSkill2Performed(InputAction.CallbackContext context) => uniqueSkills[1]?.Execute();
    private void OnSkill3Performed(InputAction.CallbackContext context) => uniqueSkills[2]?.Execute();
    private void OnSkill4Performed(InputAction.CallbackContext context) => uniqueSkills[3]?.Execute();

    #endregion

    #region Facadeメソッド（スキルから呼び出される移動リクエスト）

    /// <summary>
    /// 方向指定移動リクエスト
    /// </summary>
    public void RequestMove(Vector2Int direction)
    {
        movementController?.Move(direction);
    }

    /// <summary>
    /// 座標指定移動リクエスト（スキル用）
    /// DodgeSkillなどのスキルがGridMovementControllerを直接参照しないための窓口
    /// </summary>
    public void RequestMoveToPosition(Vector2Int targetPos, float speedMultiplier = 1.0f)
    {
        movementController?.MoveToPosition(targetPos, speedMultiplier);
    }

    #endregion

    #region ヘルパー

    /// <summary>
    /// SkillDataのサブタイプを判定し、対応するISkill実装を生成するFactory
    /// 新スキルタイプ追加時はここにcaseを追記するだけでよい
    /// </summary>
    private ISkill CreateSkillFromData(SkillData data)
    {
        if (data == null) return null;
        if (data is AttackSkillData attack)   return new AttackSkill(attack, this);
        if (data is MovementSkillData movement) return new DodgeSkill(movement, this);
        // 将来の拡張: 新スキルタイプをここに追加
        Debug.LogWarning($"[PlayerController] 未対応のSkillDataタイプ: {data.GetType().Name}");
        return null;
    }

    private Vector2Int ConvertInputToDirection(Vector2 input)
    {
        Vector2Int direction = Vector2Int.zero;
        if (Mathf.Abs(input.x) > 0.5f) direction.x = input.x > 0 ? 1 : -1;
        else if (Mathf.Abs(input.y) > 0.5f) direction.y = input.y > 0 ? 1 : -1;
        return direction;
    }

    #endregion

    #region 公開プロパティ

    /// <summary>プレイヤーが移動中かどうか（スキルのCanExecuteで参照）</summary>
    public bool IsMoving => movementController != null && movementController.IsMoving;

    /// <summary>現在のグリッド座標</summary>
    public Vector2Int CurrentGridPosition => movementController != null ? movementController.CurrentGridPosition : Vector2Int.zero;

    /// <summary>現在の向き</summary>
    public Vector2Int FacingDirection => movementController != null ? movementController.FacingDirection : Vector2Int.down;

    #endregion
}

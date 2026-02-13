using UnityEngine;

/// <summary>
/// プレイヤーの全体的な動作を管理するメインコントローラー
/// 移動、攻撃、回避などの各種コンポーネントを統合
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region コンポーネント参照

    private GridMovementController movementController;
    // TODO: 将来的に追加予定
    // private PlayerCombatController combatController;
    // private PlayerDodgeController dodgeController;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 各種コンポーネントの取得
        movementController = GetComponent<GridMovementController>();

        // コンポーネントの存在確認
        if (movementController == null)
        {
            Debug.LogError("[PlayerController] GridMovementControllerが見つかりません。");
        }
    }

    #endregion

    #region 公開メソッド（将来の拡張用）

    // TODO: 攻撃、回避などのパブリックメソッドをここに追加
    // 例:
    // public void PerformAttack() { ... }
    // public void PerformDodge() { ... }

    #endregion
}

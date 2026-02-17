using UnityEngine;

/// <summary>
/// 敵のHP管理コンポーネント（テスト用簡易実装）
/// 本格的な敵システムは別途実装予定
/// </summary>
public class EnemyHealth : GridEntityBase
{
    #region Inspector設定

    public override GridEntityType EntityType => GridEntityType.Enemy;

    [Header("HP設定")]
    [Tooltip("最大HP")]
    [Min(1)]
    [SerializeField] private int maxHealth = 100;
    
    [Header("Grid設定")]
    [Tooltip("グリッド参照（Scene内のGrid）")]
    [SerializeField] private Grid grid;

    #endregion

    #region 内部状態

    private int currentHealth;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // HP初期化
        currentHealth = maxHealth;
        
        // Grid参照チェック
        if (grid == null)
        {
            Debug.LogError("[EnemyHealth] Grid参照がnullです。Inspectorで設定してください。");
            return;
        }
        
        // 現在のワールド座標からグリッド座標を取得
        Vector3Int cellPosition = grid.WorldToCell(transform.position);
        Vector2Int gridPos = new Vector2Int(cellPosition.x, cellPosition.y);
        
        // タイル中央にスナップ（プレイヤーと同じ処理）
        Vector3 centerPosition = grid.CellToWorld(cellPosition) + grid.cellSize / 2f;
        transform.position = centerPosition;
        
        // MapManager登録用の占有グリッド設定
        InitializeSingleGridEntity(gridPos);
    }

    #endregion

    #region Public API

    /// <summary>
    /// ダメージを受ける
    /// </summary>
    /// <param name="damage">受けるダメージ量</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] ダメージ受け取り: {damage} (残りHP: {currentHealth})");

        // HP が 0 以下になったら死亡処理
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        Debug.Log($"[EnemyHealth] 敵が倒された: {gameObject.name}");
        
        // TODO: 将来的には死亡アニメーション、ドロップアイテムなど
        Destroy(gameObject);
    }

    #endregion
}

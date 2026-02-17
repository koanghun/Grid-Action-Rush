using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤー攻撃ロジック管理コントローラー
/// 攻撃入力処理、範囲計算、ダメージ判定、視覚エフェクトを担当
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector設定

    [Header("攻撃スキル設定")]
    [SerializeField] private AttackSkillData attackData;

    #endregion

    #region 依存コンポーネント

    private PlayerController playerController;

    #endregion

    #region クールタイム管理

    private float lastAttackTime = -Mathf.Infinity;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 依存コンポーネント取得
        playerController = GetComponent<PlayerController>();

        // コンポーネント存在確認
        if (playerController == null)
        {
            Debug.LogError("[PlayerAttackController] PlayerControllerが見つかりません。");
        }

        // 攻撃データ確認
        if (attackData == null)
        {
            Debug.LogWarning("[PlayerAttackController] AttackSkillDataが未設定です。");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 攻撃を実行
    /// PlayerControllerから呼び出される
    /// </summary>
    public void PerformAttack()
    {
        // 攻撃データが未設定の場合は無視
        if (attackData == null)
        {
            Debug.LogWarning("[PlayerAttackController] 攻撃データが未設定のため、攻撃できません。");
            return;
        }

        // クールタイム中は無視
        if (Time.time < lastAttackTime + attackData.cooldownDuration)
        {
            Debug.Log("[PlayerAttackController] クールタイム中です。");
            return;
        }

        // 移動中は攻撃不可
        if (playerController.IsMoving)
        {
            Debug.Log("[PlayerAttackController] 移動中は攻撃できません。");
            return;
        }

        // クールタイム更新
        lastAttackTime = Time.time;

        // 攻撃範囲計算
        List<Vector2Int> targetGrids = CalculateAttackRange();

        // 視覚エフェクト表示（非同期）
        ShowAttackEffectAsync(targetGrids).Forget();

        // ダメージ判定
        ApplyDamageToTargets(targetGrids);

        Debug.Log($"[PlayerAttackController] 攻撃実行: {targetGrids.Count}個のグリッドを攻撃");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// プレイヤーの向きを基準に攻撃範囲を計算
    /// </summary>
    /// <returns>攻撃対象グリッド座標リスト</returns>
    private List<Vector2Int> CalculateAttackRange()
    {
        List<Vector2Int> targetGrids = new List<Vector2Int>();

        // 現在の状態を取得
        Vector2Int playerPos = playerController.CurrentGridPosition;
        Vector2Int facingDir = playerController.FacingDirection;

        // 攻撃パターンが未設定の場合は正面1マスのみ
        if (attackData.attackGridPattern == null || attackData.attackGridPattern.Count == 0)
        {
            targetGrids.Add(playerPos + facingDir);
            return targetGrids;
        }

        // 攻撃パターンを向きに応じて回転
        foreach (Vector2Int relativePos in attackData.attackGridPattern)
        {
            Vector2Int rotatedPos = RotateGridByFacingDirection(relativePos, facingDir);
            Vector2Int worldPos = playerPos + rotatedPos;
            targetGrids.Add(worldPos);
        }

        return targetGrids;
    }

    /// <summary>
    /// 相対グリッド座標をプレイヤーの向きに応じて回転
    /// </summary>
    /// <param name="relativePos">相対座標（デフォルトは上向き基準）</param>
    /// <param name="facingDir">プレイヤーの向き</param>
    /// <returns>回転後の相対座標</returns>
    private Vector2Int RotateGridByFacingDirection(Vector2Int relativePos, Vector2Int facingDir)
    {
        // 上向き（0, 1）がデフォルト
        if (facingDir == Vector2Int.up)
        {
            return relativePos;
        }
        // 下向き（0, -1）: 180度回転
        else if (facingDir == Vector2Int.down)
        {
            return new Vector2Int(-relativePos.x, -relativePos.y);
        }
        // 右向き（1, 0）: 90度時計回り
        else if (facingDir == Vector2Int.right)
        {
            return new Vector2Int(-relativePos.y, relativePos.x);
        }
        // 左向き（-1, 0）: 90度反時計回り
        else if (facingDir == Vector2Int.left)
        {
            return new Vector2Int(relativePos.y, -relativePos.x);
        }

        // 想定外の向き
        return relativePos;
    }

    /// <summary>
    /// 攻撃範囲に視覚エフェクトを表示（非同期）
    /// </summary>
    /// <param name="targetGrids">対象グリッドリスト</param>
    private async UniTaskVoid ShowAttackEffectAsync(List<Vector2Int> targetGrids)
    {
        // エフェクトプレハブが未設定の場合はスキップ
        if (attackData.rangeEffectPrefab == null)
        {
            return;
        }

        // PoolManagerが存在しない場合はスキップ
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("[PlayerAttackController] PoolManagerが見つかりません。エフェクトを表示できません。");
            return;
        }

        // 各グリッドにエフェクトを配置
        List<GameObject> effectObjects = new List<GameObject>();
        foreach (Vector2Int gridPos in targetGrids)
        {
            Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, 0);
            GameObject effect = PoolManager.Instance.Get(attackData.rangeEffectPrefab, worldPos, Quaternion.identity);
            effectObjects.Add(effect);
        }

        // 指定時間待機
        await UniTask.Delay((int)(attackData.effectDuration * 1000));

        // エフェクトをプールに返却
        foreach (GameObject effect in effectObjects)
        {
            PoolManager.Instance.Return(attackData.rangeEffectPrefab, effect);
        }
    }

    /// <summary>
    /// 攻撃範囲内の敵にダメージを適用（グリッドベース、MapManager使用）
    /// </summary>
    /// <param name="targetGrids">対象グリッドリスト</param>
    private void ApplyDamageToTargets(List<Vector2Int> targetGrids)
    {
        // MapManagerが存在しない場合はスキップ
        if (MapManager.Instance == null)
        {
            Debug.LogWarning("[PlayerAttackController] MapManagerが見つかりません。ダメージ判定をスキップします。");
            return;
        }

        // 範囲内のエンティティを取得（HashSetで重複除去済み）
        HashSet<IGridEntity> targets = MapManager.Instance.GetEntitiesInArea(targetGrids);

        // 敵とボスにのみダメージを適用
        foreach (IGridEntity entity in targets)
        {
            if (entity.EntityType == GridEntityType.Enemy || 
                entity.EntityType == GridEntityType.Boss)
            {
                EnemyHealth health = entity.GameObjectReference.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(attackData.damage);
                }
            }
        }
    }

    #endregion
}

using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// グリッド攻撃スキル実装
/// PlayerAttackControllerから移行したロジックをSkillBaseに準拠した形で再実装
/// </summary>
public class AttackSkill : SkillBase
{
    #region データ参照

    private readonly AttackSkillData attackData;

    #endregion

    #region コンストラクタ

    public AttackSkill(AttackSkillData data, PlayerController controller) : base(data, controller)
    {
        attackData = data;
    }

    #endregion

    #region SkillBase 実装

    protected override void OnExecute()
    {
        List<Vector2Int> targetGrids = CalculateAttackRange();
        ShowAttackEffectAsync(targetGrids).Forget();
        ApplyDamageToTargets(targetGrids);
        Debug.Log($"[AttackSkill] 攻撃実行: {targetGrids.Count}個のグリッドを攻撃");
    }

    #endregion

    #region 攻撃範囲計算

    private List<Vector2Int> CalculateAttackRange()
    {
        var targetGrids = new List<Vector2Int>();
        Vector2Int pos = playerController.CurrentGridPosition;
        Vector2Int facing = playerController.FacingDirection;

        if (attackData.attackGridPattern == null || attackData.attackGridPattern.Count == 0)
        {
            targetGrids.Add(pos + facing);
            return targetGrids;
        }

        foreach (Vector2Int relative in attackData.attackGridPattern)
        {
            targetGrids.Add(pos + RotateByFacing(relative, facing));
        }
        return targetGrids;
    }

    /// <summary>
    /// 相対座標をプレイヤーの向きに合わせて回転
    /// デフォルトは「上（0,1）」向き基準
    /// </summary>
    private Vector2Int RotateByFacing(Vector2Int rel, Vector2Int facing)
    {
        if (facing == Vector2Int.up) return rel;
        if (facing == Vector2Int.down) return new Vector2Int(-rel.x, -rel.y);
        if (facing == Vector2Int.right) return new Vector2Int(-rel.y, rel.x);
        if (facing == Vector2Int.left) return new Vector2Int(rel.y, -rel.x);
        return rel;
    }

    #endregion

    #region エフェクト・ダメージ処理

    private async UniTaskVoid ShowAttackEffectAsync(List<Vector2Int> targetGrids)
    {
        if (attackData.rangeEffectPrefab == null) return;
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("[AttackSkill] PoolManagerが見つかりません。");
            return;
        }

        var effects = new List<GameObject>();
        foreach (Vector2Int gp in targetGrids)
        {
            Vector3 wp = new Vector3(gp.x, gp.y, 0);
            effects.Add(PoolManager.Instance.Get(attackData.rangeEffectPrefab, wp, Quaternion.identity));
        }

        await UniTask.Delay((int)(attackData.effectDuration * 1000));

        foreach (GameObject e in effects)
            PoolManager.Instance.Return(attackData.rangeEffectPrefab, e);
    }

    private void ApplyDamageToTargets(List<Vector2Int> targetGrids)
    {
        if (MapManager.Instance == null) return;

        HashSet<IGridEntity> targets = MapManager.Instance.GetEntitiesInArea(targetGrids);
        foreach (IGridEntity entity in targets)
        {
            if (entity.EntityType == GridEntityType.Enemy ||
                entity.EntityType == GridEntityType.Boss)
            {
                // 攻撃範囲が重なった場合でも、複数回ダメージを与えないようにする
                entity.GameObjectReference.GetComponent<EnemyHealth>()?.TakeDamage(attackData.damage);
            }
        }
    }

    #endregion
}

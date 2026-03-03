using UnityEngine;

/// <summary>
/// 全スキル共通のベース抽象クラス
/// クールタイム管理ロジックをここに集約し、各スキルの重複実装を排除
/// </summary>
public abstract class SkillBase : ISkill
{
    #region 依存データ

    protected readonly SkillData skillData;
    protected readonly PlayerController playerController;

    #endregion

    #region クールタイム管理

    // -Infinityで初期化することで、初回は必ず実行可能にする
    private float lastExecuteTime = float.NegativeInfinity;

    #endregion

    #region コンストラクタ

    protected SkillBase(SkillData skillData, PlayerController playerController)
    {
        this.skillData = skillData;
        this.playerController = playerController;
    }

    #endregion

    #region ISkill 実装

    /// <summary>
    /// スキル実行可能判定（クールタイム + 移動中チェック）
    /// サブクラスでoverrideして追加条件を設定可能
    /// </summary>
    public virtual bool CanExecute()
    {
        if (skillData == null) return false;
        if (playerController.IsMoving) return false;
        return Time.time >= lastExecuteTime + skillData.cooldownDuration;
    }

    /// <summary>
    /// スキルを実行する。CanExecuteチェック後にOnExecuteを呼び出す
    /// </summary>
    public void Execute()
    {
        if (!CanExecute()) return;
        lastExecuteTime = Time.time;
        OnExecute();
    }

    #endregion

    #region サブクラス実装メソッド

    /// <summary>スキル固有のロジック（サブクラスで実装）</summary>
    protected abstract void OnExecute();

    #endregion
}

/// <summary>
/// 全スキルの共通契約インターフェース
/// このインターフェースに依存することで、PlayerControllerはスキルの実装詳細を知らなくてよい
/// </summary>
public interface ISkill
{
    /// <summary>スキルが実行可能な状態かどうか（クールタイム・状態チェック）</summary>
    bool CanExecute();

    /// <summary>スキルを実行する。内部でCanExecuteを確認する</summary>
    void Execute();
}

# 開発ガイドライン (Development Guidelines)

本プロジェクトは、実務に近い開発フローを想定し、以下のルールに基づいて運用されています。

## 1. ブランチ戦略 (Branch Strategy)
**GitHub Flow** を採用しています。`master` ブランチは常にデプロイ可能な状態（Deployable）を保ちます。

* **`master`**: 
  * プロダクション用ブランチ。
  * **直接プッシュ禁止 (Protected Branch)**。
  * Pull Request (PR) のマージ時、GitHub Actionsを通じてWebGLビルドが自動デプロイされます。
* **`feat/機能名`**: 
  * 新機能開発用ブランチ (例: `feat/player-movement`)。
* **`fix/バグ名`**: 
  * バグ修正用ブランチ (例: `fix/collision-error`)。

## 2. コミット規約 (Commit Convention)
可読性を高めるため、**Conventional Commits** のプレフィックス（接頭辞）を使用し、内容は**日本語**で記述します。

**フォーマット:** `<type>: <内容>`

| Prefix | 説明 (Description) | 例 (Example) |
| :--- | :--- | :--- |
| **feat** | 新機能の追加 | `feat: プレイヤーの移動ロジックを実装` |
| **fix** | バグ修正 | `fix: タイルマップの当たり判定を修正` |
| **docs** | ドキュメントのみの変更 | `docs: ビルド手順書を更新` |
| **style** | コードの動作に影響しない変更 (フォーマット等) | `style: インデント修正` |
| **refactor** | バグ修正や機能追加を含まないコード変更 | `refactor: 経路探索アルゴリズムを整理` |
| **chore** | ビルドプロセスやツールの変更 | `chore: Unityバージョンを2022.3へ更新` |
## 3. Unity設定 & 環境 (Unity Settings)
チーム開発時の競合を防ぐため、以下の設定を必須とします。

* **Version**: Unity 6000.3.8f1 (LTS)
* **Asset Serialization**: `Force Text` (Project Settings > Editor)
* **Meta Files**: `Visible Meta Files`
* **Git LFS**: 画像(*.png, *.psd)や音声(*.wav, *.mp3)などのバイナリファイルはGit LFSで管理します。

## 4. ワークフロー (Workflow)
1. Issueを作成し、タスクを定義する。
2. 作業用ブランチを作成する (`git checkout -b feat/task-name`)。
3. 機能実装後、`master` ブランチへ Pull Request (PR) を作成する。
4. コードレビュー（セルフチェック）完了後、マージする。
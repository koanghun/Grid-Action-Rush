# ⚔️ Grid Action Rush (仮)

![Unity](https://img.shields.io/badge/Unity-6.3LTS-black?style=flat&logo=unity)
![Platform](https://img.shields.io/badge/Platform-WebGL-blue)
![License](https://img.shields.io/badge/License-MIT-green)

> **"グリッド単位の制約の中で、極限のスピードと打撃感を追求した2Dアクションゲーム"**
>
> Unityエンジニアとしての技術力（設計・最適化・シェーダー）を証明するためのポートフォリオプロジェクトです。

<br>

## 🎮 デモプレイ (Play Demo)
ブラウザですぐにプレイ可能です（ダウンロード不要）。
### 👉 **[Play on GitHub Pages](https://[YOUR_GITHUB_ID].github.io/[YOUR_REPO_NAME]/)**

<br>

## 📌 プロジェクト概要 (Overview)
* **ジャンル**: 2D グリッドベース・リアルタイムアクション
* **開発期間**: 2026.02 ~ (進行中)
* **開発環境**: Unity 6.3 LTS(6000.3.8f1), C#, Visual Studio 2026, Antigravity
* **ターゲット**: WebGL (PC Browser)

<br>

## 🛠 技術スタック (Tech Stack)
本プロジェクトでは、**WebGL環境でのパフォーマンス**と**保守性の高いコード設計**を最優先しています。

| Category | Technology | Reason for Selection |
| :--- | :--- | :--- |
| **Async** | **UniTask** | コルーチンのオーバーヘッド回避、可読性の高い非同期処理の実装 |
| **Input** | **New Input System** | イベント駆動型の入力処理、将来的なゲームパッド対応 |
| **Tween** | **DOTween** | 物理演算を使わないスムーズな移動・演出の補間 |
| **Design** | **Observer / Command** | 疎結合な設計 (MVP/MVCパターンの部分適用) |
| **CI/CD** | **GitHub Actions** | ビルドおよびGitHub Pagesへのデプロイ自動化 |

<br>

## 🚀 技術的なこだわり (Key Features & Challenges)

### 1. 物理エンジンに頼らない独自の移動システム
Unityの `Rigidbody2D` や `AddForce` は使用していません。グリッドベースの正確な座標管理を行うため、独自の移動ロジックを実装しました。
* **線形補間 (Lerp)**: スムーズな移動表現。
* **座標計算**: `Vector2Int` ベースで論理座標を管理し、ワールド座標へ変換。

### 2. WebGL最適化 (Memory Management)
ガベージコレクション(GC)によるスパイクを防ぐため、徹底したメモリ管理を行っています。
* **Object Pooling**: 敵キャラクターや弾幕、エフェクトの生成・破棄を回避。
* **Struct vs Class**: 頻繁に生成されるデータ構造には `struct` を採用し、ヒープ割り当てを抑制。

<br>

## 🏗 アーキテクチャ (Architecture)

コードの結合度を下げるため、役割を明確に分離しています。

```mermaid
classDiagram
    class GameManager {
        +GameState State
        +ManageFlow()
    }
    class GridManager {
        +Vector3Int WorldToCell()
        +bool IsWalkable()
    }
    class PlayerController {
        -InputBuffer buffer
        -IMovement movement
        +OnMoveInput()
    }
    class InputSystem {
        <<Interface>>
        +Action OnMove
        +Action OnAttack
    }

    GameManager --> GridManager
    PlayerController ..> InputSystem : Subscribe
    PlayerController --> GridManager : Query Position

# 회피 스킬 시스템 구현 계획

플레이어의 회피 스킬 시스템을 구현합니다. ScriptableObject 기반 스킬 데이터를 사용하여 가변 거리 대시 이동과 유연한 장애물 처리를 실현합니다.

## User Review Required

> [!IMPORTANT]
> **아키텍처 결정 사항**
> - ✅ **ScriptableObject 사용**: 스킬 데이터를 외부 데이터화
> - ✅ **가변 거리 대시**: 고정 2칸이 아닌 N칸 대응
> - ✅ **장애물 타입 분류**: 벽/몬스터/낭떠러지를 구별
> - ✅ **향 시스템**: 4방향(상하좌우) + 향후 8방향 대응

> [!WARNING]
> **향후 확장 계획 (이번에는 미구현)**
> - 대각선 방향 회피 (8방향 대응)
> - 낭떠러지 타일 건너뛰기 처리 (맵 설계 필요)
> - 회피 중 무적 시간 (전투 시스템 통합 후)


## Proposed Changes

### 1. 스킬 데이터 시스템 (상속 구조)

> [!NOTE]
> **설계 패턴**
> - 공통 필드는 추상 베이스 클래스 `SkillData`에 정의
> - 스킬 타입별로 상속하여 전용 필드 추가
> - 향후 버프/디버프 스킬 추가 시에도 쉽게 확장 가능

#### [NEW] [SkillData.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Skills/SkillData.cs)

**추상 베이스 클래스** - 모든 스킬의 공통 필드 정의.

```csharp
/// <summary>
/// 全スキルの基底クラス
/// </summary>
public abstract class SkillData : ScriptableObject
{
    [Header("基本情報")]
    public string skillName;           // スキル名
    public Sprite skillIcon;           // スキルアイコン (UI用)
    
    [Header("クールタイム")]
    public float cooldownDuration;     // クールタイム (秒)
}
```

---

#### [NEW] [MovementSkillData.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Skills/MovementSkillData.cs)

**이동 계열 스킬 전용** - 회피, 대시 등에 사용.

```csharp
/// <summary>
/// 移動系スキルデータ（回避、ダッシュなど）
/// </summary>
public class MovementSkillData : SkillData
{
    [Header("移動設定")]
    public int dashDistance;              // ダッシュ距離 (マス数)
    public float speedMultiplier = 2.0f;  // 移動速度倍率
    
    [Header("障害物処理")]
    public bool canPassWall = false;      // 壁を通過可能か
    public bool canPassMonster = false;   // モンスターを通過可能か
    public bool canPassCliff = false;     // 崖を通過可能か (将来実装)
}
```

**설계 이유**:
- `canPassXXX` 플래그로 스킬별 통과 가능 장애물 제어
- 예: 일반 회피는 모두 false, "순간이동" 스킬은 일부 true

---

#### [NEW] [AttackSkillData.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Skills/AttackSkillData.cs)

**공격 계열 스킬 전용**.

```csharp
/// <summary>
/// 攻撃系スキルデータ
/// </summary>
public class AttackSkillData : SkillData
{
    [Header("攻撃設定")]
    public int damage;                    // ダメージ量
    public AttackRangePattern rangePattern; // 攻撃範囲パターン
    
    // TODO (将来実装): グリッド形状を視覚的にデザイン
    // Unity Editor拡張でグリッドエディタを作成予定
    // public List<Vector2Int> attackGridPattern; 
}

// 攻撃範囲パターンの列挙型 (Phase 1: シンプルなパターンのみ)
public enum AttackRangePattern
{
    Single,      // 単一グリッド (1マス)
    Cross,       // 十字 (上下左右)
    Square3x3,   // 3x3範囲
    Line         // 直線 (向いている方向)
}
```

**향후 확장 계획**:
- Unity Editor 확장으로 그리드 형태를 시각적으로 디자인
- Inspector에서 그리드를 직접 클릭하여 공격 범위 지정
- `List<Vector2Int>`로 커스텀 패턴 저장

---

#### [NEW] [DodgeSkillData.asset](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Data/Skills/DodgeSkillData.asset)

회피 스킬의 데이터 에셋 예시.

**초기 설정값**:
- `skillName`: "긴급회피"
- `dashDistance`: 2
- `speedMultiplier`: 2.0f
- `cooldownDuration`: 1.0f
- `canPassWall`: false
- `canPassMonster`: false
- `canPassCliff`: false (향후 구현)

---

### 2. 그리드 시스템 확장

#### [MODIFY] [GridMovementController.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs)

**추가 기능**:
1. 향 관리 시스템
2. 스킬 기반 대시 이동
3. 경로상 장애물 판정

**신규 필드**:
```csharp
[Header("回避スキル設定")]
[SerializeField] private MovementSkillData dodgeSkill;  // 回避スキルデータ

// 向き管理
private Vector2Int facingDirection = Vector2Int.down;

// スキル状態
private bool isSkillActive = false;
```

**신규 메서드**:

**`UpdateFacingDirection(Vector2Int direction)`**
- 이동 방향에 따라 향을 업데이트
- 향후 8방향 대응 시에도 여기를 확장

**`OnDodgePerformed(InputAction.CallbackContext context)`**
- F키 입력 시 이벤트 핸들러
- 스킬 데이터를 참조하여 대시 실행

**`TryDodge()`**
- 회피 스킬 실행 전처리
- 쿨타임 확인, 향 확인

**`CalculateDodgePath(Vector2Int direction, int distance)`**
- 향 + 거리로부터 대시 경로를 계산
- 장애물 판정을 포함한 경로 탐색
- **반환값**: 실제로 이동 가능한 최종 좌표

**`DodgeAsync(Vector2Int targetPos, float speedMultiplier)`**
- UniTask를 사용한 고속 대시 이동
- `speedMultiplier`로 스킬별 속도 조정

---

### 3. 장애물 판정 시스템

#### [NEW] [ObstacleType.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Map/ObstacleType.cs)

장애물 타입의 열거형.

```csharp
public enum ObstacleType
{
    None,           // 移動可能
    Wall,           // 壁（通過不可）
    Monster,        // モンスター（通過不可）
    Cliff,          // 崖（スキップ可能、将来実装）
    Hazard          // ハザード（ダメージ床など）
}
```

---

#### [MODIFY] [GridMovementController.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs)

**`GetObstacleType(Vector2Int gridPos)`**
- 지정한 그리드 좌표의 장애물 타입을 판정
- 현재는 `Physics2D.OverlapCircle`로 콜라이더 검출
- 향후 Tilemap이나 맵 데이터 참조로 확장

**`CalculateDodgePath(Vector2Int direction, int distance)` 구현 상세**:

```csharp
private Vector2Int CalculateDodgePath(Vector2Int direction, int distance)
{
    Vector2Int currentPos = currentGridPosition;
    
    for (int i = 1; i <= distance; i++)
    {
        Vector2Int nextPos = currentPos + (direction * i);
        ObstacleType obstacle = GetObstacleType(nextPos);
        
        // 壁やモンスターの場合は手前で停止
        if (obstacle == ObstacleType.Wall || obstacle == ObstacleType.Monster)
        {
            return currentPos + (direction * (i - 1)); // 1マス手前に停止
        }
        
        // 崖の場合はスキップ（将来実装）
        // if (obstacle == ObstacleType.Cliff)
        // {
        //     continue; // この座標はスキップして次を確認
        // }
    }
    
    // 障害物がなければ最大距離まで移動
    return currentPos + (direction * distance);
}
```

**장애물 처리의 우선순위**:
1. 벽/몬스터 검출 → 바로 앞 칸에서 정지
2. 낭떠러지 검출 → 건너뛰고 다음 칸 체크 (향후 구현)
3. 장애물 없음 → 최대 거리까지 이동

---

### 4. Input System 통합

#### [MODIFY] [InputSystem_Actions.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions)

**신규 액션**:
- **액션명**: `Dodge`
- **바인딩**: 
  - 키보드: `F`
  - 게임패드: `Button South (A/Cross)`
- **액션 타입**: Button

**구독**:
```csharp
private void OnEnable()
{
    inputActions.Enable();
    inputActions.Player.Move.performed += OnMovePerformed;
    inputActions.Player.Dodge.performed += OnDodgePerformed;  // 追加
}

private void OnDisable()
{
    inputActions.Player.Move.performed -= OnMovePerformed;
    inputActions.Player.Dodge.performed -= OnDodgePerformed;  // 追加
    inputActions.Disable();
}
```

---

### 5. 향후 확장 대응

#### 8방향 이동 대응 (Phase 2에서 구현 예정)

**변경점**:
- `facingDirection`을 `Vector2Int`로 유지 (대각선 대응)
- `UpdateFacingDirection`에서 8방향을 정규화

```csharp
// 例: 右上方向 = (1, 1)
private Vector2Int NormalizeDirection(Vector2 input)
{
    // 8方向に正規化
    Vector2Int dir = Vector2Int.zero;
    if (input.x > 0.5f) dir.x = 1;
    else if (input.x < -0.5f) dir.x = -1;
    
    if (input.y > 0.5f) dir.y = 1;
    else if (input.y < -0.5f) dir.y = -1;
    
    return dir;
}
```

#### 낭떠러지 건너뛰기 처리 (맵 시스템 구현 후)

**전제 조건**:
- Tilemap에 낭떠러지 전용 타일 정의
- 또는 `TileData` ScriptableObject로 낭떠러지 플래그 관리

**구현안**:
```csharp
// CalculateDodgePath 内で崖をスキップ
if (obstacle == ObstacleType.Cliff)
{
    // 崖タイルは移動経路には含めないが通過可能
    continue;
}
```

## Verification Plan

### Automated Tests

Unity Test Framework를 사용한 단위 테스트 (옵션):

```csharp
[Test]
public void CalculateDodgePath_WithWall_StopsBeforeWall()
{
    // 2マス先に壁がある場合、1マス目に停止することを確認
}

[Test]
public void CalculateDodgePath_NoObstacle_MovesFullDistance()
{
    // 障害物がない場合、スキルのdashDistance分移動することを確認
}
```

### Manual Verification

#### 1. ScriptableObject 생성
1. Unity 에디터에서 `Assets/Data/Skills` 폴더 생성
2. 우클릭 > Create > Skills > Skill Data
3. `DodgeSkillData` 이름으로 저장
4. Inspector에서 다음 설정:
   - Skill Name: "긴급회피"
   - Dash Distance: 2
   - Skill Speed: 2.0
   - Cooldown Duration: 1.0

#### 2. Input System 설정
1. `InputSystem_Actions.inputactions` 열기
2. Player 액션 맵에 `Dodge` 액션 추가
3. F키 바인드
4. Generate C# Class 실행

#### 3. 기본 동작 테스트

**테스트1: 기본 대시**
1. Play 모드에서 방향키로 위로 이동
2. F키 누르기
3. 기대 결과: 위 방향으로 빠르게 2칸 이동

**테스트2: 장애물 (벽)**
1. 플레이어 앞 1칸에 벽 배치
2. F키 누르기
3. 기대 결과: 이동 안하고, 향만 업데이트

**테스트3: 장애물 (중간)**
1. 플레이어 앞 2칸에 벽 배치 (1칸은 비어있음)
2. F키 누르기
3. 기대 결과: 1칸까지 이동 (벽 앞에서 정지)

**테스트4: 스킬 데이터 변경**
1. `DodgeSkillData`의 `dashDistance`를 3으로 변경
2. F키 누르기
3. 기대 결과: 3칸 이동 (장애물이 없을 경우)

**테스트5: 속도 확인**
1. 통상 이동 (moveSpeed=5)과 회피 대시 (skillSpeed=2.0) 비교
2. 기대 결과: 회피가 명확히 더 빠름

#### 4. WebGL 빌드 테스트
1. Build Settings > WebGL로 빌드
2. 브라우저에서 실행
3. 모든 기능이 정상 동작하는지 확인
4. Console에서 GC Spike가 없는지 확인

---

## 📚 参考資料

- [ScriptableObject Best Practices](https://unity.com/how-to/architect-game-code-scriptable-objects)
- [UniTask GitHub](https://github.com/Cysharp/UniTask)
- [Tilemap API Reference](https://docs.unity3d.com/Manual/Tilemap.html)

---

**추천 브랜치명**: `feat/dodge-skill-system`

**추천 커밋 예시**:
```
feat: ScriptableObjectベースの回避スキルシステムを実装

- SkillDataクラスを追加（可変距離ダッシュ対応）
- GridMovementControllerに向き管理機能を追加
- 経路上の障害物判定ロジックを実装
- UniTaskによる高速ダッシュアニメーションを実装
- 将来的な8方向/崖スキップ対応の基盤を構築
```

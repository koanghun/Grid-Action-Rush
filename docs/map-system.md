# グリッドベースマップシステム実装計画

## 背景 (Background)

GitHub Issue [#6](https://github.com/koanghun/Grid-Action-Rush/issues/6)에 따라 그리드 기반 맵 시스템을 구축합니다.

현재 `GridMovementController`와 `PlayerSkillController`는 구현되어 있지만, 맵 데이터와 장애물 판정 시스템이 존재하지 않습니다. `PlayerSkillController.CalculateDodgePath()`의 TODO 주석에서 MapManager가 미구현 상태임을 확인했습니다.

```csharp
// TODO: MapManager実装後に変更
// ObstacleType obstacle = mapManager.GetObstacleType(nextPos);
ObstacleType obstacle = ObstacleType.None; // 仮実装：障害物なしとして扱う
```

이번 구현으로 Unity Tilemap 기반 맵 시스템을 구축하고, 통행 가능/불가능 판정 로직을 추가합니다.

---

## User Review Required

> [!IMPORTANT]
> **Unity Editor 수동 작업 필수**
> 
> 이번 구현은 다음 수동 작업이 필요합니다:
> - **Tilemap 셋업**: Grid GameObject, Tilemap Renderer, Tilemap Collider 2D 추가
> - **Tile Palette 생성**: Window > 2D > Tile Palette에서 벽/바닥 타일 등록
> - **TileData ScriptableObject 생성**: Unity Editor에서 Create > TileData
> - **MapManager에 데이터 연결**: Inspector에서 Tilemap 참조와 TileData 리스트 설정
>
> 코드 생성은 AI가 진행하지만, Unity 에셋 생성 및 Scene 셋업은 사용자가 진행해야 합니다.



---

## Proposed Changes

### Core Data Layer

#### [NEW] [TileData.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Map/TileData.cs)

타일 정보를 정의하는 ScriptableObject를 생성합니다.

**주요 속성:**
- `tileName`: 타일 이름 (Inspector 표시용)
- `isWalkable`: 통행 가능 여부
- `obstacleType`: 장애물 타입 (`ObstacleType` enum 사용)
- `moveCostMultiplier`: 이동 비용 배율 (미래 구현, 늪/얼음 등)

**설계 의도:**
- ScriptableObject를 사용하여 데이터와 로직 분리 (SOLID 원칙)
- Unity Editor에서 쉽게 설정 가능 (레벨 디자이너 친화적)
- 메모리 효율적 (같은 타일 타입은 하나의 인스턴스 공유)

---

### Manager Layer

#### [NEW] [MapManager.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Map/MapManager.cs)

맵 전체를 관리하는 MonoBehaviour를 생성합니다.

**주요 책임:**
1. **Tilemap 데이터 로드**: Unity Tilemap에서 그리드 데이터를 읽어 Dictionary로 변환
2. **타일 조회 API 제공**:
   - `GetObstacleType(Vector2Int gridPos)`: 특정 좌표의 장애물 타입 반환
   - `IsWalkable(Vector2Int gridPos)`: 특정 좌표의 통행 가능 여부 반환
3. **디버깅 지원**: Gizmo로 타일 상태 시각화 (선택사항)

**싱글톤 패턴:**
```csharp
public static MapManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
}
```

**설계 이점:**
- 다른 스크립트에서 `MapManager.Instance`로 직접 접근 가능
- `FindObjectOfType` 호출 제거로 성능 향상 (WebGL 최적화)
- Scene에 하나의 MapManager만 존재함을 보장

**데이터 구조:**
```csharp
Dictionary<Vector2Int, TileData> tileDataMap;
```

**WebGL 최적화 고려 사항:**
- Dictionary를 사용하여 O(1) 조회 성능 확보
- GC 압력을 줄이기 위해 Start()에서 한 번만 초기화
- 런타임 중 Instantiate/Destroy 없음

**로드 프로세스:**
1. Inspector에서 설정한 `Tilemap` 컴포넌트 참조
2. Inspector에서 설정한 `TileData[]` 배열 (각 타일 타입별 데이터)
3. Start()에서 Tilemap.cellBounds를 순회하며 Dictionary 구축
4. 각 셀의 Tile을 확인하고 대응하는 TileData를 매핑

---

### Integration with Existing Controllers

#### [MODIFY] [PlayerSkillController.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Player/PlayerSkillController.cs)

회피 스킬의 경로 계산에 MapManager를 통합합니다.

**변경 내용:**
- `Awake()` 메서드: MapManager 싱글톤 접근으로 변경 (TODO 주석 제거)
  ```csharp
  // 변경 전 (TODO 주석)
  // mapManager = FindObjectOfType<MapManager>();
  
  // 변경 후
  private MapManager mapManager => MapManager.Instance;
  ```
- `CalculateDodgePath()` 메서드: 장애물 판정 로직 활성화
  ```csharp
  // 변경 전
  ObstacleType obstacle = ObstacleType.None; // 仮実装
  
  // 변경 후
  ObstacleType obstacle = MapManager.Instance.GetObstacleType(nextPos);
  ```

**동작 시나리오:**
1. 플레이어가 회피 스킬 발동 (Space 키)
2. `CalculateDodgePath()`가 이동 경로 상의 각 타일 체크
3. 벽(`ObstacleType.Wall`)을 만나면 바로 앞에서 정지
4. 몬스터(`ObstacleType.Monster`)도 동일하게 처리

---

#### [MODIFY] [GridMovementController.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs)

일반 이동에 통행 가능 여부 체크를 추가합니다.

**변경 내용:**
- `TryMove()` 메서드: 통행 가능 판정 추가
  ```csharp
  // 변経 전
  // TODO: 将来的にここで衝突判定や移動可能判定を追加
  
  // 변경 후
  if (!MapManager.Instance.IsWalkable(targetGridPos))
  {
      return; // 通行不可の場合は移動キャンセル
  }
  ```

**싱글톤 접근:**
- 필드 선언 불필요 (`MapManager.Instance`로 직접 접근)
- Awake/Start에서 참조 획득 과정 제거

**동작 시나리오:**
1. 플레이어가 방향키로 이동 시도
2. `TryMove()`가 목표 타일의 `IsWalkable()` 체크
3. 통행 불가 타일이면 이동 취소 (아무 일도 일어나지 않음)
4. 통행 가능하면 기존 로직대로 `MoveToGridPositionAsync()` 호출

---

## Verification Plan

### Automated Tests

현재 프로젝트에 유닛 테스트가 존재하지 않으므로, 자동화 테스트는 작성하지 않습니다. Unity Play Mode에서 수동 테스트로 검증합니다.

### Manual Verification

> [!NOTE]
> **사용자가 Unity Editor에서 직접 테스트해야 합니다.**

#### 準備 (Setup)

1. **Tilemap 생성**:
   - Hierarchy에서 마우스 우클릭 > 2D Object > Tilemap > Rectangular
   - Grid GameObject와 Tilemap 자식 GameObject 생성 확인

2. **테스트용 TileData 생성** (Unity Editor):
   - Project 창에서 마우스 우클릭 > Create > TileData
   - 다음 3개 생성:
     - `FloorTile`: isWalkable = true, obstacleType = None
     - `WallTile`: isWalkable = false, obstacleType = Wall
     - `CliffTile`: isWalkable = false, obstacleType = Cliff

3. **Scene 셋업**:
   - Player GameObject에 `GridMovementController`, `PlayerController`, `PlayerSkillController` 컴포넌트 부착 확인
   - 빈 GameObject 생성하여 `MapManager` 컴포넌트 추가
   - MapManager Inspector:
     - Tilemap 필드에 Tilemap 할당
     - Tile Data List에 FloorTile, WallTile, CliffTile 추가

4. **타일 배치**:
   - Window > 2D > Tile Palette 열기
   - 벽 타일과 바닥 타일을 Tilemap에 배치 (예: 벽으로 둘러싸인 방)

#### テストケース (Test Cases)

**TC-1: 벽 타일 이동 차단**
1. Play Mode 실행
2. 플레이어를 벽 타일 앞에 위치시킴
3. 벽 방향으로 방향키 입력
4. **예상 결과**: 플레이어가 움직이지 않아야 함

**TC-2: 바닥 타일 정상 이동**
1. Play Mode 실행
2. 플레이어를 바닥 타일 위에 위치시킴
3. 인접한 바닥 타일 방향으로 방향키 입력
4. **예상 결과**: 플레이어가 정상적으로 이동해야 함

**TC-3: 회피 스킬 벽 충돌**
1. Play Mode 실행
2. 플레이어를 벽 앞 2칸 거리에 위치시킴
3. 벽 방향을 바라보게 하고 Space 키로 회피 스킬 발동
4. **예상 결과**: 플레이어가 벽 바로 앞 타일에서 정지해야 함 (벽을 통과하지 않음)

**TC-4: 회피 스킬 정상 경로**
1. Play Mode 실행
2. 플레이어를 바닥 타일 위에 위치시킴
3. 장애물 없는 방향으로 회피 스킬 발동
4. **예상 결과**: 플레이어가 최대 거리(dodgeSkillData.dashDistance)만큼 이동해야 함

**TC-5: Gizmo 시각화** (선택사항)
1. Scene View에서 MapManager가 있는 GameObject 선택
2. **예상 결과**: 
   - 통행 가능 타일에 녹색 테두리 표시
   - 통행 불가 타일에 빨간색 테두리 표시

---

## Implementation Notes

### 아키텍처 패턴

- **ScriptableObject 데이터 주도 설계**: 코드 변경 없이 타일 속성 수정 가능
- **Facade 패턴**: PlayerController가 GridMovementController와 PlayerSkillController를 wrapping
- **Single Responsibility Principle**: MapManager는 맵 데이터만 관리, 이동 로직은 Controller가 담당

### 확장성 고려

미래 Phase 2 구현 시:
- **이동 비용 배율**: `TileData.moveCostMultiplier`를 활용한 늪/얼음 타일
- **동적 장애물**: 몬스터 이동 시 `MapManager.UpdateObstacle()` API 추가
- **에디터 확장**: `[MenuItem("Tools/Map/Generate Map Data")]`로 Tilemap 자동 분석

### WebGL 최적화 체크리스트

- ✅ Dictionary 사용으로 O(1) 타일 조회
- ✅ Start()에서 한 번만 데이터 로드 (런타임 GC 없음)
- ✅ Object Pooling 불필요 (타일은 정적 데이터)
- ✅ Threading 사용 안 함 (WebGL Single Thread)

---

## 제안 커밋 메시지

```
feat: グリッドベースマップシステムの実装

- TileDataのScriptableObject作成
- MapManager MonoBehaviourで障害物判定APIを提供
- PlayerSkillControllerにMapManager統合
- GridMovementControllerに通行可能判定を追加

WebGL最適化のためDictionaryベースのタイル管理を採用。
Unity Tilemapとの連携でレベルデザインワークフローを確立。

Resolves #6
```

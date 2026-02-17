# 그리드 엔티티 시스템 (Grid Entity System)

MapManager에 통합된 그리드 기반 엔티티 추적 시스템입니다.

## 개요
Physics2D 대신 MapManager가 직접 각 그리드에 위치한 엔티티(플레이어, 몬스터, 보스 등)를 관리합니다. 이를 통해 정확한 그리드 판정과 대형 몬스터 처리를 효율적으로 수행합니다.

## 핵심 컴포넌트

### 1. IGridEntity 인터페이스
모든 그리드 객체는 이 인터페이스를 구현해야 합니다.

```csharp
public interface IGridEntity
{
    GridEntityType EntityType { get; }
    List<Vector2Int> OccupiedGrids { get; } // 현재 점유 중인 좌표들
    GameObject GameObjectReference { get; }
}
```

### 2. MapManager
전체 그리드 상태를 관리하는 싱글톤입니다. **Lazy Initialization** 패턴을 사용하여 안전성을 확보했습니다.

- **접근 방식**: `MapManager.Instance` 호출 시 씬 내 객체를 찾거나(`FindAnyObjectByType`), 없으면 자동으로 생성함.
- **데이터 구조**: `Dictionary<Vector2Int, List<IGridEntity>>`
- **주요 기능**:
  - `RegisterEntity`: 엔티티 생성 시 등록
  - `UnregisterEntity`: 엔티티 파괴 시 제거
  - `UpdateEntityPosition`: 엔티티 이동 시 위치 업데이트
  - `GetEntitiesInArea`: 공격 범위 내 엔티티 검색

## 구현 상세

### 엔티티 생명주기 및 초기화

`GridEntityBase`를 상속받은 엔티티(예: `EnemyHealth`)는 다음과 같은 순서로 초기화됩니다.

1.  **Awake**:
    *   현재 Transform 위치를 기반으로 초기 그리드 좌표 계산.
    *   `grid.CellToWorld`를 사용하여 **Transform을 그리드 중앙으로 스냅(Snap)**.
    *   `OccupiedGrids` 리스트 초기화.
2.  **OnEnable**:
    *   `MapManager.Instance.RegisterEntity(this)` 자동 호출.
    *   오브젝트 풀링 등으로 재활성화될 때마다 실행됨.
3.  **OnDisable**:
    *   `MapManager.Instance.UnregisterEntity(this)` 자동 호출.

> [!NOTE]
> **대형 몬스터 처리**
> 2x2 크기 보스의 경우 4개의 그리드 좌표 모두에 동일한 `IGridEntity` 참조가 등록됩니다.

### 위치 업데이트 로직
엔티티 이동 시 `UpdateEntityPosition`을 호출합니다.

**알고리즘: 전체 제거 후 재등록 (Simple & Safe)**
1. `entity.OccupiedGrids`를 순회하며 기존 좌표 딕셔너리에서 엔티티 제거.
2. 새로운 좌표 리스트(`newOccupiedGrids`)를 순회하며 딕셔너리에 엔티티 등록.
3. `entity.OccupiedGrids`를 새로운 좌표로 갱신.

> [!NOTE]
> **대형 몬스터 처리**
> 2x2 크기 보스의 경우 4개의 그리드 좌표 모두에 동일한 `IGridEntity` 참조가 등록됩니다.

### 공격 판정 로직
공격 시 중복 데미지를 방지하기 위해 `HashSet`을 사용합니다.

```csharp
// 공격 범위 내 모든 그리드 조회
List<Vector2Int> attackArea = ...;
HashSet<IGridEntity> targets = MapManager.Instance.GetEntitiesInArea(attackArea);

// HashSet으로 중복 제거된 타겟에게만 데미지 적용
foreach (var target in targets) {
    target.ApplyDamage(...);
}
```

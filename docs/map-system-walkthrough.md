# グリッドベースマップシステム実装 - Walkthrough

## 구현 완료 항목

### ✅ Phase 1: 기반 시스템 구축 완료

#### 1. TileData.cs ScriptableObject 작성
**파일 위치**: `Assets/Scripts/Map/TileData.cs`

**주요 기능**:
- 타일 속성 정의 (이름, 통행 가능 여부, 장애물 타입, 이동 비용 배율)
- `[CreateAssetMenu]`로 Unity Editor에서 쉽게 생성 가능
- Read-only 프로퍼티로 데이터 무결성 보장
- `OnValidate()`로 Inspector에서 이름 자동 동기화

**설계 강점**:
- ScriptableObject 사용으로 데이터와 로직 분리 (SOLID 원칙)
- 메모리 효율적 (같은 타일 타입은 하나의 인스턴스 공유)

---

#### 2. MapManager.cs MonoBehaviour 작성
**파일 위치**: `Assets/Scripts/Map/MapManager.cs`

**주요 기능**:
- 싱글톤 패턴 (`MapManager.Instance`로 전역 접근)
- Tilemap에서 그리드 데이터를 읽어 Dictionary로 변환 (O(1) 조회)
- 공개 API:
  - `GetObstacleType(Vector2Int)`: 장애물 타입 반환
  - `IsWalkable(Vector2Int)`: 통행 가능 여부 반환
  - `GetTileData(Vector2Int)`: TileData 직접 반환
- Gizmo로 타일 상태 시각화 (녹색=통행 가능, 빨간색=통행 불가)

**WebGL 최적화**:
- Dictionary 사용으로 O(1) 타일 조회
- Start()에서 한 번만 데이터 로드 (런타임 GC 없음)
- FindObjectOfType 제거 (싱글톤 패턴)

---

### ✅ Phase 3: 기존 시스템 통합 완료

#### 3. PlayerSkillController.cs 수정
**변경 내용**:
- TODO 주석 제거
- `CalculateDodgePath()`에서 `MapManager.Instance.GetObstacleType()` 호출
- Null 체크로 MapManager 미존재 시 안전 처리

**동작**:
- 회피 스킬 사용 시 경로 상의 벽/몬스터 감지
- 장애물 발견 시 바로 앞에서 정지

---

#### 4. GridMovementController.cs 수정
**변경 내용**:
- `TryMove()`에서 `MapManager.Instance.IsWalkable()` 호출
- 통행 불가 타일로 이동 시도 시 이동 취소

**동작**:
- 플레이어가 방향키로 벽 타일로 이동 시도 시 무시
- 통행 가능한 타일로만 이동 허용

---

## 🛠️ Unity Editor 수동 작업 가이드

> [!IMPORTANT]
> **코드 구현은 완료되었습니다!**
> 
> 이제 Unity Editor에서 다음 작업을 진행해주세요:

### Step 1: 스크립트 파일 생성

Unity Editor의 Project 창에서 다음 절차를 따라주세요:

1. `Assets/Scripts/Map` 폴더에서 마우스 우클릭
2. **Create > C# Script** 선택
3. 파일 이름 입력:
   - `TileData.cs`
   - `MapManager.cs`
4. 각 파일을 열고 위에서 제공한 코드를 복사하여 붙여넣기
5. 저장 (Ctrl+S)

---

### Step 2: Tilemap 셋업

#### 2-1. Grid 및 Tilemap GameObject 생성

1. Hierarchy에서 마우스 우클릭
2. **2D Object > Tilemap > Rectangular** 선택
3. 자동으로 다음이 생성됨:
   - `Grid` (부모 GameObject)
   - `Tilemap` (자식 GameObject, Tilemap Renderer 포함)

#### 2-2. Tilemap Collider 추가 (선택사항)

1. Tilemap GameObject 선택
2. Inspector 하단의 **Add Component** 클릭
3. **Tilemap Collider 2D** 검색 후 추가

---

### Step 3: TileData ScriptableObject 생성

#### 3-1. 테스트용 타일 데이터 생성

Project 창에서 `Assets/ScriptableObjects/Tiles` 폴더 생성 (권장):

1. **FloorTile** 생성:
   - 마우스 우클릭 > Create > Map > TileData
   - Inspector에서 설정:
     - Tile Name: `Floor`
     - Is Walkable: ✅ (체크)
     - Obstacle Type: `None`

2. **WallTile** 생성:
   - 마우스 우클릭 > Create > Map > TileData
   - Inspector에서 설정:
     - Tile Name: `Wall`
     - Is Walkable: ❌ (언체크)
     - Obstacle Type: `Wall`

3. **CliffTile** 생성 (선택사항):
   - 마우스 우클릭 > Create > Map > TileData
   - Inspector에서 설정:
     - Tile Name: `Cliff`
     - Is Walkable: ❌ (언체크)
     - Obstacle Type: `Cliff`

---

### Step 4: Tile Palette 셋업

#### 4-1. Tile Palette 창 열기

1. 상단 메뉴: **Window > 2D > Tile Palette**
2. **Create New Palette** 클릭
3. 이름: `MapPalette` (권장)

#### 4-2. 타일 에셋 생성

1. Project 창에서 `Assets/Tiles` 폴더 생성
2. 마우스 우클릭 > **Create > 2D > Tiles > Tile**
3. 다음 타일 생성:
   - `FloorTile_Render` (Sprite에 바닥 스프라이트 할당)
   - `WallTile_Render` (Sprite에 벽 스프라이트 할당)
4. Tile Palette 창에 드래그하여 등록

> [!NOTE]
> 스프라이트가 없다면 임시로 Unity 기본 Sprite (예: Square)를 사용하거나, 색상이 다른 임시 이미지를 생성하세요.

---

### Step 5: MapManager 셋업

#### 5-1. MapManager GameObject 생성

1. Hierarchy에서 마우스 우클릭 > **Create Empty**
2. 이름: `MapManager`
3. **Add Component** > `MapManager` 스크립트 추가

#### 5-2. Inspector 설정

MapManager 컴포넌트의 Inspector에서:

1. **Tilemap** 필드:
   - Hierarchy의 `Tilemap` GameObject를 드래그하여 할당

2. **Tile Mappings** 리스트:
   - Size: `2` (또는 생성한 타일 개수)
   - Element 0:
     - Tile: `FloorTile_Render` (Tile Palette 타일)
     - Tile Data: `FloorTile` (ScriptableObject)
   - Element 1:
     - Tile: `WallTile_Render`
     - Tile Data: `WallTile`

---

### Step 6: 테스트 맵 제작

1. Tile Palette 창에서 `WallTile_Render` 선택
2. Scene View에서 Tilemap 위에 벽 패턴 그리기 (예: 사각형 방)
3. `FloorTile_Render` 선택하여 바닥 채우기

**추천 테스트 패턴**:
```
W W W W W
W F F F W
W F P F W  (P = Player 시작 위치)
W F F F W
W W W W W
```

---

## ✅ 검증 체크리스트

Play Mode에서 다음 항목을 확인해주세요:

### TC-1: 벽 타일 이동 차단
- [ ] 플레이어를 벽 타일 앞에 위치
- [ ] 벽 방향으로 방향키 입력
- [ ] **예상 결과**: 플레이어가 움직이지 않음

### TC-2: 바닥 타일 정상 이동
- [ ] 플레이어를 바닥 타일 위에 위치
- [ ] 인접한 바닥 타일 방향으로 방향키 입력
- [ ] **예상 결과**: 플레이어가 정상적으로 이동

### TC-3: 회피 스킬 벽 충돌
- [ ] 플레이어를 벽 앞 2칸 거리에 위치
- [ ] 벽 방향을 바라보게 하고 Space 키로 회피 스킬 발동
- [ ] **예상 결과**: 플레이어가 벽 바로 앞 타일에서 정지 (벽 통과 안 함)

### TC-4: Gizmo 시각화 (Scene View)
- [ ] Scene View에서 MapManager GameObject 선택
- [ ] **예상 결과**:
  - 통행 가능 타일에 녹색 테두리 표시
  - 통행 불가 타일에 빨간색 테두리 표시

---

## 커밋 메시지 제안

```bash
feat: グリッドベースマップシステムの実装

- TileData ScriptableObjectで タイル属性を定義
- MapManager シングルトンで障害物判定APIを提供
- PlayerSkillControllerにMapManager統合（回避スキルの壁判定）
- GridMovementControllerに通行可能判定を追加

WebGL最適化のためDictionaryベースのタイル管理を採用。
Unity Tilemapとの連携でレベルデザインワークフローを確立。

Resolves #6
```

---

## 다음 단계 (Phase 2 - 미래 구현)

- [ ] 에디터 확장: Tilemap → MapData 자동 생성 (`[MenuItem]`)
- [ ] 이동 비용 배율 시스템 (늪, 얼음 등)
- [ ] 동적 장애물 업데이트 (몬스터 이동 시 `UpdateObstacle()` API)
- [ ] Rule Tile을 사용한 자동 타일링

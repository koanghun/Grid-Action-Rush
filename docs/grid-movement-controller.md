# 플레이어 이동 컨트롤러 구현 계획

타일 단위 그리드 이동 시스템을 가진 플레이어 컨트롤러를 구현합니다.

## User Review Required

> [!IMPORTANT]
> **구현 방식 확정**
> - ✅ **UniTask** 사용 (패키지 설치 필요)
> - ✅ **Vector3.Lerp** 사용 (수동 보간)
> - ✅ **코드 주석**: 일본어로 작성

> [!NOTE]
> **UniTask 패키지 설치**
> - UniTask는 Unity Package Manager를 통해 Git URL로 설치합니다
> - URL: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

> [!WARNING]
> **Grid 컴포넌트 참조**
> - 구현에서는 씬에 Tilemap Grid가 설정되어 있다고 가정합니다.
> - Inspector에서 Grid 컴포넌트를 할당하거나, GameObject.Find로 찾을 수 있습니다.

## Proposed Changes

> [!NOTE]
> **Unity 워크플로우**
> - 아래 스크립트 파일들은 **사용자가 Unity 에디터에서 직접 생성**합니다
> - 이 문서에서는 각 파일의 **코드 내용만** 제공합니다
> - 메타 파일 동기화를 위해 Unity의 "Create > C# Script" 기능 사용 권장

### 1. 폴더 구조

Unity 에디터에서 다음 폴더 구조를 생성하세요:

```
Assets/
└── Scripts/
    └── Player/
        ├── GridMovementController.cs
        └── PlayerController.cs
```

---

### 2. Player Controller Component

#### GridMovementController.cs

**핵심 기능**:
- **타일 단위 이동**: `Vector2Int` 기반 그리드 좌표 시스템
- **입력 버퍼링**: 이동 중 마지막 입력 1개만 저장 (선입력 대응)
- **부드러운 이동**: UniTask + Vector3.Lerp를 사용한 시각적 보간
- **이동 상태 관리**: `isMoving` 플래그로 중복 이동 방지

**주요 컴포넌트**:
```csharp
public class GridMovementController : MonoBehaviour
{
    // Inspector 設定
    [SerializeField] private Grid grid;
    [SerializeField] private float moveSpeed = 5f;
    
    // Input System
    private InputSystem_Actions inputActions;
    
    // 移動状態
    private Vector2Int currentGridPosition;
    private bool isMoving = false;
    private Vector2Int? bufferedInput = null; // 移動中の先行入力（最後の1つのみ）
    private CancellationTokenSource moveCts;
}
```

**입력 처리 로직**:
- [Update()](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/NewMonoBehaviourScript.cs#11-16)에서 Input System의 Move 액션 구독
- **방향키** 입력을 Vector2Int로 변환 (WASD는 스킬/공격용으로 사용)
- 이동 중이면 `bufferedInput`에 마지막 입력만 저장, 아니면 즉시 이동 시작
- 이동 완료 시 `bufferedInput`이 있으면 즉시 다음 이동 시작

**이동 UniTask**:
```csharp
// UniTaskを使用した非同期移動処理
private async UniTask MoveToGridPositionAsync(Vector2Int targetGridPos, CancellationToken ct)
{
    isMoving = true;
    
    Vector3 startPos = transform.position;
    Vector3 targetPos = grid.CellToWorld((Vector3Int)targetGridPos) + grid.cellSize / 2;
    
    float elapsed = 0f;
    float duration = 1f / moveSpeed;
    
    while (elapsed < duration)
    {
        ct.ThrowIfCancellationRequested(); // キャンセル対応
        
        transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
        elapsed += Time.deltaTime;
        await UniTask.Yield(PlayerLoopTiming.Update);
    }
    
    transform.position = targetPos;
    currentGridPosition = targetGridPos;
    isMoving = false;
    
    // バッファに先行入力があれば次の移動を開始
    if (bufferedInput.HasValue)
    {
        Vector2Int nextInput = bufferedInput.Value;
        bufferedInput = null;
        _ = MoveToGridPositionAsync(nextInput, this.GetCancellationTokenOnDestroy());
    }
}
```

---

### 3. Supporting Scripts

#### PlayerController.cs

플레이어의 전반적인 행동을 관리하는 메인 컨트롤러 (향후 확장용).
- `GridMovementController` 참조
- 공격, 회피 등 다른 액션 컴포넌트 통합 (향후 추가 예정)

**간단한 구조**:
```csharp
public class PlayerController : MonoBehaviour
{
    private GridMovementController movementController;
    
    private void Awake()
    {
        movementController = GetComponent<GridMovementController>();
    }
}
```

---

### 4. Input System Integration

현재 프로젝트의 [InputSystem_Actions.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions)에 이미 Move 액션이 정의되어 있습니다:
- **방향키** 바인딩만 사용 (WASD는 스킬/공격용)
- Gamepad 지원 ✓

> [!NOTE]
> **키 배치**
> - **이동**: 방향키 (↑↓←→)
> - **스킬**: Q, W, E, R
> - **일반 공격**: A
> - **회피**: F

**중요**: Input Actions 파일에서 "Generate C# Class" 옵션을 활성화해야 합니다. (검증 단계에서 진행)

## Verification Plan

### Automated Tests

현재 프로젝트에 테스트 프레임워크가 설정되어 있지만, Unity Play Mode 테스트는 씬 설정이 필요하므로 이 단계에서는 자동화된 테스트를 작성하지 않습니다.

### Manual Verification

사용자가 Unity 에디터에서 직접 테스트해야 합니다:

#### 1. **Unity에서 스크립트 파일 생성**
   1. Unity 에디터 Project 창에서 `Assets` 폴더에 `Scripts` 폴더 생성
   2. `Scripts` 폴더 안에 `Player` 폴더 생성
   3. `Player` 폴더에서 마우스 우클릭 > Create > C# Script
   4. 파일명: `GridMovementController` (확장자 없이)
   5. 다시 마우스 우클릭 > Create > C# Script
   6. 파일명: `PlayerController`
   7. 각 파일을 열어서 이 문서의 코드 내용을 복사/붙여넣기

#### 2. **Input System 설정**
   1. [InputSystem_Actions.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions) 파일 선택
   2. Inspector에서 "Generate C# Class" 체크박스 활성화
   3. Apply 버튼 클릭
   4. Unity가 `InputSystem_Actions.cs` 파일을 생성할 때까지 대기

#### 5. **씬 설정**
   1. 새 씬을 생성하거나 기존 씬을 엽니다
   2. GameObject > 2D Object > Tilemap > Rectangular 생성
   3. Tilemap에 타일을 배치합니다 (테스트용으로 몇 개면 충분)
   4. GameObject > 2D Object > Sprites > Square로 플레이어 오브젝트 생성
   5. 플레이어 오브젝트에 `GridMovementController` 컴포넌트 추가
   6. Inspector에서 Grid 컴포넌트를 할당 (Tilemap의 부모에 있는 Grid)

#### 6. **기능 테스트**
   플레이 모드에서 다음 동작을 확인합니다:
   
   **테스트 1: 기본 이동**
   - WASD 또는 방향키로 이동
   - 예상 결과: 플레이어가 타일 단위로 부드럽게 이동
   
   **테스트 2: 입력 버퍼링(폐지)**
   - 플레이어가 이동 중일 때 다른 방향키를 빠르게 누름
   - 예상 결과: 현재 이동이 끝나면 즉시 다음 이동이 시작됨
   
   **테스트 3: 연속 입력(폐지)**
   - 방향키를 여러 번 빠르게 누름 (3번 이상)
   - 예상 결과: 버퍼 크기(기본값 3)만큼만 저장되고, 나머지는 무시됨
   
   **테스트 4: 그리드 정렬**
   - Scene 뷰에서 플레이어의 위치를 확인
   - 예상 결과: 이동 후 항상 타일 중앙에 정확히 위치함

#### 7. **디버깅**
   - Console 창에서 에러 메시지 확인
   - 이동 속도가 너무 빠르거나 느리면 Inspector에서 `Move Speed` 값 조정 (기본값: 5)

---

### 예상 이슈 및 해결 방법

**이슈 1**: Grid가 null인 경우
- **해결**: Inspector에서 Grid 컴포넌트가 할당되었는지 확인

**이슈 2**: InputSystem_Actions를 찾을 수 없음
- **해결**: Input Actions 파일에서 "Generate C# Class" 옵션이 활성화되었는지 확인

**이슈 3**: 플레이어가 그리드 중앙에 정렬되지 않음
- **해결**: Grid의 Cell Size 설정 확인 (기본값: 1x1)

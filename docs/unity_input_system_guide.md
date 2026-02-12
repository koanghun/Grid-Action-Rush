# Unity Input System 완전 가이드

## 개요

**Unity Input System**은 구 Input Manager (`Input.GetKey()`)를 대체하는 새로운 입력 시스템입니다.

### 구 Input Manager vs 신 Input System

| 항목 | 구 Input Manager | 신 Input System |
|------|-----------------|------------------|
| 코드 예시 | `Input.GetKey(KeyCode.W)` | `inputActions.Player.Move.ReadValue<Vector2>()` |
| 설정 방법 | Project Settings > Input Manager | [.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions) 파일 |
| 디바이스 대응 | 기본적인 키보드/마우스 | 멀티 디바이스 (Gamepad, Touch, XR 등) |
| 리바인딩 | 코드 수정 필요 | 런타임에서 동적 변경 가능 |
| WebGL 대응 | 문제 있음 | 최적화 완료 |

---

## InputSystem_Actions 클래스란?

### 자동 생성되는 C# 래퍼

[.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions) 파일에서 **자동 생성**되는 C# 클래스로, 입력 액션에 타입 안전하게 접근할 수 있습니다.

```
InputSystem_Actions.inputactions (JSON 설정 파일)
        ↓ Unity가 자동 생성
InputSystem_Actions.cs (C# 클래스)
```

### 생성 절차

1. [InputSystem_Actions.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions) 파일 선택
2. Inspector에서 **"Generate C# Class"** 체크
3. **Apply** 버튼 클릭
4. `InputSystem_Actions.cs`가 자동 생성됨

> [!WARNING]
> **자동 생성된 파일은 직접 수정하지 마세요**
> [.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions) 파일을 변경하면 `InputSystem_Actions.cs`가 재생성됩니다.

---

## 기본 사용법

### 1. 인스턴스화와 활성화

```csharp
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        // 인스턴스 생성
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        // 활성화 (입력 수신 시작)
        inputActions.Enable();
    }

    private void OnDisable()
    {
        // 비활성화 (입력 수신 중지)
        inputActions.Disable();
    }
}
```

### 2. 입력 읽기

#### 방법 1: ReadValue (폴링 방식) - 연속 입력용

**사용 시나리오**: 이동, 조준 등 **연속적인 값**을 읽을 때

```csharp
private void Update()
{
    // ✅ 이동: 매 프레임 연속으로 읽어야 함
    Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();
    
    // ✅ 조준: 매 프레임 현재 값 필요
    Vector2 look = inputActions.Player.Look.ReadValue<Vector2>();
}
```

**장점**: 매 프레임 최신 값 확인, 구현이 간단
**단점**: 값이 없어도 매 프레임 체크

#### 방법 2: 이벤트 방식 (권장) - 버튼 입력용

**사용 시나리오**: 공격, 점프 등 **순간적인 동작**을 감지할 때

```csharp
private void OnEnable()
{
    inputActions.Enable();
    
    // ✅ 공격: "눌린 순간" 한 번만
    inputActions.Player.Attack.performed += OnAttackPerformed;
    
    // ✅ 점프: "눌린 순간" 한 번만
    inputActions.Player.Jump.performed += OnJumpPerformed;
}

private void OnDisable()
{
    inputActions.Player.Attack.performed -= OnAttackPerformed;
    inputActions.Player.Jump.performed -= OnJumpPerformed;
    inputActions.Disable();
}

private void OnAttackPerformed(InputAction.CallbackContext context)
{
    Debug.Log("공격!");
}
```

**장점**: 입력 순간만 반응, Update() 부하 없음
**단점**: 구독/해제 관리 필요

#### 📋 선택 가이드

| 입력 종류 | 방식 | 이유 |
|----------|------|------|
| 이동 (WASD/스틱) | **폴링** | 연속적인 Vector2 값 |
| 조준 (마우스/스틱) | **폴링** | 연속적인 Vector2 값 |
| 공격 (버튼) | **이벤트** | 순간적인 동작 |
| 점프 (버튼) | **이벤트** | 순간적인 동작 |
| 대시 (버튼) | **이벤트** | 순간적인 동작 |
```

---

## Action Maps 구조

### 본 프로젝트 구성

```
InputSystem_Actions
├── Player (게임플레이 중)
│   ├── Move          // Vector2 - 이동
│   ├── Look          // Vector2 - 시점
│   ├── Attack        // Button  - 공격
│   ├── Jump          // Button  - 점프
│   ├── Sprint        // Button  - 대시
│   ├── Crouch        // Button  - 웅크리기
│   └── Interact      // Button  - 상호작용
└── UI (메뉴 중)
    ├── Navigate      // Vector2 - UI 조작
    ├── Submit        // Button  - 확인
    ├── Cancel        // Button  - 취소
    └── Click         // Button  - 클릭
```

### Action Map 전환

```csharp
// 게임플레이 중
inputActions.Player.Enable();
inputActions.UI.Disable();

// 메뉴 표시 시
inputActions.Player.Disable();
inputActions.UI.Enable();
```

---

## 키 바인딩 확인

### 현재 설정 (본 프로젝트)

#### 이동 (Player.Move)
- **방향키**: ↑↓←→
- **WASD**: **미사용** (스킬/공격용으로 예약)
- **Gamepad**: 왼쪽 스틱

#### 공격 (Player.Attack)
- **키보드**: A, Enter
- **마우스**: 좌클릭
- **Gamepad**: West 버튼 (□)

#### 점프 (Player.Jump)
- **키보드**: Space
- **Gamepad**: South 버튼 (×)

---

## 자주 발생하는 문제와 해결

### 1. `InputSystem_Actions`를 찾을 수 없음

**원인**: C# 클래스가 생성되지 않음

**해결**:
1. `.inputactions` 파일 선택
2. Inspector에서 "Generate C# Class" 체크
3. Apply 클릭

### 2. 입력이 반응하지 않음

**체크리스트**:
```csharp
// ✓ 인스턴스화되었는가?
inputActions = new InputSystem_Actions();

// ✓ 활성화되었는가?
inputActions.Enable();

// ✓ 올바른 Action Map인가?
inputActions.Player.Move  // OK
inputActions.UI.Move      // NG: UI에는 Move가 없음
```

### 3. WebGL에서 동작하지 않음

**원인**: 구 Input Manager와의 충돌

**해결**: Project Settings > Player > Active Input Handling을 **"Input System Package (New)"**로 설정

### 4. Modification time 에러

패턴:
```
Import Error Code:(4)
Message: Build asset version error
```

**해결**: Unity 재시작 (대부분의 경우 해결됨)

---

## 구 코드에서 마이그레이션 예제

### Before (구 Input Manager)

```csharp
private void Update()
{
    // 이동
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    Vector2 move = new Vector2(h, v);
    
    // 점프
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Jump();
    }
    
    // 공격
    if (Input.GetMouseButtonDown(0))
    {
        Attack();
    }
}
```

### After (신 Input System)

```csharp
private InputSystem_Actions inputActions;

private void Awake()
{
    inputActions = new InputSystem_Actions();
}

private void OnEnable()
{
    inputActions.Enable();
    inputActions.Player.Jump.performed += _ => Jump();
    inputActions.Player.Attack.performed += _ => Attack();
}

private void Update()
{
    Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();
}
```

**장점**:
- 코드가 간결함
- 디바이스 전환이 자동
- 리바인딩이 쉬움

---

## 본 프로젝트에서의 실제 구현

### GridMovementController.cs - 폴링 방식 사용

**왜 이동에는 폴링을 사용했나?**

이동 입력은 **연속적인 Vector2 값**이므로 이벤트보다 폴링이 적합합니다.

```csharp
public class GridMovementController : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        // 1. 인스턴스화
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        // 2. 활성화
        inputActions.Enable();
    }

    private void Update()
    {
        // 3. 입력 읽기 (방향키만)
        // 매 프레임 현재 입력 상태를 확인
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        
        // 디지털화 (-1, 0, 1만)
        if (Mathf.Abs(input.x) > 0.5f)
        {
            direction.x = input.x > 0 ? 1 : -1;
        }
        else if (Mathf.Abs(input.y) > 0.5f)
        {
            direction.y = input.y > 0 ? 1 : -1;
        }
    }
}
```

### 향후 확장: 공격/회피는 이벤트 방식

```csharp
// 공격, 점프 등은 이벤트 방식이 적합
private void OnEnable()
{
    inputActions.Enable();
    inputActions.Player.Attack.performed += OnAttack;
    inputActions.Player.Dodge.performed += OnDodge;
}

private void OnAttack(InputAction.CallbackContext context)
{
    // A키: 일반 공격 (한 번만 실행)
    combatController?.PerformAttack();
}
```
```

---

## 요약

| 요점 | 설명 |
|------|------|
| **자동 생성** | [.inputactions](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions)에서 `InputSystem_Actions.cs` 생성 |
| **타입 안전** | `inputActions.Player.Move`처럼 타입 지정 접근 |
| **Enable/Disable** | 반드시 [OnEnable()](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs#46-50)/[OnDisable()](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs#51-55)에서 관리 |
| **이벤트 방식** | `performed` 이벤트 사용 권장 |
| **Action Map** | 게임플레이와 UI 전환 가능 |

새로운 Input System은 처음엔 복잡해 보이지만, 멀티 플랫폼 대응과 리바인딩 기능 등 장기적으로 큰 장점이 있습니다.

# 회피 스킬 시스템 구현 내역

> **상태**: ✅ 구현 완료
> **브랜치**: `feat/dodge-skill-system` → `master` 병합 완료

ScriptableObject 기반 스킬 데이터를 사용한 가변 거리 대시 이동 및 장애물 처리 시스템.  
초기 계획에서 리팩토링을 거쳐 `ISkill` 인터페이스 기반 슬롯 구조로 전환됨.

---

## 아키텍처 개요

```
PlayerController
  └── ISkill dodgeSkill  ←  DodgeSkill 인스턴스 (순수 C# 클래스)
                                └── SkillBase : ISkill
                                      └── MovementSkillData (ScriptableObject)
```

> [!IMPORTANT]
> **`PlayerSkillController.cs` (MonoBehaviour)는 삭제됨**  
> 리팩토링으로 `DodgeSkill.cs` (순수 C# 클래스)로 대체되었습니다.  
> 회피 관련 로직이 `GridMovementController`에 포함되지 않으며,  
> `PlayerController`가 `Awake()`에서 `DodgeSkill` 인스턴스를 생성합니다.
>
> **`GridMovementController`에 `dodgeSkill` 필드, `TryDodge()` 등이 없음**  
> 초기 계획과 달리, 이동 컨트롤러는 순수 이동만 담당합니다.

---

## 아키텍처 결정 사항

- ✅ **ScriptableObject 사용**: 스킬 데이터를 외부 데이터화
- ✅ **가변 거리 대시**: 고정 2칸이 아닌 N칸 대응
- ✅ **장애물 타입 분류**: 벽/몬스터/낭떠러지를 구별
- ✅ **향 시스템**: `GridMovementController.FacingDirection` (4방향)
- ✅ **ISkill 슬롯 구조**: 공격/회피/고유스킬을 동일한 인터페이스로 통합

> [!WARNING]
> **향후 확장 계획 (미구현)**
> - 대각선 방향 회피 (8방향 대응)
> - 낭떠러지 타일 건너뛰기 처리 (`DodgeSkill.CalculateDodgePath()` 내 `TODO` 주석 참조)
> - 회피 중 무적 시간 (전투 시스템 통합 후)

---

## 구현된 파일 목록

### 데이터 계층

#### [MovementSkillData.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/MovementSkillData.cs)
- `int dashDistance` — 대시 거리 (칸 수)
- `float speedMultiplier` — 이동 속도 배율 (기본 2.0)
- `bool canPassWall` — 벽 통과 여부
- `bool canPassMonster` — 몬스터 통과 여부
- `bool canPassCliff` — 낭떠러지 통과 여부 (미구현, 예약)

#### [SkillData.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/SkillData.cs)
- `MovementSkillData`의 베이스 추상 클래스
- `string skillName`, `Sprite skillIcon`, `float cooldownDuration` 보유

### 스킬 추상화 계층

#### [ISkill.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/ISkill.cs)
- `bool CanExecute()` — 실행 가능 여부 판정
- `void Execute()` — 스킬 실행 (내부에서 CanExecute 체크)

#### [SkillBase.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/SkillBase.cs)
- `ISkill` 구현 추상 클래스
- `Time.time` 기반 쿨타임 관리
- `playerController.IsMoving` 체크 (이동 중 스킬 불가)

### 스킬 구현체

#### [DodgeSkill.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/DodgeSkill.cs)
- `SkillBase` 상속 (순수 C# 클래스, MonoBehaviour 아님)
- `OnExecute()` — 경로 계산 후 `playerController.RequestMoveToPosition()` 호출
- `CalculateDodgePath(start, dir, distance)` — 장애물 판정 포함한 최종 도달 좌표 계산
  - 벽/몬스터 감지 시 앞 칸에서 정지
  - 낭떠러지 처리는 미구현 (`TODO` 주석)

### 그리드 이동 시스템

#### [GridMovementController.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Player/GridMovementController.cs)
- `MoveToPosition(targetPos, speedMultiplier)` — `DodgeSkill`이 호출하는 좌표 지정 이동
- `FacingDirection` — 현재 향 (4방향 `Vector2Int`)
- `isMoving` 플래그 — `MoveToPosition()` **시작 전에** `true` 설정 (동일 프레임 이중 입력 버그 방지)
- `try/finally` 구조로 CancellationToken 예외 시에도 `isMoving = false` 보장

#### [ObstacleType.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Map/ObstacleType.cs)
```csharp
public enum ObstacleType { None, Wall, Monster, Cliff, Hazard }
```

### 플레이어 컨트롤러

#### [PlayerController.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Player/PlayerController.cs)
- `[SerializeField] MovementSkillData dodgeSkillData` — Inspector에서 데이터 할당
- `Awake()` 에서 `new DodgeSkill(dodgeSkillData, this)` 로 슬롯 초기화
- `OnDodgePerformed` → `dodgeSkill?.Execute()` 호출
- 키 바인딩: `F` 키

### Input System

#### [InputSystem_Actions.inputactions](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/InputSystem_Actions.inputactions)
- `Player` Action Map에 `Dodge` (Button) 액션 등록
- 키보드: `<Keyboard>/f`
- Q/W/E/R 고유 스킬 슬롯(`Skill1~4`)도 추가됨

---

## 인스펙터 설정 방법

1. `MovementSkillData` ScriptableObject 에셋 생성
   - Create > Skills > Movement Skill Data
2. Dash Distance, Speed Multiplier, Cooldown 등 설정
3. `PlayerController` 컴포넌트의 `Dodge Skill Data` 슬롯에 할당

---

## 검증

- F 키 입력 → `[DodgeSkill] 回避実行: (현재좌표) → (목표좌표)` 콘솔 로그 출력
- 장애물 앞에서 정지 동작 확인
- 이동 중 F키 입력 시 회피 미발동 확인 (`SkillBase.CanExecute()` 차단)

---

## 미구현 / 향후 과제

| 항목 | 위치 | 비고 |
|---|---|---|
| 낭떠러지 건너뛰기 | `DodgeSkill.CalculateDodgePath()` | `TODO` 주석 있음 |
| 회피 중 무적 | 전투 시스템 구현 후 추가 | — |
| 8방향 대각선 | `GridMovementController` 확장 | — |

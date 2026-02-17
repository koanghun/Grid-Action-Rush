# 구현 계획 - 그리드 공격 시스템

이 계획의 목표는 인스펙터에서 ScriptableObject를 통해 공격 패턴(그리드)을 유연하게 정의할 수 있는 공격 시스템을 구현하는 것입니다.

## 사용자 검토 필요 항목

> [!IMPORTANT]
> **Unity 에디터 작업 필요**
> 저는 파일 생성이나 Unity 에디터 제어 권한이 없으므로, 다음 작업을 직접 수행해주셔야 합니다:
> 1. 스크립트 생성 (`PlayerAttackController.cs`, `PoolManager.cs`, `EnemyHealth.cs`).
> 2. ScriptableObject (`AttackSkillData`) 에셋 생성.
> 3. 인스펙터에서 `attackGridPattern` 설정.
> 4. `PlayerAttackController` 컴포넌트에 참조 할당.

## 변경 제안

### 데이터 계층
#### [수정] [AttackSkillData.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Skills/AttackSkillData.cs)
- 사용자 정의 상대 좌표를 위한 `List<Vector2Int> attackGridPattern` 필드 추가.
- 시각 효과 관련 필드 추가 (`rangeEffectPrefab`, `effectDuration`).

### 코어 시스템
#### [신규] [PoolManager.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Managers/PoolManager.cs)
- 오브젝트 풀링을 관리하기 위한 싱글톤 패턴 적용.
- 주요 메서드: `Get(prefab, pos, rot)`, `Return(obj)`.
- **이유**: 공격 시 빈번한 생성/파괴로 인한 GC 스파이크를 방지하여 WebGL 성능 최적화.

### 플레이어 로직
#### [신규] [PlayerAttackController.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/PlayerAttackController.cs)
- `PlayerController`로부터 입력을 전달받아 처리.
- `Time.time`을 사용한 쿨타임 관리.
- 플레이어가 바라보는 방향을 기준으로 `attackGridPattern`을 실제 월드 좌표로 변환.
- `PoolManager`를 통해 이펙트 생성.
- `MapManager.GetEntitiesInArea`를 사용하여 적 감지 (Physics2D 사용 안 함).

#### [수정] [PlayerController.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Player/PlayerController.cs)
- `PlayerAttackController` 의존성 주입 및 참조 추가.
- 'Attack' 입력 액션을 컨트롤러로 라우팅.

### 테스트/적(Enemy)
#### [신규] [EnemyHealth.cs](file:///c:/Users/parkkh/projects/Grid-Action-Rush/Assets/Scripts/Enemy/EnemyHealth.cs)
- `GridEntityBase`를 상속받아 자동으로 MapManager에 등록/해제.
- 공격 로직 검증을 위한 간단한 HP 관리 및 `TakeDamage` 메서드 구현.
- `Awake` 시 Transform 위치를 기준으로 그리드 좌표 초기화 및 스냅핑.

## 검증 계획

### 수동 검증
1. **에디터 설정**:
   - `AttackSkillData` 에셋 생성 및 "십자(Cross)" 패턴 설정: `(0,1), (0,-1), (1,0), (-1,0)`.
   - 플레이어에게 할당.
2. **플레이 모드**:
   - 'A' 키(공격 키) 입력.
   - 설정한 상대 그리드 위치에 이펙트가 올바르게 표시되는지 확인.
   - 더미 적(Enemy)에 대한 데미지 로그가 콘솔에 출력되는지 확인.

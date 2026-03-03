# 공격 시스템 구현 내역

> **상태**: ✅ 구현 완료
> **브랜치**: `feat/attack-system` → `master` 병합 완료

인스펙터에서 ScriptableObject를 통해 공격 패턴(그리드)을 유연하게 정의할 수 있는 공격 시스템.  
초기 계획에서 리팩토링을 거쳐 `ISkill` 인터페이스 기반 슬롯 구조로 전환됨.

---

## 아키텍처 개요

```
PlayerController
  └── ISkill attackSkill  ←  AttackSkill 인스턴스 (순수 C# 클래스)
                                └── SkillBase : ISkill
                                      └── AttackSkillData (ScriptableObject)
```

> [!IMPORTANT]
> **`PlayerAttackController.cs` (MonoBehaviour)는 삭제됨**  
> 리팩토링으로 `AttackSkill.cs` (순수 C# 클래스)로 대체되었습니다.  
> 컴포넌트를 플레이어 오브젝트에 직접 붙이는 구조가 아닌,  
> `PlayerController`가 `Awake()`에서 인스턴스를 직접 생성합니다.

---

## 구현된 파일 목록

### 데이터 계층

#### [AttackSkillData.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/AttackSkillData.cs)
- `List<Vector2Int> attackGridPattern` — 플레이어 기준 상대 좌표 패턴
- `int damage` — 데미지 량
- `AttackRangePattern rangePattern` — 하위 호환용 열거형 (실제로는 `attackGridPattern` 사용)
- `GameObject rangeEffectPrefab` — 공격 범위 이펙트 프리팹
- `float effectDuration` — 이펙트 표시 시간

#### [SkillData.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/SkillData.cs)
- `AttackSkillData`의 베이스 추상 클래스
- `string skillName`, `Sprite skillIcon`, `float cooldownDuration` 보유

### 스킬 추상화 계층

#### [ISkill.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/ISkill.cs)
- `bool CanExecute()` — 실행 가능 여부 판정
- `void Execute()` — 스킬 실행 (내부에서 CanExecute 체크)

#### [SkillBase.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/SkillBase.cs)
- `ISkill` 구현 추상 클래스
- `Time.time` 기반 쿨타임 관리 로직을 여기에 공통화
- `playerController.IsMoving` 체크 (이동 중 스킬 사용 불가)
- 서브클래스는 `OnExecute()` 만 구현

### 스킬 구현체

#### [AttackSkill.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Skills/AttackSkill.cs)
- `SkillBase` 상속 (순수 C# 클래스, MonoBehaviour 아님)
- `CalculateAttackRange()` — 플레이어 향에 따라 패턴 회전 후 월드 좌표 변환
- `RotateByFacing()` — 4방향 회전 로직 (상하좌우)
- `ShowAttackEffectAsync()` — `PoolManager`를 통해 이펙트 생성/반환 (UniTask)
- `ApplyDamageToTargets()` — `MapManager.GetEntitiesInArea()`로 그리드 기반 피격 판정

### 코어 시스템

#### [PoolManager.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Manager/PoolManager.cs)
- 싱글톤 패턴
- `Get(prefab, pos, rot)` — 풀에서 오브젝트 꺼내기
- `Return(prefab, obj)` — 풀에 오브젝트 반환
- WebGL GC 스파이크 방지 목적

#### [PlayerController.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Player/PlayerController.cs)
- `[SerializeField] AttackSkillData attackSkillData` — Inspector에서 데이터 할당
- `Awake()` 에서 `new AttackSkill(attackSkillData, this)` 로 슬롯 초기화
- `OnAttackPerformed` → `attackSkill?.Execute()` 호출
- 키 바인딩: `A` 키 (+ 마우스 좌클릭)

### 테스트/적(Enemy)

#### [EnemyHealth.cs](file:///Users/kh/Develop/Unity_Projects/Grid-Action-Rush/Assets/Scripts/Enemy/EnemyHealth.cs)
- `GridEntityBase` 상속 → `MapManager`에 자동 등록/해제
- `TakeDamage(int damage)` — HP 감소 + 사망 처리 (`Destroy`)
- `Awake()` 에서 Transform 위치 → 그리드 좌표 변환 및 스냅

---

## 인스펙터 설정 방법

1. `AttackSkillData` ScriptableObject 에셋 생성
   - Create > Skills > Attack Skill Data
2. `attackGridPattern` 에 공격 패턴 좌표 입력
   - 십자: `(0,1), (0,-1), (1,0), (-1,0)`
3. `PlayerController` 컴포넌트의 `Attack Skill Data` 슬롯에 할당
4. (선택) `rangeEffectPrefab` 에 이펙트 프리팹 할당

---

## 검증

- 'A' 키 입력 → `[AttackSkill] 攻撃実行: N個のグリッドを攻撃` 콘솔 로그 출력
- 설정한 상대 그리드 위치에 이펙트 표시 (rangeEffectPrefab 할당 시)
- 범위 내 `EnemyHealth` 오브젝트에 데미지 적용 확인

# Unity 에디터 설정 가이드

## 1. Scene 기본 구조 (필수 Manager 오브젝트)

### MapManager 설정
1. **Hierarchy**에서 빈 GameObject 생성 → 이름: `MapManager`
2. `MapManager.cs` 컴포넌트 추가
3. Inspector 설정:
   - **Tilemap**: Scene의 Grid > Tilemap 오브젝트를 드래그
   - **Tile Mappings**: 사용하는 타일과 TileData ScriptableObject 연결

### PoolManager 설정
1. **Hierarchy**에서 빈 GameObject 생성 → 이름: `PoolManager`
2. `PoolManager.cs` 컴포넌트 추가
3. 설정 불필요 (자동 관리)

---

## 2. Player 설정

### 기존 Player GameObject에 컴포넌트 추가
1. Player GameObject 선택
2. `PlayerAttackController.cs` 컴포넌트 추가
3. Inspector 설정:
   - **Attack Data**: 생성한 AttackSkillData ScriptableObject 할당 (아래 참조)

### Input Actions 설정
1. `InputSystem_Actions` 에셋 더블클릭
2. **Player** Action Map 선택
3. **Attack** 액션 추가:
   - Action Type: Button
   - Binding: Keyboard > A
4. **Save Asset** 클릭

---

## 3. Enemy 생성

### 적 GameObject 생성
1. **Hierarchy**에서 새 GameObject 생성 → 이름: `Enemy`
2. **Transform 위치를 정수 좌표로 설정** (중요!)
   - Position: (5, 3, 0) ← 정수여야 함
3. 컴포넌트 추가:
   - `EnemyHealth.cs` (이미 GridEntityBase 상속됨)
   - `SpriteRenderer` (시각화용, 선택사항)
4. Inspector 설정:
   - **Max Health**: 30
5. **주의**: Collider2D는 **추가하지 않음** (Physics2D 사용 안 함)

### 여러 적 배치
- 같은 방법으로 Enemy를 복제하여 여러 곳에 배치
- 각 적의 Position을 정수 좌표로 설정

---

## 4. 2x2 보스 생성 (선택사항)

### 보스 스크립트 생성 (Unity에서)
1. Unity Editor에서 `Assets/Scripts/Enemy/BossEnemy.cs` 생성
2. 아래 코드 작성:

```csharp
using UnityEngine;

public class BossEnemy : GridEntityBase
{
    public override GridEntityType EntityType => GridEntityType.Boss;

    [SerializeField] private int maxHealth = 300;
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        
        // 현재 위치를 루트로 2x2 초기화
        Vector2Int rootPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        
        InitializeMultiGridEntity(rootPos, width: 2, height: 2);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[BossEnemy] ダメージ: {damage} (残りHP: {currentHealth})");
        
        if (currentHealth <= 0)
        {
            Debug.Log("[BossEnemy] ボス撃破！");
            Destroy(gameObject);
        }
    }
}
```

### 보스 GameObject 생성
1. Hierarchy에서 새 GameObject 생성 → 이름: `Boss`
2. Transform Position: (10, 10, 0) ← 2x2 영역의 왼쪽 아래 좌표
3. `BossEnemy.cs` 컴포넌트 추가
4. `SpriteRenderer` 추가 (크기를 2x2로 설정)

---

## 5. AttackSkillData ScriptableObject 생성

### 기본 공격 생성
1. Project 창에서 우클릭 → `Create > Skills > Attack Skill Data`
2. 이름: `BasicAttack`
3. Inspector 설정:
   ```
   Skill Name: 基本攻撃
   Damage: 10
   Cooldown Duration: 1.0
   
   Attack Grid Pattern:
   - Size: 1
   - Element 0: (0, 1)  // 정면 1칸
   
   Range Effect Prefab: (아래에서 생성한 이펙트 할당)
   Effect Duration: 0.5
   ```

### 십자 공격 생성 (선택사항)
1. Project 창에서 우클릭 → `Create > Skills > Attack Skill Data`
2. 이름: `CrossAttack`
3. Inspector 설정:
   ```
   Attack Grid Pattern:
   - Size: 4
   - Element 0: (0, 1)   // 위
   - Element 1: (0, -1)  // 아래
   - Element 2: (1, 0)   // 오른쪽
   - Element 3: (-1, 0)  // 왼쪽
   ```

---

## 6. 공격 이펙트 프리팹 생성 (선택사항)

### 간단한 이펙트 만들기
1. Hierarchy에서 빈 GameObject 생성 → 이름: `AttackEffect`
2. `SpriteRenderer` 컴포넌트 추가
3. Sprite 설정 또는 색상 설정:
   - Color: 빨간색, Alpha: 0.5 (반투명)
4. Project로 드래그하여 **프리팹화**
5. Hierarchy에서 원본 삭제
6. `AttackSkillData`의 **Range Effect Prefab**에 이 프리팹을 할당

---

## 7. EnemyHealth.cs 수정 (이미 수정됨)

사용자가 이미 수정했지만, 완전한 코드:

```csharp
using UnityEngine;

public class EnemyHealth : GridEntityBase
{
    public override GridEntityType EntityType => GridEntityType.Enemy;

    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        
        // 현재 위치로 초기화
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        InitializeSingleGridEntity(gridPos);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] ダメージ: {damage} (残りHP: {currentHealth})");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"[EnemyHealth] 倒された: {gameObject.name}");
            Destroy(gameObject);
        }
    }
}
```

---

## 8. 테스트 시나리오

### TC-1: 기본 공격 테스트
1. Player를 (5, 4)에 배치
2. Enemy를 (5, 5)에 배치
3. Play Mode 실행
4. 위쪽(W) 방향키로 플레이어 방향 전환
5. `A` 키로 공격
6. **예상 결과**: Enemy가 데미지 받음

### TC-2: 십자 공격 테스트
1. Player를 (5, 5)에 배치
2. Enemy를 4방향 (5,6), (5,4), (6,5), (4,5)에 배치
3. PlayerAttackController의 Attack Data를 `CrossAttack`으로 변경
4. Play Mode 실행
5. `A` 키로 공격
6. **예상 결과**: 4마리 모두 데미지

### TC-3: 2x2 보스 테스트
1. Boss를 (10, 10)에 배치
2. Player를 (10, 9)에 배치
3. 위쪽을 보고 공격
4. **예상 결과**: 보스가 한 번만 데미지 (중복 없음)

---

## 요약 체크리스트

- [ ] MapManager GameObject 생성 및 설정
- [ ] PoolManager GameObject 생성
- [ ] Player에 PlayerAttackController 추가
- [ ] Input Actions에 Attack 액션 추가 (A 키)
- [ ] AttackSkillData ScriptableObject 생성
- [ ] 공격 이펙트 프리팹 생성 (선택사항)
- [ ] Enemy GameObject 생성 (정수 좌표!)
- [ ] EnemyHealth.cs 수정 완료 (GridEntityBase 상속)
- [ ] 테스트 실행

모든 설정이 완료되면 Play Mode에서 공격 시스템이 작동합니다!

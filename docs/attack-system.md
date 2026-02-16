# 공격 시스템 구현 계획

플레이어와 몬스터의 공격 시스템을 구현합니다. 그리드 범위 판정, ScriptableObject 기반 데이터 관리, 시각 이펙트, 쿨타임 관리를 포함한 포괄적인 시스템을 구축합니다.

## 배경 (Background)

현재 플레이어와 몬스터의 공격 시스템이 미구현 상태입니다. 기존 `AttackSkillData.cs`에는 기본적인 구조가 정의되어 있지만, 실행 로직이 존재하지 않습니다.

**기존 코드베이스 분석 결과:**
- ✅ `SkillData` 추상 클래스: 쿨타임 관리 기반 존재
- ✅ `AttackSkillData`: 데미지와 공격 범위 패턴 enum 정의됨
- ✅ `PlayerSkillController`: 회피 스킬 구현 패턴 참고 가능
- ⚠️ 공격 범위의 구체적인 그리드 패턴 미구현 (TODO 주석 확인)
- ⚠️ 공격 실행 로직 미구현

## 사용자 확인 필요 사항

> [!IMPORTANT]
> **Unity Editor에서 수동 작업 필수**
> 
> 다음 작업은 사용자가 Unity Editor에서 직접 수행해야 합니다:
> - **ScriptableObject 생성**: Unity Editor에서 Create > Skills > Attack Skill Data
> - **공격 범위 설정**: Inspector에서 공격 패턴(Single/Cross/Square3x3/Line)과 공격 그리드 리스트 설정
> - **PlayerAttackController 참조 설정**: Inspector에서 생성한 AttackSkillData 할당
> - **이펙트 프리팹 생성**: 공격 범위 표시용 그리드 이펙트 프리팹(파티클 or 스프라이트)
> - **Object Pool 설정**: PoolManager에 공격 이펙트 프리팹 등록

> [!WARNING]
> **기존 코드 영향 범위**
> 
> `PlayerController`에 공격 입력 처리를 추가합니다. 기존 회피 스킬 구현에는 영향을 주지 않습니다.

## 구현 변경 사항

### 데이터 레이어

#### [수정] [AttackSkillData.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Skills/AttackSkillData.cs)

기존 TODO 주석 부분을 구현합니다.

**추가 내용:**
```csharp
[Header("攻撃範囲設定")]
[Tooltip("攻撃範囲グリッド座標リスト（プレイヤー基準の相対座標）")]
public List<Vector2Int> attackGridPattern = new List<Vector2Int>();

[Header("視覚エフェクト設定")]
[Tooltip("攻撃範囲表示用エフェクトプレハブ")]
public GameObject rangeEffectPrefab;

[Tooltip("エフェクト表示時間")]
[Min(0.1f)]
public float effectDuration = 0.5f;
```

**설계 의도:**
- `attackGridPattern`: 에디터에서 자유롭게 공격 범위 설정 가능 (예: Cross 패턴이면 `[(0,1), (1,0), (0,-1), (-1,0)]`)
- `rangePattern` enum은 후방 호환성을 위해 유지하되, 실제 판정은 `attackGridPattern` 사용
- ScriptableObject로 여러 공격 패턴 양산 가능 (일반 공격, 스킬1, 스킬2 등)

---

### 컨트롤러 레이어

#### [신규] [PlayerAttackController.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Player/PlayerAttackController.cs)

플레이어 공격 로직을 관리하는 새 MonoBehaviour를 생성합니다.

**주요 책임:**
1. **공격 입력 처리**: A 키 입력 감지
2. **쿨타임 관리**: 이전 공격으로부터 경과 시간 추적
3. **공격 범위 계산**: 플레이어의 방향과 `AttackSkillData.attackGridPattern`으로 실제 월드 좌표 계산
4. **시각 이펙트 표시**: Object Pool에서 공격 범위 이펙트 획득 및 표시
5. **데미지 판정**: 공격 범위 내 적에게 데미지 이벤트 발행

**클래스 구조:**
```csharp
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector設定
    [SerializeField] private AttackSkillData attackData;
    #endregion

    #region 依存コンポーネント
    private PlayerController playerController;
    #endregion

    #region クールタイム管理
    private float lastAttackTime = -Mathf.Infinity;
    #endregion

    public void PerformAttack() { /* 実装 */ }
    private List<Vector2Int> CalculateAttackRange() { /* 実装 */ }
    private async UniTask ShowAttackEffectAsync(List<Vector2Int> targetGrids) { /* 実装 */ }
    private void ApplyDamageToTargets(List<Vector2Int> targetGrids) { /* 実装 */ }
}
```

**설계 패턴:**
- `PlayerSkillController`의 회피 스킬 구현 패턴 답습
- Facade 패턴: `PlayerController`를 통해 호출
- Single Responsibility: 공격 로직만 담당

**WebGL 최적화:**
- Object Pooling 사용으로 이펙트 생성 시 GC 회피
- UniTask 사용으로 이펙트 표시 비동기 처리 (WebGL Single Thread 대응)
- `Time.time` 기반 쿨타임 관리 (Coroutine보다 메모리 효율적)

---

#### [수정] [PlayerController.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Player/PlayerController.cs)

공격 입력을 받아 `PlayerAttackController`에 위임합니다.

**추가 내용:**
```csharp
#region 依存コンポーネント
private PlayerAttackController attackController; // 追加
#endregion

private void Awake()
{
    // 既存コード...
    attackController = GetComponent<PlayerAttackController>(); // 追加
}

private void OnAttackInput()
{
    attackController?.PerformAttack();
}
```

**Input System 설정:**
- Input Actions에 "Attack" 액션 추가 (A 키 바인딩)
- `OnAttackInput()`을 "Attack" 액션의 콜백에 등록

---

### 시각 이펙트 레이어

#### [신규] [PoolManager.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Managers/PoolManager.cs)

Object Pooling을 관리하는 싱글톤 MonoBehaviour를 생성합니다.

> [!NOTE]
> **범용 설계**
> 
> 향후 몬스터 공격 이펙트, 스킬 이펙트, UI 이펙트 등 다양한 프리팹에 대응 가능한 범용 풀 관리 시스템을 구축합니다.

**주요 API:**
```csharp
public static PoolManager Instance { get; private set; }

/// <summary>
/// プールからオブジェクトを取得
/// </summary>
public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation);

/// <summary>
/// オブジェクトをプールに返却
/// </summary>
public void Return(GameObject obj);
```

**데이터 구조:**
```csharp
private Dictionary<GameObject, Queue<GameObject>> poolDictionary;
```

**WebGL 최적화:**
- 초기 풀 크기 설정 가능 (Warmup으로 시작 시 생성)
- Dictionary를 통한 O(1) 프리팹 검색
- Queue를 통한 FIFO 관리로 캐시 효율 향상

---

### 적 통합 (기본 구현)

#### [신규] [EnemyHealth.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Enemy/EnemyHealth.cs)

테스트용 간이 적 HP 관리 컴포넌트를 생성합니다.

> [!NOTE]
> **테스트용 최소 구현**
> 
> 본격적인 적 시스템은 별도 이슈로 구현 예정입니다. 이번에는 공격 시스템 동작 확인용으로 최소한의 기능만 구현합니다.

**주요 기능:**
- HP 관리 (Inspector에서 설정 가능)
- 데미지 수신 API
- 디버그 로그 출력
- HP 0이 되면 GameObject 파괴

**클래스 구조:**
```csharp
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[EnemyHealth] ダメージ受け取り: {damage} (残りHP: {currentHealth})");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[EnemyHealth] 敵が倒された");
        Destroy(gameObject);
    }
}
```

---

#### [수정] [PlayerAttackController.cs](file:///c:/Users/zse63/unity/Grid-Action-Rush/Assets/Scripts/Player/PlayerAttackController.cs)

데미지 판정 로직에 적 검출을 추가합니다.

**추가 내용:**
```csharp
private void ApplyDamageToTargets(List<Vector2Int> targetGrids)
{
    foreach (var gridPos in targetGrids)
    {
        // グリッド座標をワールド座標に変換
        Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, 0);
        
        // 該当座標の敵を検出（Physics2D.OverlapCircleAll）
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.3f);
        
        foreach (var hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackData.damage);
            }
        }
    }
}
```

**설계 고려 사항:**
- `Physics2D.OverlapCircleAll`로 범위 내 적 검출
- 레이어 마스크 사용으로 불필요한 충돌 판정 제외 (성능 향상)
- 향후 `MapManager.GetEntitiesAtPosition()` API로 교체 가능

---

## 검증 계획

### 자동화 테스트

현재 프로젝트에 유닛 테스트가 존재하지 않으므로 자동 테스트는 작성하지 않습니다. Unity Play Mode에서 수동 테스트로 검증합니다.

### 수동 검증

> [!IMPORTANT]
> **Unity Editor에서 수동 테스트 필수**
> 
> 다음 테스트 케이스를 사용자가 Unity Editor에서 실행해야 합니다.

#### 준비 (Setup)

1. **공격 ScriptableObject 생성**:
   - Project 창에서 우클릭 > Create > Skills > Attack Skill Data
   - 설정 예시:
     - `skillName`: "통상공격"
     - `damage`: 10
     - `cooldownDuration`: 1.0
     - `attackGridPattern`: `[(0, 1)]` (전방 1칸)

2. **공격 이펙트 프리팹 생성**:
   - Sprite 또는 Particle System으로 시각 이펙트 생성
   - 0.5초 후 자동 삭제되는 컴포넌트 추가

3. **테스트용 적 GameObject 생성**:
   - Cube 또는 Sprite로 적 생성
   - `EnemyHealth` 컴포넌트 추가 (maxHealth = 30)
   - 레이어를 "Enemy"로 설정

4. **PoolManager 셋업**:
   - Scene에 빈 GameObject 생성
   - `PoolManager` 컴포넌트 추가

5. **Player 셋업**:
   - `PlayerAttackController` 컴포넌트 추가
   - Inspector에서 `attackData`에 생성한 ScriptableObject 할당

#### 테스트 케이스 (Test Cases)

**TC-1: 기본 공격 발동**
1. Play Mode 실행
2. A 키 누르기
3. **예상 결과**:
   - 콘솔에 디버그 로그 표시
   - 공격 범위에 이펙트 표시 (0.5초간)

**TC-2: 쿨타임 검증**
1. Play Mode 실행
2. A 키 연타
3. **예상 결과**:
   - 1초 간격으로만 공격 발동
   - 쿨타임 중에는 아무 일도 일어나지 않음
   - 콘솔에 "[PlayerAttackController] クールタイム中です" 표시

**TC-3: 적에 대한 데미지 판정**
1. Play Mode 실행
2. 플레이어를 적의 1칸 아래에 배치
3. 위쪽 방향을 보고 A 키 누르기
4. **예상 결과**:
   - 콘솔에 "[EnemyHealth] ダメージ受け取り: 10 (残りHP: 20)" 표시
   - 3회 공격하면 적이 파괴됨

**TC-4: 공격 범위 밖의 적**
1. Play Mode 실행
2. 플레이어를 적으로부터 2칸 이상 떨어뜨려 배치
3. A 키 누르기
4. **예상 결과**:
   - 이펙트는 표시되지만 데미지는 발생하지 않음
   - 적의 HP가 줄어들지 않음

**TC-5: 복수 칸 공격 범위**
1. 공격 ScriptableObject의 `attackGridPattern`을 Cross 패턴으로 변경:
   - `[(0,1), (1,0), (0,-1), (-1,0)]`
2. 적을 4방향에 배치
3. A 키 누르기
4. **예상 결과**:
   - 4방향 모두에 이펙트 표시
   - 4마리 적 모두 데미지를 받음

---

## 구현 노트

### 아키텍처 패턴

- **ScriptableObject 데이터 주도 설계**: 코드 변경 없이 공격 패턴 추가 가능
- **Object Pooling**: WebGL 환경에서 GC 스파이크 회피
- **Facade 패턴**: `PlayerController`가 입력을 받아 전문 컨트롤러에 위임
- **Single Responsibility Principle**: 각 컨트롤러가 단일 책임 보유

### 확장성 고려

향후 확장 포인트:

- **몬스터 공격**: `EnemyAttackController` 추가하여 동일한 `AttackSkillData` 재사용
- **공격 모션**: Animator와 연동하여 애니메이션 재생
- **연속 공격 (콤보)**: `lastAttackTime`을 배열화하여 콤보 카운트 관리
- **공격 범위 프리뷰**: 공격 전 범위를 반투명 표시
- **넉백**: `AttackSkillData`에 `knockbackDistance` 추가

### WebGL 최적화 체크리스트

- ✅ Object Pooling으로 이펙트 생성 시 GC 회피
- ✅ UniTask 사용으로 비동기 처리 (Threading 미사용)
- ✅ `Time.time` 기반 쿨타임 관리 (메모리 효율적)
- ✅ Dictionary를 통한 O(1) 프리팹 검색
- ✅ Physics2D 레이어 마스크 사용으로 불필요한 판정 제외

---

## 제안 커밋 메시지

```
feat: プレイヤーとモンスターの攻撃システムを実装

- AttackSkillDataに攻撃範囲グリッドパターンを追加
- PlayerAttackControllerでAキー攻撃処理を実装
- PoolManagerでObject Poolingシステムを構築
- EnemyHealthで簡易的なダメージ判定を実装

WebGL最適化のためObject PoolingとUniTaskを採用。
ScriptableObjectベースでエディタから攻撃パターンを自由に設計可能。

Resolves #[이슈번호]
```

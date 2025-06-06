using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("에이전트 설정")]
    public string agentName = "BT Agent";
    public IBattleAgent agent;

    [Header("이동 설정")]
    public float moveSpeed = 5f;

    [Header("체력 설정")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("애니메이션")]
    public Animator animator;

    [Header("UI 연결")]
    public Slider healthBar;

    [Header("회전 설정")]
    public float rotationSpeed = 10f;

    [Header("쿨타임 설정")]
    public float attackCooldownTime = 2.5f;
    public float defendCooldownTime = 2.5f;
    public float dodgeCooldownTime = 5.0f;

    [Header("전투 설정")]
    public float attackRange = 2f;
    public float attackDamage = 25f;

    [Header("무기 시스템")]
    public GameObject weaponObject; // 무기 오브젝트(콜라이더+isTrigger, 태그="Weapon")
    private Collider weaponCollider;
    private Coroutine attackCoroutine;

    // 내부 상태
    private Rigidbody rb;
    private AgentController enemy;
    private AgentState currentState = AgentState.Idle;

    // 쿨타임 관리
    private float attackCooldown = 0f;
    private float defendCooldown = 0f;
    private float dodgeCooldown = 0f;

    // 행동 락
    private bool isActionPlaying = false;

    // 무적 시간
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float invincibleDuration = 2.0f;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        currentHP = maxHP;

        // 무기 콜라이더 참조 및 기본 비활성화
        // if (weaponObject != null)
            weaponCollider = weaponObject.GetComponent<Collider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    void Start()
    {
        // 같은 GameObject에 있는 IBattleAgent 구현체 찾기
        IBattleAgent foundAgent = GetComponent<IBattleAgent>();
        if (foundAgent != null)
        {
            SetAgent(foundAgent);
        }
    }

    void Update()
    {
        UpdateCooldowns();
        UpdateInvincibility();
        UpdateMovementAnimation();
        UpdateHealthBarUI();
    }

    void FixedUpdate()
    {
        // 행동 중이면 이동 강제 정지
        if (isActionPlaying)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    #region UI 업데이트

    private void UpdateHealthBarUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHP / maxHP;
        }
    }

    #endregion

    #region 애니메이션 업데이트

    private void UpdateMovementAnimation()
    {
        if (animator == null) return;

        // 실제 velocity를 기반으로 이동 상태 확인
        Vector3 velocity = rb.velocity;
        bool isMoving = velocity.magnitude > 0.1f;

        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // 로컬 좌표계 기준으로 이동 방향 계산
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            animator.SetFloat("MoveX", Mathf.Clamp(localVelocity.x, -1f, 1f));
            animator.SetFloat("MoveY", Mathf.Clamp(localVelocity.z, -1f, 1f));
        }
        else
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }
    }

    #endregion

    #region 공개 메서드 (BattleManager에서 호출)

    public void SetEnemy(AgentController enemyAgent)
    {
        enemy = enemyAgent;
    }

    public void SetAgent(IBattleAgent battleAgent)
    {
        agent = battleAgent;
        if (agent != null)
        {
            agent.Initialize(this);
        }
    }

    public void StartBattle()
    {
        currentHP = maxHP;
        currentState = AgentState.Idle;

        attackCooldown = 0f;
        defendCooldown = 0f;
        dodgeCooldown = 0f;

        isActionPlaying = false;
        isInvincible = false;

        Debug.Log($"{agentName} 전투 시작!");
    }

    public void UpdateAgent(GameObservation observation)
    {
        if (!IsAlive() || agent == null) return;

        // 에이전트에게 행동 결정 요청
        AgentAction action = agent.DecideAction(observation);

        // 행동 실행
        ActionResult result = ExecuteAction(action);

        // 결과를 에이전트에게 통보
        agent.OnActionResult(result);
    }

    public void OnBattleEnd(EpisodeResult result)
    {
        if (agent != null)
        {
            agent.OnEpisodeEnd(result);
        }

        Debug.Log($"{agentName} 전투 종료 - 승리: {result.won}");
    }

    public void ResetAgent()
    {
        currentHP = maxHP;
        currentState = AgentState.Idle;

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }
    }

    #endregion

    #region 상태 조회 메서드

    public bool IsAlive() => currentHP > 0f;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
    public AgentState GetCurrentState() => currentState;
    public string GetAgentName() => agentName;

    public CooldownState GetCooldownState()
    {
        return new CooldownState
        {
            attackCooldown = attackCooldown,
            defendCooldown = defendCooldown,
            dodgeCooldown = dodgeCooldown,
            attackMaxTime = attackCooldownTime,
            defendMaxTime = defendCooldownTime,
            dodgeMaxTime = dodgeCooldownTime
        };
    }

    #endregion

    #region 행동 실행

    public ActionResult ExecuteAction(AgentAction action)
    {
        if (!IsAlive() || isActionPlaying)
        {
            return ActionResult.Failure(action.type, "행동 불가 상태");
        }

        switch (action.type)
        {
            case ActionType.Idle:
                return ExecuteIdle();

            case ActionType.MoveForward:
            case ActionType.MoveBack:
            case ActionType.MoveLeft:
            case ActionType.MoveRight:
                return ExecuteMove(action);

            case ActionType.Attack:
                return ExecuteAttack();

            case ActionType.Defend:
                return ExecuteDefend();

            case ActionType.Dodge:
                return ExecuteDodge();

            default:
                return ActionResult.Failure(action.type, "알 수 없는 행동");
        }
    }

    private ActionResult ExecuteIdle()
    {
        currentState = AgentState.Idle;
        return ActionResult.Success(ActionType.Idle);
    }

    private ActionResult ExecuteMove(AgentAction action)
    {
        if (isActionPlaying)
            return ActionResult.Failure(action.type, "다른 행동 중");

        Vector3 moveDirection = GetMoveDirection(action.type);

        // 월드 좌표 기준 이동 (회전에 영향받지 않음)
        Vector3 worldMoveDirection = moveDirection;

        rb.velocity = new Vector3(worldMoveDirection.x * moveSpeed, rb.velocity.y, worldMoveDirection.z * moveSpeed);

        // 이동 방향을 바라보도록 회전
        if (worldMoveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
            StartCoroutine(SmoothRotation(targetRotation, 0.2f));
        }

        currentState = AgentState.Moving;
        return ActionResult.Success(action.type);
    }

    private ActionResult ExecuteAttack()
    {
        if (attackCooldown > 0f)
            return ActionResult.Failure(ActionType.Attack, "공격 쿨타임");

        // 공격 전에 적을 바라보도록 회전
        if (enemy != null)
        {
            Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            if (directionToEnemy.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                StartCoroutine(SmoothRotation(targetRotation, 0.1f));
            }
        }

        attackCooldown = attackCooldownTime;
        currentState = AgentState.Attacking;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        StartActionLock(0.7f); // 공격 모션 시간

        // LYD 시스템: 공격 애니메이션 시간 동안만 무기 콜라이더 활성화
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(EnableWeaponColliderForDuration(0.7f));

        return ActionResult.Success(ActionType.Attack, 0f); // 데미지는 충돌에서 처리
    }

    private ActionResult ExecuteDefend()
    {
        if (defendCooldown > 0f)
            return ActionResult.Failure(ActionType.Defend, "방어 쿨타임");

        defendCooldown = defendCooldownTime;
        currentState = AgentState.Defending;

        if (animator != null)
        {
            animator.SetTrigger("Guard");
        }

        StartActionLock(0.7f);

        return ActionResult.Success(ActionType.Defend);
    }

    private ActionResult ExecuteDodge()
    {
        if (dodgeCooldown > 0f)
            return ActionResult.Failure(ActionType.Dodge, "회피 쿨타임");

        dodgeCooldown = dodgeCooldownTime;
        currentState = AgentState.Dodging;

        if (animator != null)
        {
            animator.SetTrigger("Dodge");
        }

        StartActionLock(0.7f);

        // 회피 중 무적 처리
        isInvincible = true;
        invincibleTimer = 0.7f;

        return ActionResult.Success(ActionType.Dodge);
    }

    #endregion

    #region 유틸리티 메서드

    private Vector3 GetMoveDirection(ActionType moveType)
    {
        switch (moveType)
        {
            case ActionType.MoveForward: return Vector3.forward;
            case ActionType.MoveBack: return Vector3.back;
            case ActionType.MoveLeft: return Vector3.left;
            case ActionType.MoveRight: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    private void UpdateCooldowns()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;
        if (defendCooldown > 0f)
            defendCooldown -= Time.deltaTime;
        if (dodgeCooldown > 0f)
            dodgeCooldown -= Time.deltaTime;
    }

    private void UpdateInvincibility()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
                isInvincible = false;
        }
    }

    private void StartActionLock(float duration)
    {
        isActionPlaying = true;
        Invoke(nameof(EndActionLock), duration);
    }

    private void EndActionLock()
    {
        isActionPlaying = false;
        currentState = AgentState.Idle;
    }

    // 부드러운 회전을 위한 코루틴
    private IEnumerator SmoothRotation(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible || !IsAlive()) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        isInvincible = true;
        invincibleTimer = invincibleDuration;

        Debug.Log($"{agentName} 피해 {damage}, 남은 HP: {currentHP}");

        if (!IsAlive())
        {
            currentState = AgentState.Dead;
            Debug.Log($"{agentName} 사망!");
        }
    }

    #endregion

    #region 무기 시스템 (LYD 기반)

    /// <summary>
    /// 공격 애니메이션 시간 동안 무기 콜라이더 On/Off (LYD 시스템)
    /// </summary>
    private IEnumerator EnableWeaponColliderForDuration(float duration)
    {
        EnableWeaponCollider();
        // 실제 판정 타이밍(애니메이션 흐름에 따라 0.7~1.0배수 조절 가능)
        yield return new WaitForSeconds(duration * 0.7f);
        DisableWeaponCollider();
    }

    /// <summary>
    /// 무기 콜라이더 활성화
    /// </summary>
    private void EnableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = true;
    }

    /// <summary>
    /// 무기 콜라이더 비활성화
    /// </summary>
    private void DisableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    /// <summary>
    /// 무기에 맞을 때 피격 판정 (LYD 시스템)
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon") && !isInvincible)
        {
            TakeDamage(25f);
            Debug.Log($"{agentName} 무기에 피격! 데미지: 25");
        }
    }

    #endregion

}



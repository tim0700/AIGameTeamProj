using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("에이전트 설정")]
    public string agentName = "BT Agent";
    public IBattleAgent agent;

    [Header("이동 설정 (Player.cs 기반)")]
    public float moveSpeed = 5f;

    [Header("체력 설정")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("애니메이션")]
    public Animator animator;

    [Header("쿨타임 설정")]
    public float attackCooldownTime = 2.5f;
    public float defendCooldownTime = 2.5f;
    public float dodgeCooldownTime = 5.0f;

    [Header("전투 설정")]
    public float attackRange = 2f;
    public float attackDamage = 25f;

    // 내부 상태
    private Rigidbody rb;
    private AgentController enemy;
    private AgentState currentState = AgentState.Idle;

    // 쿨타임 관리
    private float attackCooldown = 0f;
    private float defendCooldown = 0f;
    private float dodgeCooldown = 0f;

    // 행동 락 (Player.cs와 동일)
    private bool isActionPlaying = false;

    // 무적 시간 (Player.cs와 동일)
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float invincibleDuration = 2.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        currentHP = maxHP;
    }

    void Start()
    {
        // 에이전트 초기화 (추후 BT 에이전트 연결 시 사용)
        if (agent != null)
        {
            agent.Initialize(this);
        }
    }

    void Update()
    {
        UpdateCooldowns();
        UpdateInvincibility();

        if (animator != null)
        {
            animator.SetFloat("HP", currentHP / maxHP);
            animator.SetBool("IsAlive", IsAlive());
        }
    }

    void FixedUpdate()
    {
        // 행동 중이면 이동 강제 정지 (Player.cs와 동일)
        if (isActionPlaying)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

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
        transform.position = transform.position; // 위치는 BattleManager에서 설정

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

    #region 행동 실행 (Player.cs 기반)

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

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }

        return ActionResult.Success(ActionType.Idle);
    }

    private ActionResult ExecuteMove(AgentAction action)
    {
        if (isActionPlaying)
            return ActionResult.Failure(action.type, "다른 행동 중");

        Vector3 moveDirection = GetMoveDirection(action.type);
        Vector3 worldDirection = transform.TransformDirection(moveDirection);

        rb.velocity = new Vector3(worldDirection.x * moveSpeed, rb.velocity.y, worldDirection.z * moveSpeed);

        currentState = AgentState.Moving;

        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
            animator.SetFloat("MoveX", moveDirection.x);
            animator.SetFloat("MoveY", moveDirection.z);
        }

        return ActionResult.Success(action.type);
    }

    private ActionResult ExecuteAttack()
    {
        if (attackCooldown > 0f)
            return ActionResult.Failure(ActionType.Attack, "공격 쿨타임");

        attackCooldown = attackCooldownTime;
        currentState = AgentState.Attacking;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        StartActionLock(0.7f); // 공격 모션 시간

        // 공격 판정 (간단한 거리 기반)
        if (enemy != null)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= attackRange)
            {
                enemy.TakeDamage(attackDamage);
                return ActionResult.Success(ActionType.Attack, attackDamage);
            }
        }

        return ActionResult.Success(ActionType.Attack, 0f);
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

        StartActionLock(0.7f); // 방어 모션 시간

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

        StartActionLock(0.7f); // 회피 모션 시간

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
}
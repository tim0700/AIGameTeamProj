using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("������Ʈ ����")]
    public string agentName = "BT Agent";
    public IBattleAgent agent;

    [Header("�̵� ���� (Player.cs ���)")]
    public float moveSpeed = 5f;

    [Header("ü�� ����")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("�ִϸ��̼�")]
    public Animator animator;

    [Header("��Ÿ�� ����")]
    public float attackCooldownTime = 2.5f;
    public float defendCooldownTime = 2.5f;
    public float dodgeCooldownTime = 5.0f;

    [Header("���� ����")]
    public float attackRange = 2f;
    public float attackDamage = 25f;

    // ���� ����
    private Rigidbody rb;
    private AgentController enemy;
    private AgentState currentState = AgentState.Idle;

    // ��Ÿ�� ����
    private float attackCooldown = 0f;
    private float defendCooldown = 0f;
    private float dodgeCooldown = 0f;

    // �ൿ �� (Player.cs�� ����)
    private bool isActionPlaying = false;

    // ���� �ð� (Player.cs�� ����)
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
        // ������Ʈ �ʱ�ȭ (���� BT ������Ʈ ���� �� ���)
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
        // �ൿ ���̸� �̵� ���� ���� (Player.cs�� ����)
        if (isActionPlaying)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    #region ���� �޼��� (BattleManager���� ȣ��)

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

        Debug.Log($"{agentName} ���� ����!");
    }

    public void UpdateAgent(GameObservation observation)
    {
        if (!IsAlive() || agent == null) return;

        // ������Ʈ���� �ൿ ���� ��û
        AgentAction action = agent.DecideAction(observation);

        // �ൿ ����
        ActionResult result = ExecuteAction(action);

        // ����� ������Ʈ���� �뺸
        agent.OnActionResult(result);
    }

    public void OnBattleEnd(EpisodeResult result)
    {
        if (agent != null)
        {
            agent.OnEpisodeEnd(result);
        }

        Debug.Log($"{agentName} ���� ���� - �¸�: {result.won}");
    }

    public void ResetAgent()
    {
        currentHP = maxHP;
        currentState = AgentState.Idle;
        transform.position = transform.position; // ��ġ�� BattleManager���� ����

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }
    }

    #endregion

    #region ���� ��ȸ �޼���

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

    #region �ൿ ���� (Player.cs ���)

    public ActionResult ExecuteAction(AgentAction action)
    {
        if (!IsAlive() || isActionPlaying)
        {
            return ActionResult.Failure(action.type, "�ൿ �Ұ� ����");
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
                return ActionResult.Failure(action.type, "�� �� ���� �ൿ");
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
            return ActionResult.Failure(action.type, "�ٸ� �ൿ ��");

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
            return ActionResult.Failure(ActionType.Attack, "���� ��Ÿ��");

        attackCooldown = attackCooldownTime;
        currentState = AgentState.Attacking;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        StartActionLock(0.7f); // ���� ��� �ð�

        // ���� ���� (������ �Ÿ� ���)
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
            return ActionResult.Failure(ActionType.Defend, "��� ��Ÿ��");

        defendCooldown = defendCooldownTime;
        currentState = AgentState.Defending;

        if (animator != null)
        {
            animator.SetTrigger("Guard");
        }

        StartActionLock(0.7f); // ��� ��� �ð�

        return ActionResult.Success(ActionType.Defend);
    }

    private ActionResult ExecuteDodge()
    {
        if (dodgeCooldown > 0f)
            return ActionResult.Failure(ActionType.Dodge, "ȸ�� ��Ÿ��");

        dodgeCooldown = dodgeCooldownTime;
        currentState = AgentState.Dodging;

        if (animator != null)
        {
            animator.SetTrigger("Dodge");
        }

        StartActionLock(0.7f); // ȸ�� ��� �ð�

        // ȸ�� �� ���� ó��
        isInvincible = true;
        invincibleTimer = 0.7f;

        return ActionResult.Success(ActionType.Dodge);
    }

    #endregion

    #region ��ƿ��Ƽ �޼���

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

        Debug.Log($"{agentName} ���� {damage}, ���� HP: {currentHP}");

        if (!IsAlive())
        {
            currentState = AgentState.Dead;
            Debug.Log($"{agentName} ���!");
        }
    }

    #endregion
}
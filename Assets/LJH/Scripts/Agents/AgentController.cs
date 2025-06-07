using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    [Header("������Ʈ ����")]
    public string agentName = "BT Agent";
    public IBattleAgent agent;

    [Header("�̵� ����")]
    public float moveSpeed = 5f;

    [Header("ü�� ����")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("�ִϸ��̼�")]
    public Animator animator;

    [Header("UI ����")]
    public Slider healthBar;

    [Header("ȸ�� ����")]
    public float rotationSpeed = 10f;

    [Header("��Ÿ�� ����")]
    public float attackCooldownTime = 2.5f;
    public float defendCooldownTime = 2.5f;
    public float dodgeCooldownTime = 5.0f;

    [Header("���� ����")]
    public float attackRange = 2f;
    public float attackDamage = 25f;

    [Header("���� �ý���")]
    public GameObject weaponObject; // ���� ������Ʈ(�ݶ��̴�+isTrigger, �±�="Weapon")
    private Collider weaponCollider;
    private Coroutine attackCoroutine;

    // ���� ����
    private Rigidbody rb;
    private AgentController enemy;
    private AgentState currentState = AgentState.Idle;

    // ��Ÿ�� ����
    private float attackCooldown = 0f;
    private float defendCooldown = 0f;
    private float dodgeCooldown = 0f;

    // �ൿ ��
    private bool isActionPlaying = false;

    // ���� �ð�
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float invincibleDuration = 2.0f;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        currentHP = maxHP;

        // ���� �ݶ��̴� ���� �� �⺻ ��Ȱ��ȭ
        // if (weaponObject != null)
            weaponCollider = weaponObject.GetComponent<Collider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    void Start()
    {
        // ���� GameObject�� �ִ� IBattleAgent ����ü ã��
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
        // �ൿ ���̸� �̵� ���� ����
        if (isActionPlaying)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    #region UI ������Ʈ

    private void UpdateHealthBarUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHP / maxHP;
        }
    }

    #endregion

    #region �ִϸ��̼� ������Ʈ

    private void UpdateMovementAnimation()
    {
        if (animator == null) return;

        // ���� velocity�� ������� �̵� ���� Ȯ��
        Vector3 velocity = rb.velocity;
        bool isMoving = velocity.magnitude > 0.1f;

        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // ���� ��ǥ�� �������� �̵� ���� ���
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

    #region �ൿ ����

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
        return ActionResult.Success(ActionType.Idle);
    }

    private ActionResult ExecuteMove(AgentAction action)
    {
        if (isActionPlaying)
            return ActionResult.Failure(action.type, "�ٸ� �ൿ ��");

        Vector3 moveDirection = GetMoveDirection(action.type);

        // ���� ��ǥ ���� �̵� (ȸ���� ������� ����)
        Vector3 worldMoveDirection = moveDirection;

        rb.velocity = new Vector3(worldMoveDirection.x * moveSpeed, rb.velocity.y, worldMoveDirection.z * moveSpeed);

        // �̵� ������ �ٶ󺸵��� ȸ��
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
            return ActionResult.Failure(ActionType.Attack, "���� ��Ÿ��");

        // ���� ���� ���� �ٶ󺸵��� ȸ��
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

        StartActionLock(0.7f); // ���� ��� �ð�

        // LYD �ý���: ���� �ִϸ��̼� �ð� ���ȸ� ���� �ݶ��̴� Ȱ��ȭ
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(EnableWeaponColliderForDuration(0.7f));

        // // ✅ 여기 추가
        // var result = ActionResult.Success(ActionType.Attack, 0f);
        // result.target = enemy?.gameObject;

        // return result;

        return ActionResult.Success(ActionType.Attack, 0f); // �������� �浹���� ó��

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

        StartActionLock(0.7f);

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

        StartActionLock(0.7f);

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

    // �ε巯�� ȸ���� ���� �ڷ�ƾ
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

        Debug.Log($"{agentName} ���� {damage}, ���� HP: {currentHP}");

        if (!IsAlive())
        {
            currentState = AgentState.Dead;
            Debug.Log($"{agentName} ���!");
        }
    }

    #endregion

    #region ���� �ý��� (LYD ���)

    /// <summary>
    /// ���� �ִϸ��̼� �ð� ���� ���� �ݶ��̴� On/Off (LYD �ý���)
    /// </summary>
    private IEnumerator EnableWeaponColliderForDuration(float duration)
    {
        EnableWeaponCollider();
        // ���� ���� Ÿ�̹�(�ִϸ��̼� �帧�� ���� 0.7~1.0��� ���� ����)
        yield return new WaitForSeconds(duration * 0.7f);
        DisableWeaponCollider();
    }

    /// <summary>
    /// ���� �ݶ��̴� Ȱ��ȭ
    /// </summary>
    private void EnableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = true;
    }

    /// <summary>
    /// ���� �ݶ��̴� ��Ȱ��ȭ
    /// </summary>
    private void DisableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    /// <summary>
    /// ���⿡ ���� �� �ǰ� ���� (LYD �ý���)
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon") && !isInvincible)
        {
            TakeDamage(25f);
            Debug.Log($"{agentName} ���⿡ �ǰ�! ������: 25");
        }
    }

    #endregion

}



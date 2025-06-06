// ✅ PlayerRL.cs (공격/수비형 에이전트 연동을 위한 수정 포함)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.MLAgents;

public class PlayerRL : MonoBehaviour
{
    [Header("이동/입력 세팅")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 3f;

    [Header("애니메이터")]
    public Animator animator;

    [Header("체력")]
    public float maxHP = 100f;
    private float currentHP;
    public Image hpBarImage;
    private RectTransform hpBarRect;
    private float hpBarMaxWidth;

    [Header("스킬 쿨타임 오버레이")]
    public GameObject attackCooldownOverlay;
    public GameObject guardCooldownOverlay;
    public GameObject dodgeCooldownOverlay;

    private RectTransform attackRect;
    private RectTransform guardRect;
    private RectTransform dodgeRect;
    private float cooldownMaxHeight = 100f;

    private Rigidbody rb;
    private float yRotation = 0f;

    private float attackCooldown = 0f;
    private float guardCooldown = 0f;
    private float dodgeCooldown = 0f;

    // 스킬별 쿨타임 타이머
    public float attackCooldownTime = 2.5f;
    public float guardCooldownTime = 2.5f;
    public float dodgeCooldownTime = 5.0f;

    // 모션 락(스킬 동작 중 이동, 입력 불가)
    private bool isActionPlaying = false;

    private const string ATTACK_CLIP = "HumanM@1HAttack01_R";
    private const string GUARD_CLIP  = "HumanM@ShieldAttack01";
    private const string DODGE_CLIP  = "HumanM@Combat_TakeDamage01";

    [Header("무기 오브젝트 (칼 등)")]
    public GameObject weaponObject;
    private Collider weaponCollider;
    private Coroutine attackCoroutine;

    // 피격 후 무적 상태 변수
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float invincibleDuration = 2.0f;

    public PlayerRL opponent; // ✅ 상대 참조

    [Header("스킬 사용 시간 기록 (쿨타임 분석용)")]
    private float lastGuardTime = -999f;
    private float lastDodgeTime = -999f;
    private float lastAttackTime = -999f;

    [Header("외부 접근용: 최근 스킬 사용 시각 (읽기 전용)")]
    public float LastAttackTime => lastAttackTime;
    public float LastGuardTime => lastGuardTime;
    public float LastDodgeTime => lastDodgeTime;



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentHP = maxHP;

        // 체력바 RectTransform, 최대 width 저장
        if (hpBarImage != null)
        {
            hpBarRect = hpBarImage.GetComponent<RectTransform>();
            hpBarMaxWidth = hpBarRect.sizeDelta.x;
        }

        // 각 스킬 쿨타임 오버레이 준비 (초기 비활성화)
        SetupCooldownOverlay(attackCooldownOverlay, out attackRect);
        SetupCooldownOverlay(guardCooldownOverlay, out guardRect);
        SetupCooldownOverlay(dodgeCooldownOverlay, out dodgeRect);


        // 무기 콜라이더 참조 및 기본 비활성화
        if (weaponObject != null)
            weaponCollider = weaponObject.GetComponent<Collider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    /// <summary>
    /// 쿨타임 오버레이 오브젝트 세팅 (비활성화, 높이 초기화)
    /// </summary>
    void SetupCooldownOverlay(GameObject overlayObj, out RectTransform rect)
    {
        rect = null;
        if (overlayObj != null)
        {
            rect = overlayObj.GetComponent<RectTransform>();
            overlayObj.SetActive(false);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, cooldownMaxHeight);
            // RectTransform Pivot.y = 0, Anchor.y = 0(아래 기준)으로 설정
        }
    }

    void Update()
    {
        // 체력바: HP 비율에 따라 width(가로 길이) 조절 (오른쪽→왼쪽으로 줄어듦)
        if (hpBarRect != null)
        {
            float ratio = currentHP / maxHP;
            float newWidth = hpBarMaxWidth * Mathf.Clamp01(ratio);
            hpBarRect.sizeDelta = new Vector2(newWidth, hpBarRect.sizeDelta.y);
        }

        // 각 스킬 쿨타임 오버레이: 높이 줄이기(위→아래로 감소)
        HandleCooldown(ref attackCooldown, attackCooldownTime, attackCooldownOverlay, attackRect);
        HandleCooldown(ref guardCooldown, guardCooldownTime, guardCooldownOverlay, guardRect);
        HandleCooldown(ref dodgeCooldown, dodgeCooldownTime, dodgeCooldownOverlay, dodgeRect);

        // 무적 타이머 갱신
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
                isInvincible = false;
        }

        // 모션 락 중엔 추가 입력/이동 불가
        if (isActionPlaying) return;

        // 마우스 X축 입력으로 캐릭터 좌우(Y축) 회전
        float mouseX = Input.GetAxis("Mouse X");
        yRotation += mouseX * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        // WASD 입력
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        bool isMoving = input.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", input.x);
        animator.SetFloat("MoveY", input.z);

        // 이동 중에도 스킬 사용 가능 (스킬 우선 처리)
        bool usedSkill = false;

        // 공격 입력 (좌클릭)
        if (Input.GetMouseButtonDown(0) && attackCooldown <= 0f)
        {
            RL_Attack();
            usedSkill = true;
        }

        // 방어 입력 (우클릭)
        else if (Input.GetMouseButtonDown(1) && guardCooldown <= 0f)
        {
            RL_Guard();
            usedSkill = true;
        }
        else if (Input.GetKeyDown(KeyCode.E) && dodgeCooldown <= 0f)
        {
            RL_Dodge();
            usedSkill = true;
        }

        // 스킬 입력시 즉시 이동 정지(velocity=0), 이하 입력 무시
        if (usedSkill)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // 이동중이면 추가 입력/처리 없이 종료
        if (isMoving) return;
    }

    void FixedUpdate()
    {
        if (isActionPlaying)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(h, 0, v).normalized;
        Vector3 moveVec = transform.forward * move.z + transform.right * move.x;
        rb.velocity = moveVec * moveSpeed + new Vector3(0, rb.velocity.y, 0);
    }

    /// <summary>
    /// 쿨타임 오버레이 UI 실시간 제어
    /// </summary>
    void HandleCooldown(ref float cooldown, float maxTime, GameObject overlayObj, RectTransform rect)
    {
        if (overlayObj == null || rect == null) return;
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            if (cooldown < 0f) cooldown = 0f;
            float ratio = cooldown / maxTime;
            SetOverlayHeight(rect, cooldownMaxHeight * ratio);

            // 쿨타임 종료시 오버레이 비활성화, 높이 초기화
            if (cooldown <= 0f)
            {
                overlayObj.SetActive(false);
                SetOverlayHeight(rect, cooldownMaxHeight);
            }
        }
    }

    /// <summary>
    /// 오버레이의 높이(y)만 조절 (Pivot.y=0 기준)
    /// </summary>
    void SetOverlayHeight(RectTransform rect, float height)
    {
        if (rect == null) return;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    /// <summary>
    /// 애니메이션 클립 길이(초) 반환 (이름 완전일치 필요)
    /// </summary>
    float GetCurrentAnimationLength(string animClipName)
    {
        var clip = System.Array.Find(animator.runtimeAnimatorController.animationClips, x => x.name == animClipName);
        if (clip != null) return clip.length;
        return 0.7f;
    }

    /// <summary>
    /// 스킬 사용시 입력 및 이동 락
    /// </summary>
    void StartActionLock(float duration)
    {
        isActionPlaying = true;
        Invoke(nameof(EndActionLock), duration);
    }

    void EndActionLock()
    {
        isActionPlaying = false;
    }

    /// <summary>
    /// 공격 애니메이션 시간 동안 무기 콜라이더 On/Off (애니메이션 이벤트 불필요)
    /// </summary>
    IEnumerator EnableWeaponColliderForDuration(float duration)
    {
        EnableWeaponCollider();
        yield return new WaitForSeconds(duration * 0.7f);
        DisableWeaponCollider();
    }

    /// <summary>
    /// 무기 콜라이더 활성화
    /// </summary>
    void EnableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = true;
    }

    /// <summary>
    /// 무기 콜라이더 비활성화
    /// </summary>
    void DisableWeaponCollider()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    /// <summary>
    /// 데미지 처리(무적 중엔 무시), 무적 상태 진입
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        isInvincible = true;
        invincibleTimer = invincibleDuration;
        // 체력바 갱신은 Update에서 자동
    }

    /// <summary>
    /// 무기에 맞을 때 피격 판정(25 데미지)
    /// </summary>
    void OnTriggerEnter(Collider other)
    //공격자가 무기를 휘두르면 피격자의 onTriggerEnter() 호출.
    {
        if (other.CompareTag("Weapon"))
        {
            TakeDamage(25f);

            if (other.transform.root.TryGetComponent<PlayerRL>(out var attacker))
            //위의 if조건은 공격자의 컴포넌트를 찾는 코드
            {
                if (attacker == this) return;

                if (attacker.TryGetComponent<PlayerRLAgent_Attack>(out var atkAgent))
                //공격자 에이전트 객체를 atkAgent로 불러옴.
                {
                    atkAgent.RegisterAttack(25f); //이는 누적데미지나 행동 통계 기록용

                    if (opponent != null) //공격 보상1. 상대가 스킬 쓴 후 쿨 중 공격 성공
                        {
                            float timeSinceGuard = Time.time - opponent.lastGuardTime;
                            float timeSinceDodge = Time.time - opponent.lastDodgeTime;
                            float timeSinceAttack = Time.time - opponent.lastAttackTime;

                        // 스킬 사용 직후 1.0초 이내면, 빈틈을 찌른 것으로 판단
                        if ((timeSinceGuard >= 0f && timeSinceGuard < 1.0f) ||
                            (timeSinceDodge >= 0f && timeSinceDodge < 1.0f) ||
                            (timeSinceAttack >= 0f && timeSinceAttack < 1.0f))
                        {
                            atkAgent.AddReward(+2f); // 빈틈 공격 성공 보상
                            Debug.Log("[Reward1] 상대가 스킬 사용 직후(1초 이내)에 공격 성공");
                            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
                            sr.Add("Attack/Reward/Reward1", 1f, StatAggregationMethod.MostRecent);
                            }
                        }                        
                }

                if (TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
                {
                    defAgent.RegisterDamage(25f); 
                }
            }
        }
    }

    public float CurHP => currentHP;
    public float AttackCD => attackCooldown;
    public float GuardCD => guardCooldown;
    public float DodgeCD => dodgeCooldown;

    public bool CanAttack() => attackCooldown <= 0f && !isActionPlaying;
    public bool CanGuard() => guardCooldown <= 0f && !isActionPlaying;
    public bool CanDodge() => dodgeCooldown <= 0f && !isActionPlaying;

    /* 스킬을 외부에서 호출할 래퍼 */

    public void RL_Attack()
    {
        if (!CanAttack()) return; //공격 쿨타임 중이거나, 모션 락 상태면 공격 불가 → 함수 종료
        lastAttackTime = Time.time; //시간기록


        animator.SetTrigger("Attack");
        attackCooldown = attackCooldownTime; //쿨다운 초기화 2.5초.
        if (attackCooldownOverlay != null)
        {
            attackCooldownOverlay.SetActive(true);
            SetOverlayHeight(attackRect, cooldownMaxHeight);
        }
        StartActionLock(GetCurrentAnimationLength(ATTACK_CLIP));
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(EnableWeaponColliderForDuration(GetCurrentAnimationLength(ATTACK_CLIP)));
        // 일정 시간 동안만 무기 콜라이더가 켜짐 → 이 타이밍에 충돌하면 OnTriggerEnter() 발생

        if (opponent != null && opponent.TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
            defAgent.RegisterOpponentAttack();
        // 방어 에이전트한테 상대가 공격했다는걸 알려줌 → 반격 조건 체크용

        if (TryGetComponent<PlayerRLAgent_Defense>(out var myDefAgent))
            myDefAgent.RegisterCounterAttackSuccess();
        //자기 자신이 방어형 에이전트였다면, 이전에 회피/방어 성공 후 곧바로 공격에 성공한 경우 반격 보상이 있을 수 있음
    }

    public void RL_Guard()
    {
        if (!CanGuard()) return;
        lastGuardTime = Time.time; //시간기록

        animator.SetTrigger("Guard");
        guardCooldown = guardCooldownTime; //방어 쿨타임 2.5초
        if (guardCooldownOverlay != null)
        {
            guardCooldownOverlay.SetActive(true);
            SetOverlayHeight(guardRect, cooldownMaxHeight);
        }
        StartActionLock(GetCurrentAnimationLength(GUARD_CLIP));

        if (TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
            defAgent.RegisterSuccessfulDefense("Guard");
    }

    public void RL_Dodge()
    {
        if (!CanDodge()) return;
        lastDodgeTime = Time.time; //시간기록


        animator.SetTrigger("Dodge");
        dodgeCooldown = dodgeCooldownTime; //회피 쿨타임 5초
        if (dodgeCooldownOverlay != null)
        {
            dodgeCooldownOverlay.SetActive(true);
            SetOverlayHeight(dodgeRect, cooldownMaxHeight);
        }
        StartActionLock(GetCurrentAnimationLength(DODGE_CLIP));

        if (TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
            defAgent.RegisterSuccessfulDefense("Dodge");

        //공격 보상3. 회피 직후 2초 이내 공격 성공, 회피 시도 시간 공격형에게 전달
        if (TryGetComponent<PlayerRLAgent_Attack>(out var atkAgent))
            atkAgent.RegisterDodge(); 
    }

    /* 에피소드 초기화를 위한 리셋 
    강화학습에서 한 번의 학습 에피소드가 끝나고 다음 에피소드를 시작하기 직전에 호출.
    */
    public void ResetStatus()
    {
        currentHP = maxHP;
        attackCooldown = guardCooldown = dodgeCooldown = 0f;
        isActionPlaying = false;
        isInvincible = false;
        invincibleTimer = 0f;
        rb.velocity = Vector3.zero;
        transform.localPosition = Vector3.zero;
    }
}


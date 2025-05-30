using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("이동/입력 세팅")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 3f;

    [Header("애니메이터")]
    public Animator animator;

    [Header("체력")]
    public float maxHP = 100f;
    private float currentHP;
    public Image hpBarImage; // 체력바 이미지 (width 조절용)
    private RectTransform hpBarRect;
    private float hpBarMaxWidth;

    [Header("스킬 쿨타임 오버레이")]
    public GameObject attackCooldownOverlay;
    public GameObject guardCooldownOverlay;
    public GameObject dodgeCooldownOverlay;

    private RectTransform attackRect;
    private RectTransform guardRect;
    private RectTransform dodgeRect;
    private float cooldownMaxHeight = 100f; // 오버레이의 기본 세로 크기(px)

    private Rigidbody rb;
    private float yRotation = 0f;

    // 스킬별 쿨타임 타이머
    private float attackCooldown = 0f;
    private float guardCooldown = 0f;
    private float dodgeCooldown = 0f;

    private float attackCooldownTime = 2.5f;
    private float guardCooldownTime = 2.5f;
    private float dodgeCooldownTime = 5.0f;

    // 모션 락 (스킬 동작 중 이동/입력 불가)
    private bool isActionPlaying = false;

    private const string ATTACK_CLIP = "HumanM@1HAttack01_R";
    private const string GUARD_CLIP  = "HumanM@ShieldAttack01";
    private const string DODGE_CLIP  = "HumanM@Combat_TakeDamage01";

    [Header("무기 오브젝트 (칼 등)")]
    public GameObject weaponObject; // 무기 오브젝트(콜라이더+isTrigger, 태그="Weapon")
    private Collider weaponCollider;
    private Coroutine attackCoroutine;

    // 피격 후 무적 상태 변수
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float invincibleDuration = 2.0f;

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

        // 테스트용: Z키 누르면 데미지 10 (무적 중엔 무시)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TakeDamage(10f);
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
            animator.SetTrigger("Attack");
            attackCooldown = attackCooldownTime;
            if (attackCooldownOverlay != null)
            {
                attackCooldownOverlay.SetActive(true);
                SetOverlayHeight(attackRect, cooldownMaxHeight);
            }
            StartActionLock(GetCurrentAnimationLength(ATTACK_CLIP));
            usedSkill = true;

            // 공격 애니메이션 시간 동안만 무기 콜라이더 활성화
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(EnableWeaponColliderForDuration(GetCurrentAnimationLength(ATTACK_CLIP)));
        }
        // 방어 입력 (우클릭)
        else if (Input.GetMouseButtonDown(1) && guardCooldown <= 0f)
        {
            animator.SetTrigger("Guard");
            guardCooldown = guardCooldownTime;
            if (guardCooldownOverlay != null)
            {
                guardCooldownOverlay.SetActive(true);
                SetOverlayHeight(guardRect, cooldownMaxHeight);
            }
            StartActionLock(GetCurrentAnimationLength(GUARD_CLIP));
            usedSkill = true;
            // 방어는 무기 콜라이더 불필요
        }
        // 회피 입력 (E키)
        else if (Input.GetKeyDown(KeyCode.E) && dodgeCooldown <= 0f)
        {
            animator.SetTrigger("Dodge");
            dodgeCooldown = dodgeCooldownTime;
            if (dodgeCooldownOverlay != null)
            {
                dodgeCooldownOverlay.SetActive(true);
                SetOverlayHeight(dodgeRect, cooldownMaxHeight);
            }
            StartActionLock(GetCurrentAnimationLength(DODGE_CLIP));
            usedSkill = true;
            // 회피는 무기 콜라이더 불필요
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
        // 스킬 모션 중엔 이동 강제 정지
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
        // 없으면 기본값 0.7초로 처리
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
        // 실제 판정 타이밍(애니메이션 흐름에 따라 0.7~1.0배수 조절 가능)
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
        if (isInvincible) return; // 무적 상태면 무시

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
    {
        if (other.CompareTag("Weapon"))
        {
            TakeDamage(25f);
        }
    }
}

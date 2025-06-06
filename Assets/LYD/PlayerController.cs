using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 3f;
    public Animator animator;

    private Rigidbody rb;
    private float yRotation = 0f;

    // 쿨타임 타이머
    private float attackCooldown = 0f;
    private float guardCooldown = 0f;
    private float dodgeCooldown = 0f;

    // 모션 중 여부
    private bool isActionPlaying = false;

    // 실제 애니메이션 클립명 반영
    private const string ATTACK_CLIP = "HumanM@1HAttack01_R";
    private const string GUARD_CLIP  = "HumanM@ShieldAttack01";
    private const string DODGE_CLIP  = "HumanM@Combat_TakeDamage01";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        // 애니메이션 클립명 리스트 디버그 출력
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            Debug.Log("[애니메이션 클립] " + clip.name);
        }
    }

    void Update()
    {
        // [1] 마우스 좌우 회전
        float mouseX = Input.GetAxis("Mouse X");
        yRotation += mouseX * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        // [2] 쿨타임 갱신
        if (attackCooldown > 0) attackCooldown -= Time.deltaTime;
        if (guardCooldown > 0) guardCooldown -= Time.deltaTime;
        if (dodgeCooldown > 0) dodgeCooldown -= Time.deltaTime;

        // [3] 모션 중엔 입력 불가
        if (isActionPlaying) return;

        // [4] WASD 입력
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        bool isMoving = input.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", input.x);
        animator.SetFloat("MoveY", input.z);

        // [5] 이동 중에는 액션 불가
        if (isMoving) return;

        // [6] 공격 (좌클릭, 쿨타임 2.5초)
        if (Input.GetMouseButtonDown(0) && attackCooldown <= 0f)
        {
            animator.SetTrigger("Attack");
            attackCooldown = 2.5f;
            StartActionLock(GetCurrentAnimationLength(ATTACK_CLIP));
        }
        // [7] 방어 (우클릭, 쿨타임 2.5초)
        else if (Input.GetMouseButtonDown(1) && guardCooldown <= 0f)
        {
            animator.SetTrigger("Guard");
            guardCooldown = 2.5f;
            StartActionLock(GetCurrentAnimationLength(GUARD_CLIP));
        }
        // [8] 회피 (E, 쿨타임 5초)
        else if (Input.GetKeyDown(KeyCode.E) && dodgeCooldown <= 0f)
        {
            animator.SetTrigger("Dodge");
            dodgeCooldown = 5.0f;
            StartActionLock(GetCurrentAnimationLength(DODGE_CLIP));
        }
    }

    void FixedUpdate()
    {
        // 모션 중엔 이동 불가
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

    // 애니메이션 길이 가져오기 (클립명 완전 일치 필요)
    float GetCurrentAnimationLength(string animClipName)
    {
        var clip = System.Array.Find(animator.runtimeAnimatorController.animationClips, x => x.name == animClipName);
        if (clip != null) return clip.length;
        Debug.LogWarning("[애니메이션 클립 미존재] " + animClipName + " → 기본값(0.7초) 적용");
        return 0.7f;
    }

    // 액션 중 입력 락
    void StartActionLock(float duration)
    {
        isActionPlaying = true;
        Invoke(nameof(EndActionLock), duration);
    }

    void EndActionLock()
    {
        isActionPlaying = false;
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 3f;
    public Animator animator;

    private Rigidbody rb;
    private float yRotation = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 마우스 좌우 회전
        float mouseX = Input.GetAxis("Mouse X");
        yRotation += mouseX * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        // WASD 입력
        int direction = GetDirectionFromKeys();
        bool isMoving = direction != -1;

        animator.SetBool("IsMoving", isMoving);
        animator.SetInteger("Direction", isMoving ? direction : 0); // Idle은 Front(0)로 대체(원하면 Idle State 따로 추가)

        // FixedUpdate 이동 처리용으로 따로 입력 저장하면 좋음
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(h, 0, v).normalized;
        Vector3 moveVec = transform.forward * move.z + transform.right * move.x;
        rb.velocity = moveVec * moveSpeed + new Vector3(0, rb.velocity.y, 0);
    }

    // 8방향: 0=Front(W), 1=FrontRight(W+D), 2=Right(D), 3=BackRight(S+D), 4=Back(S), 5=BackLeft(S+A), 6=Left(A), 7=FrontLeft(W+A)
    int GetDirectionFromKeys()
    {
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);

        if (w && d && !a && !s) return 1; // FrontRight
        if (w && a && !d && !s) return 7; // FrontLeft
        if (s && d && !w && !a) return 3; // BackRight
        if (s && a && !w && !d) return 5; // BackLeft
        if (w && !a && !d && !s) return 0; // Front
        if (s && !a && !d && !w) return 4; // Back
        if (d && !w && !s && !a) return 2; // Right
        if (a && !w && !s && !d) return 6; // Left

        // 대각선 우선, 중복/입력없음은 Idle(-1)
        return -1;
    }
}

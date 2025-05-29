using UnityEngine;
using System.Collections;

namespace BehaviorTree
{
    public class Agent : MonoBehaviour
    {
        [Header("Agent Stats")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;

        [Header("Combat Settings")]
        public float attackRange = 2f;
        public float attackDamage = 20f;
        public float attackCooldown = 2.5f;
        public float defenseCooldown = 2.5f;
        public float dodgeCooldown = 5f;
        public float dodgeDuration = 0.5f;
        public float dodgeDistance = 3f;

        [Header("Detection")]
        public float detectionRange = 10f;
        public LayerMask enemyLayer = 1;
        public LayerMask obstacleLayer = 1 << 8; // Default to layer 8 for obstacles
        public float obstacleDetectionDistance = 1.5f;

        // 컴포넌트들
        private Rigidbody rb;
        private Animator animator;
        private Collider col;

        // 상태 관리
        public bool isAttacking { get; private set; }
        public bool isDefending { get; private set; }
        public bool isDodging { get; private set; }
        public bool isDead { get; private set; }

        // 쿨타임 관리
        private float lastAttackTime;
        private float lastDefenseTime;
        private float lastDodgeTime;

        // 타겟 정보
        public Transform currentTarget { get; private set; }

        // 시각적 피드백
        private Renderer visualRenderer;
        private Color originalColor;
        private Material originalMaterial;
        private bool feedbackSystemReady = false;
        private Coroutine colorCoroutine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>(); // null일 수 있음
            col = GetComponent<Collider>();
            
            currentHealth = maxHealth;
        }
        
        private void Start()
        {
            // 시각적 피드백 초기화 - Start에서 수행하여 모든 컴포넌트가 준비된 후 실행
            StartCoroutine(InitializeVisualFeedback());
        }
        
        private IEnumerator InitializeVisualFeedback()
        {
            yield return new WaitForSeconds(0.1f); // 짧은 대기
            
            visualRenderer = GetComponentInChildren<Renderer>();
            if (visualRenderer != null)
            {
                // Material을 복사해서 사용
                originalMaterial = visualRenderer.material;
                Material instanceMaterial = new Material(originalMaterial);
                visualRenderer.material = instanceMaterial;
                originalColor = visualRenderer.material.color;
                feedbackSystemReady = true;
                Debug.Log($"{gameObject.name}: Visual feedback system initialized. Original color: {originalColor}");
            }
        }

        #region 이동 관련
        public void MoveTo(Vector3 direction)
        {
            // 시퀀셜 액션: 공격, 방어, 회피 중에는 이동 불가
            if (isDodging || isDead || isAttacking || isDefending) return;

            // 4방향 이동으로 제한
            Vector3 constrainedDirection = ConstrainToFourDirections(direction);
            if (constrainedDirection == Vector3.zero) 
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                if (animator != null)
                    animator.SetBool("IsMoving", false);
                return;
            }

            // 장애물 감지 및 회피
            Vector3 finalDirection = GetSmartMovementDirection(constrainedDirection);
            if (finalDirection == Vector3.zero)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                if (animator != null)
                    animator.SetBool("IsMoving", false);
                return;
            }

            Vector3 moveVec = finalDirection * moveSpeed;
            rb.velocity = new Vector3(moveVec.x, rb.velocity.y, moveVec.z);

            // 애니메이션 (Animator가 있을 때만)
            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
            }

            LookAtDirection(finalDirection);
        }
        
        // 4방향으로 제한하는 함수
        private Vector3 ConstrainToFourDirections(Vector3 input)
        {
            if (input.magnitude < 0.1f) return Vector3.zero;
            
            // 더 큰 축을 선택
            if (Mathf.Abs(input.x) > Mathf.Abs(input.z))
            {
                // 좌/우 방향
                return new Vector3(Mathf.Sign(input.x), 0, 0);
            }
            else
            {
                // 앞/뒤 방향
                return new Vector3(0, 0, Mathf.Sign(input.z));
            }
        }

        public void LookAtDirection(Vector3 direction)
        {
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        // 장애물을 피한 스마트한 이동 방향 결정
        private Vector3 GetSmartMovementDirection(Vector3 desiredDirection)
        {
            // 원하는 방향에 장애물이 있는지 확인
            if (!IsObstacleInDirection(desiredDirection))
            {
                return desiredDirection;
            }
            
            // 장애물이 있다면 대체 경로 찾기
            Vector3[] alternativeDirections = GetAlternativeDirections(desiredDirection);
            
            foreach (Vector3 altDirection in alternativeDirections)
            {
                if (!IsObstacleInDirection(altDirection))
                {
                    Debug.Log($"{gameObject.name}: Obstacle detected, moving {altDirection} instead");
                    return altDirection;
                }
            }
            
            // 모든 방향이 막혀있으면 정지
            Debug.Log($"{gameObject.name}: All directions blocked!");
            return Vector3.zero;
        }
        
        // 특정 방향에 장애물이 있는지 확인
        private bool IsObstacleInDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return false;
            
            // Raycast로 장애물 감지
            Vector3 origin = transform.position + Vector3.up * 0.5f; // 중심점을 약간 높임
            RaycastHit hit;
            
            if (Physics.Raycast(origin, direction, out hit, obstacleDetectionDistance, obstacleLayer))
            {
                return true;
            }
            
            // 추가로 BoxCast로 더 넓은 영역 체크
            Vector3 boxSize = new Vector3(0.5f, 1f, 0.5f);
            if (Physics.BoxCast(origin, boxSize * 0.5f, direction, out hit, transform.rotation, obstacleDetectionDistance, obstacleLayer))
            {
                return true;
            }
            
            return false;
        }
        
        // 대체 이동 방향 찾기
        private Vector3[] GetAlternativeDirections(Vector3 blockedDirection)
        {
            Vector3[] alternatives = new Vector3[3];
            
            if (blockedDirection == Vector3.forward || blockedDirection == Vector3.back)
            {
                // 전/후 방향이 막혔으면 좌/우 시도
                alternatives[0] = Vector3.right;
                alternatives[1] = Vector3.left;
                alternatives[2] = -blockedDirection; // 반대 방향
            }
            else // left or right blocked
            {
                // 좌/우 방향이 막혔으면 전/후 시도
                alternatives[0] = Vector3.forward;
                alternatives[1] = Vector3.back;
                alternatives[2] = -blockedDirection; // 반대 방향
            }
            
            // 타겟과의 거리를 고려하여 우선순위 정렬
            if (currentTarget != null)
            {
                System.Array.Sort(alternatives, (a, b) => 
                {
                    float distA = Vector3.Distance(transform.position + a * 2f, currentTarget.position);
                    float distB = Vector3.Distance(transform.position + b * 2f, currentTarget.position);
                    return distA.CompareTo(distB);
                });
            }
            
            return alternatives;
        }

        public void LookAtTarget(Transform target)
        {
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0; // Y축 회전만
                LookAtDirection(direction);
            }
        }
        #endregion

        #region 전투 관련
        public bool CanAttack()
        {
            return Time.time >= lastAttackTime + attackCooldown && !isAttacking && !isDodging && !isDead;
        }

        public bool CanDefend()
        {
            return Time.time >= lastDefenseTime + defenseCooldown && !isDefending && !isDodging && !isDead;
        }

        public bool CanDodge()
        {
            return Time.time >= lastDodgeTime + dodgeCooldown && !isDodging && !isDead;
        }

        public void Attack()
        {
            if (!CanAttack()) return;

            StartCoroutine(PerformAttack());
        }

        public void Defend()
        {
            if (!CanDefend()) return;

            StartCoroutine(PerformDefense());
        }

        public void Dodge(Vector3 direction)
        {
            if (!CanDodge()) return;

            StartCoroutine(PerformDodge(direction));
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            
            ShowAttackFeedback(); // 시각적 피드백

            if (animator != null)
                animator.SetTrigger("Attack");
            
            // 공격 범위 내 적에게 데미지
            yield return new WaitForSeconds(0.3f); // 공격 애니메이션 타이밍
            
            // 더 정확한 공격 판정을 위해 타겟 직접 확인
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
                if (distanceToTarget <= attackRange)
                {
                    Agent targetAgent = currentTarget.GetComponent<Agent>();
                    if (targetAgent != null && !targetAgent.isDead)
                    {
                        targetAgent.TakeDamage(attackDamage);
                        Debug.Log($"{gameObject.name} hit {targetAgent.gameObject.name} for {attackDamage} damage!");
                    }
                }
            }
            
            // 추가로 주변 적들도 체크 (범위 공격)
            Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            foreach (Collider enemy in enemies)
            {
                if (enemy.transform == currentTarget) continue; // 이미 처리함
                
                Agent enemyAgent = enemy.GetComponent<Agent>();
                if (enemyAgent != null && enemyAgent != this && !enemyAgent.isDead)
                {
                    float angle = Vector3.Angle(transform.forward, enemy.transform.position - transform.position);
                    if (angle < 45f) // 전방 45도 이내의 적만
                    {
                        enemyAgent.TakeDamage(attackDamage * 0.5f); // 부가 대상은 절반 데미지
                        Debug.Log($"{gameObject.name} (splash) hit {enemyAgent.gameObject.name} for {attackDamage * 0.5f} damage!");
                    }
                }
            }

            yield return new WaitForSeconds(0.5f); // 공격 후 딜레이
            isAttacking = false;
        }

        private IEnumerator PerformDefense()
        {
            isDefending = true;
            lastDefenseTime = Time.time;
            
            ShowDefenseFeedback(); // 시각적 피드백

            if (animator != null)
                animator.SetBool("IsDefending", true);
            
            yield return new WaitForSeconds(1f); // 방어 지속 시간
            
            if (animator != null)
                animator.SetBool("IsDefending", false);
            
            // 방어 종료 시 색상 복원
            if (visualRenderer != null && colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                visualRenderer.material.color = originalColor;
                colorCoroutine = null;
            }
            
            isDefending = false;
        }

        private IEnumerator PerformDodge(Vector3 direction)
        {
            isDodging = true;
            lastDodgeTime = Time.time;
            
            ShowDodgeFeedback(); // 시각적 피드백

            if (animator != null)
                animator.SetTrigger("Dodge");
            
            // 4방향으로 제한된 회피 방향
            Vector3 constrainedDirection = ConstrainToFourDirections(direction);
            if (constrainedDirection == Vector3.zero)
            {
                // 방향이 없으면 뒤로 회피
                constrainedDirection = -transform.forward;
            }
            
            // 무적 상태 + 빠른 이동
            col.enabled = false;
            Vector3 dodgeTarget = transform.position + constrainedDirection * dodgeDistance;
            
            float elapsedTime = 0;
            Vector3 startPos = transform.position;
            
            while (elapsedTime < dodgeDuration)
            {
                transform.position = Vector3.Lerp(startPos, dodgeTarget, elapsedTime / dodgeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            col.enabled = true;
            isDodging = false;
        }

        public void TakeDamage(float damage)
        {
            if (isDodging || isDead) return;

            // 방어 중이면 데미지 무효화
            if (isDefending)
            {
                Debug.Log($"{gameObject.name} BLOCKED ATTACK from {damage} damage!");
                ChangeColor(Color.green, 0.5f); // 방어 성공 시 초록색
                return;
            }

            currentHealth -= damage;
            ShowDamageFeedback(); // 시각적 피드백
            
            if (animator != null)
                animator.SetTrigger("TakeDamage");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0;
            if (animator != null)
                animator.SetTrigger("Death");
            
            // 게임 종료 로직
            Debug.Log($"{gameObject.name} 사망!");
        }
        #endregion

        #region 감지 관련
        public Transform FindNearestEnemy()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider enemy in enemies)
            {
                if (enemy.transform == transform) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy.transform;
                }
            }

            currentTarget = nearest;
            return nearest;
        }

        public float GetDistanceToTarget()
        {
            if (currentTarget == null) return float.MaxValue;
            return Vector3.Distance(transform.position, currentTarget.position);
        }

        public bool IsTargetInAttackRange()
        {
            return GetDistanceToTarget() <= attackRange;
        }
        #endregion

        #region 정보 접근자
        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        public Vector3 GetDirectionToTarget()
        {
            if (currentTarget == null) return Vector3.zero;
            return (currentTarget.position - transform.position).normalized;
        }
        #endregion

        #region 시각적 피드백
        private void ChangeColor(Color color, float duration = 0.5f)
        {
            if (!feedbackSystemReady || visualRenderer == null) 
            {
                Debug.LogWarning($"{gameObject.name}: Visual feedback system not ready");
                return;
            }
            
            if (colorCoroutine != null)
                StopCoroutine(colorCoroutine);
                
            colorCoroutine = StartCoroutine(ColorChangeCoroutine(color, duration));
        }
        
        private IEnumerator ColorChangeCoroutine(Color targetColor, float duration)
        {
            // Material 색상 직접 설정
            if (visualRenderer != null && visualRenderer.material != null)
            {
                // 색상 설정
                visualRenderer.material.color = targetColor;
                visualRenderer.material.SetColor("_Color", targetColor); // Shader에 따라 필요할 수 있음
                Debug.Log($"{gameObject.name}: Color changed to {targetColor}");
                
                yield return new WaitForSeconds(duration);
                
                // 원래 색상으로 복원
                visualRenderer.material.color = originalColor;
                visualRenderer.material.SetColor("_Color", originalColor);
                Debug.Log($"{gameObject.name}: Color restored to {originalColor}");
            }
            
            colorCoroutine = null;
        }
        
        public void ShowAttackFeedback()
        {
            ChangeColor(Color.yellow, 1f); // 더 오래 지속
            Debug.Log($"{gameObject.name}: Attack feedback!");
        }
        
        public void ShowDefenseFeedback()
        {
            ChangeColor(Color.cyan, 2f); // 방어는 더 오래
            Debug.Log($"{gameObject.name}: Defense feedback!");
        }
        
        public void ShowDodgeFeedback()
        {
            ChangeColor(Color.magenta, 1f);
            Debug.Log($"{gameObject.name}: Dodge feedback!");
        }
        
        public void ShowDamageFeedback()
        {
            ChangeColor(Color.red, 0.8f); // 빨간색으로 변경
            Debug.Log($"{gameObject.name}: Damage taken!");
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 감지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 장애물 감지 범위
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            // 4방향 장애물 감지 레이
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (Vector3 dir in directions)
            {
                Gizmos.DrawRay(origin, dir * obstacleDetectionDistance);
            }
            
            // 박스 감지 영역
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Vector3 boxSize = new Vector3(0.5f, 1f, 0.5f);
            Gizmos.DrawCube(transform.position + transform.forward * obstacleDetectionDistance * 0.5f, boxSize);
        }
    }
}

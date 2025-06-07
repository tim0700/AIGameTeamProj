using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 아레나 경계를 고려한 안전한 순찰 노드
    /// 안전 구역 내에서만 순찰 목표를 설정
    /// </summary>
    public class SafePatrolNode : BTNode
    {
        [Header("순찰 설정")]
        public float patrolRadius = 3f;          // 순찰 반경
        public float safeZoneRadius = 0.75f;     // 아레나 반지름의 75% 내에서만 순찰
        public float reachThreshold = 0.5f;      // 목표 도달 판정 거리
        public float waitTime = 1f;              // 순찰 지점 도착 후 대기 시간
        
        private Vector3 patrolTarget;
        private bool isMovingToTarget = false;
        private float waitTimer = 0f;
        private bool isWaiting = false;
        private int consecutiveFailures = 0;
        private const int maxFailures = 3;

        public SafePatrolNode() { }

        public SafePatrolNode(float radius, float safeZone = 0.75f)
        {
            patrolRadius = radius;
            safeZoneRadius = safeZone;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 대기 중인 경우
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    isMovingToTarget = false;
                    return NodeState.Success;
                }
                return NodeState.Running;
            }

            // 새로운 순찰 목표가 필요한 경우
            if (!isMovingToTarget || !IsTargetSafe(observation))
            {
                SetSafePatrolTarget(observation);
                isMovingToTarget = true;
                consecutiveFailures = 0;
            }

            // 목표까지의 거리 확인
            float distanceToTarget = Vector3.Distance(observation.selfPosition, patrolTarget);
            
            // 목표 도달
            if (distanceToTarget < reachThreshold)
            {
                Debug.Log($"[SafePatrol] 순찰 지점 도달! 대기 시작");
                isWaiting = true;
                waitTimer = waitTime;
                return NodeState.Running;
            }

            // 목표로 이동
            Vector3 moveDirection = (patrolTarget - observation.selfPosition).normalized;
            moveDirection.y = 0; // 평면 이동
            
            AgentAction moveAction = AgentAction.Move(moveDirection);
            
            if (agentController != null)
            {
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (!result.success)
                {
                    consecutiveFailures++;
                    Debug.LogWarning($"[SafePatrol] 이동 실패: {result.message}");
                    
                    // 연속 실패 시 새로운 목표 설정
                    if (consecutiveFailures >= maxFailures)
                    {
                        Debug.Log("[SafePatrol] 연속 실패로 새 목표 설정");
                        isMovingToTarget = false;
                        return NodeState.Failure;
                    }
                }
                else
                {
                    consecutiveFailures = 0;
                }
            }

            return NodeState.Running;
        }

        /// <summary>
        /// 안전한 순찰 목표 설정
        /// </summary>
        private void SetSafePatrolTarget(GameObservation observation)
        {
            int attempts = 0;
            const int maxAttempts = 10;
            
            do
            {
                // 현재 위치 기준으로 랜덤한 순찰 지점 생성
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.5f, patrolRadius);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );
                
                patrolTarget = observation.selfPosition + offset;
                attempts++;
                
            } while (!IsPositionSafe(patrolTarget, observation) && attempts < maxAttempts);

            // 안전한 위치를 찾지 못한 경우 아레나 중심 방향으로 이동
            if (attempts >= maxAttempts)
            {
                Vector3 directionToCenter = (observation.arenaCenter - observation.selfPosition).normalized;
                float safeDistance = observation.arenaRadius * safeZoneRadius * 0.8f;
                patrolTarget = observation.arenaCenter + directionToCenter * safeDistance;
                
                Debug.LogWarning("[SafePatrol] 안전한 순찰 지점을 찾지 못해 중심 방향으로 설정");
            }
            
            Debug.Log($"[SafePatrol] 새 순찰 목표 설정: {patrolTarget}");
        }

        /// <summary>
        /// 특정 위치가 안전 구역 내에 있는지 확인
        /// </summary>
        private bool IsPositionSafe(Vector3 position, GameObservation observation)
        {
            float distanceFromCenter = Vector3.Distance(position, observation.arenaCenter);
            float safeZoneActualRadius = observation.arenaRadius * safeZoneRadius;
            
            return distanceFromCenter <= safeZoneActualRadius;
        }

        /// <summary>
        /// 현재 목표가 여전히 안전한지 확인
        /// </summary>
        private bool IsTargetSafe(GameObservation observation)
        {
            return IsPositionSafe(patrolTarget, observation);
        }

        /// <summary>
        /// 노드 초기화
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            isMovingToTarget = false;
            isWaiting = false;
            consecutiveFailures = 0;
        }

        /// <summary>
        /// 디버그용 시각화
        /// </summary>
        public void DrawDebugGizmos(Vector3 arenaCenter, float arenaRadius)
        {
            // 안전 구역 표시
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 반투명 녹색
            int segments = 64;
            float angleStep = 360f / segments;
            float safeRadius = arenaRadius * safeZoneRadius;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 point1 = arenaCenter + new Vector3(Mathf.Cos(angle1) * safeRadius, 0, Mathf.Sin(angle1) * safeRadius);
                Vector3 point2 = arenaCenter + new Vector3(Mathf.Cos(angle2) * safeRadius, 0, Mathf.Sin(angle2) * safeRadius);

                Gizmos.DrawLine(point1, point2);
            }

            // 순찰 목표 표시
            if (isMovingToTarget)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(patrolTarget, 0.3f);
                
                // 현재 위치에서 목표까지 선
                if (agentController != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(agentController.transform.position, patrolTarget);
                }
            }
        }
    }
}

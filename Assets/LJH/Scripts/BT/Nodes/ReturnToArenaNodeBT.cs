using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 아레나 중심으로 복귀하는 노드
    /// 에이전트가 경계에 너무 가까이 있을 때 안전 지역으로 이동
    /// </summary>
    public class ReturnToArenaNodeBT : BTNode
    {
        [Header("복귀 설정")]
        public float safeDistance = 0.7f;    // 목표: 아레나 반지름의 70% 지점
        public float moveSpeedMultiplier = 1.2f; // 일반 이동보다 빠르게
        public float reachThreshold = 0.5f;  // 목표 도달 판정 거리

        private Vector3 targetPosition;
        private bool isReturning = false;
        private float returnStartTime;
        private float maxReturnTime = 5f; // 최대 복귀 시간 (무한 루프 방지)

        public ReturnToArenaNodeBT() { }

        public ReturnToArenaNodeBT(float safe, float speedMult = 1.2f)
        {
            safeDistance = safe;
            moveSpeedMultiplier = speedMult;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 현재 아레나 중심으로부터의 거리
            float currentDistance = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = currentDistance / observation.arenaRadius;

            // 이미 안전 지역에 있다면 성공
            if (normalizedDistance <= safeDistance)
            {
                if (isReturning)
                {
                    Debug.Log($"[ReturnToArena] 안전 지역 도달! Current: {normalizedDistance:F2} <= Target: {safeDistance}");
                    isReturning = false;
                }
                return NodeState.Success;
            }

            // 복귀 시작
            if (!isReturning)
            {
                isReturning = true;
                returnStartTime = Time.time;
                Debug.Log($"[ReturnToArena] 복귀 시작! Current distance: {normalizedDistance:F2}");
            }

            // 시간 초과 체크 (무한 루프 방지)
            if (Time.time - returnStartTime > maxReturnTime)
            {
                Debug.LogWarning("[ReturnToArena] 복귀 시간 초과! 실패 처리");
                isReturning = false;
                return NodeState.Failure;
            }

            // 아레나 중심 방향 계산
            Vector3 directionToCenter = (observation.arenaCenter - observation.selfPosition).normalized;

            // 목표 위치 계산 (중심에서 안전 거리만큼 떨어진 현재 방향의 지점)
            // 에이전트의 현재 방향을 고려하여 자연스러운 복귀 경로 설정
            Vector3 currentDirection = observation.selfPosition - observation.arenaCenter;
            currentDirection.y = 0; // Y축 제거 (평면 이동)
            currentDirection.Normalize();

            // 안전 지역 내의 목표 지점
            targetPosition = observation.arenaCenter + currentDirection * (observation.arenaRadius * safeDistance * 0.9f);

            // 목표까지의 거리
            float distanceToTarget = Vector3.Distance(observation.selfPosition, targetPosition);

            // 목표 도달 확인
            if (distanceToTarget < reachThreshold)
            {
                Debug.Log("[ReturnToArena] 목표 지점 도달!");
                isReturning = false;
                return NodeState.Success;
            }

            // 이동 방향 계산 (직접 중심으로 가는 것이 아닌 목표 지점으로)
            Vector3 moveDirection = (targetPosition - observation.selfPosition).normalized;
            moveDirection.y = 0; // 평면 이동 보장

            // 이동 실행
            AgentAction moveAction = AgentAction.Move(moveDirection);
            
            // AgentController가 없는 경우를 대비한 체크
            if (agentController != null)
            {
                // 빠른 이동을 위해 직접 실행
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    Debug.Log($"[ReturnToArena] 이동 중... Distance to safe zone: {normalizedDistance:F2}");
                }
                else
                {
                    Debug.LogWarning($"[ReturnToArena] 이동 실패: {result.message}");
                }
            }

            return NodeState.Running; // 아직 이동 중
        }

        /// <summary>
        /// 노드 초기화 시 상태 리셋
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            isReturning = false;
        }

        /// <summary>
        /// 시각적 디버깅을 위한 목표 위치 표시
        /// </summary>
        public void DrawDebugTarget()
        {
            if (isReturning)
            {
                // 목표 위치에 구체 표시
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);

                // 현재 위치에서 목표까지 선 그리기
                if (agentController != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(agentController.transform.position, targetPosition);
                }
            }
        }

        /// <summary>
        /// 안전 지역 경계선 표시 (디버깅용)
        /// </summary>
        public void DrawSafeZone(Vector3 arenaCenter, float arenaRadius)
        {
            Gizmos.color = Color.green;
            int segments = 64;
            float angleStep = 360f / segments;
            float safeRadius = arenaRadius * safeDistance;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 point1 = arenaCenter + new Vector3(Mathf.Cos(angle1) * safeRadius, 0, Mathf.Sin(angle1) * safeRadius);
                Vector3 point2 = arenaCenter + new Vector3(Mathf.Cos(angle2) * safeRadius, 0, Mathf.Sin(angle2) * safeRadius);

                Gizmos.DrawLine(point1, point2);
            }
        }
    }
}


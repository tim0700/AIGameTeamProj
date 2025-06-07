using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적과 일정 거리를 유지하는 스마트한 노드 (아레나 경계 고려한 측면 이동)
    /// </summary>
    public class MaintainDistanceNode : BTNode
    {
        private float preferredDistance;
        private float tolerance;
        private float lastMoveTime = 0f;
        private Vector3 lastMoveDirection = Vector3.zero;

        public MaintainDistanceNode(float preferredDistance = 4f, float tolerance = 1f)
        {
            this.preferredDistance = preferredDistance;
            this.tolerance = tolerance;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            float currentDistance = observation.distanceToEnemy;
            
            // 적절한 거리에 있으면 성공
            if (Mathf.Abs(currentDistance - preferredDistance) <= tolerance)
            {
                state = NodeState.Success;
                return state;
            }

            Vector3 moveDirection = CalculateSmartMoveDirection(observation);

            if (agentController != null)
            {
                AgentAction moveAction = AgentAction.Move(moveDirection);
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    lastMoveDirection = moveDirection;
                    lastMoveTime = Time.time;
                    state = NodeState.Running;
                    //Debug.Log($"{agentController.GetAgentName()} 스마트 거리 유지 이동: {moveDirection}");
                }
                else
                {
                    state = NodeState.Failure;
                    //Debug.LogWarning($"{agentController.GetAgentName()} 거리 유지 이동 실패");
                }
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }

        /// <summary>
        /// 아레나 경계를 고려한 스마트한 이동 방향 계산
        /// </summary>
        private Vector3 CalculateSmartMoveDirection(GameObservation observation)
        {
            Vector3 toEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            Vector3 toCenter = (observation.arenaCenter - observation.selfPosition).normalized;
            
            float currentDistance = observation.distanceToEnemy;
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float arenaUsageRatio = distanceFromCenter / observation.arenaRadius;

            // 거리 조정이 필요한가?
            bool needToMoveAway = currentDistance < preferredDistance;
            bool needToMoveCloser = currentDistance > preferredDistance + tolerance;

            // 아레나 외각에 너무 가까운가? (80% 이상)
            bool nearArenaEdge = arenaUsageRatio > 0.8f;

            Vector3 moveDirection = Vector3.zero;

            if (needToMoveAway)
            {
                if (nearArenaEdge)
                {
                    // 🎯 핵심 해결책: 외각 근처에서는 측면 이동!
                    moveDirection = GetLateralMovementDirection(toEnemy, toCenter);
                    Debug.Log($"[스마트 이동] 외각 근처 - 측면 이동");
                }
                else
                {
                    // 내부에서는 후퇴하되 중심 방향 고려
                    moveDirection = Vector3.Lerp(-toEnemy, toCenter, 0.3f).normalized;
                    Debug.Log($"[스마트 이동] 내부 - 중심 고려 후퇴");
                }
            }
            else if (needToMoveCloser)
            {
                // 접근이 필요하면 적 방향으로
                moveDirection = toEnemy;
                Debug.Log($"[스마트 이동] 거리 접근");
            }
            else
            {
                // 적절한 거리 - 측면으로 살짝 이동하여 예측 불가능성 추가
                moveDirection = GetLateralMovementDirection(toEnemy, toCenter);
                Debug.Log($"[스마트 이동] 적정 거리 - 측면 조정");
            }

            // Y축 제거 (평면 이동)
            moveDirection.y = 0;
            return moveDirection.normalized;
        }

        /// <summary>
        /// 측면 이동 방향 계산 (아레나 중심을 고려)
        /// </summary>
        private Vector3 GetLateralMovementDirection(Vector3 toEnemy, Vector3 toCenter)
        {
            // 적 방향 벡터를 90도 회전시켜 좌우 방향 생성
            Vector3 leftDirection = new Vector3(-toEnemy.z, 0, toEnemy.x).normalized;
            Vector3 rightDirection = new Vector3(toEnemy.z, 0, -toEnemy.x).normalized;

            // 아레나 중심에 더 가까워지는 방향 선택
            float leftScore = Vector3.Dot(leftDirection, toCenter);
            float rightScore = Vector3.Dot(rightDirection, toCenter);

            Vector3 lateralDirection;
            if (leftScore > rightScore)
            {
                lateralDirection = leftDirection;
                Debug.Log("[측면 이동] 좌측 선택 (중심 방향)");
            }
            else
            {
                lateralDirection = rightDirection;
                Debug.Log("[측면 이동] 우측 선택 (중심 방향)");
            }

            // 약간의 랜덤성 추가 (예측 불가능성)
            if (Time.time - lastMoveTime > 2f) // 2초마다 방향 재평가
            {
                if (Random.Range(0f, 1f) < 0.3f) // 30% 확률로 반대 방향
                {
                    lateralDirection = -lateralDirection;
                    Debug.Log("[측면 이동] 랜덤 방향 변경");
                }
            }

            return lateralDirection;
        }

        /// <summary>
        /// 노드 초기화
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            lastMoveTime = 0f;
            lastMoveDirection = Vector3.zero;
        }
    }
}

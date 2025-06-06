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
        private int consecutiveFailures = 0;
        private const int maxFailures = 3;

        public MaintainDistanceNode(float preferredDistance = 4f, float tolerance = 1f) 
            : base($"MaintainDistance Node ({preferredDistance:F1}±{tolerance:F1})")
        {
            this.preferredDistance = preferredDistance;
            this.tolerance = tolerance;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 적이 죽었으면 거리 유지 불필요
            if (observation.enemyHP <= 0)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 사망하여 거리 유지 불필요");
                
                state = NodeState.Success;
                return state;
            }

            float currentDistance = observation.distanceToEnemy;
            
            // 적절한 거리에 있으면 성공
            if (IsInOptimalRange(currentDistance))
            {
                consecutiveFailures = 0;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 최적 거리 유지 중. 거리: {currentDistance:F2}");
                
                state = NodeState.Success;
                return state;
            }

            Vector3 moveDirection = CalculateSmartMoveDirection(observation);

            AgentAction moveAction = AgentAction.Move(moveDirection);
            ActionResult result = agentController.ExecuteAction(moveAction);
            
            if (result.success)
            {
                lastMoveDirection = moveDirection;
                lastMoveTime = Time.time;
                consecutiveFailures = 0;
                
                state = NodeState.Running;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 스마트 거리 유지 이동: {moveDirection}");
            }
            else
            {
                consecutiveFailures++;
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 거리 유지 이동 실패 ({consecutiveFailures}/{maxFailures}): {result.message}");
                
                // 연속 실패시 전략 변경
                if (consecutiveFailures >= maxFailures)
                {
                    if (enableLogging)
                        Debug.LogWarning($"[{nodeName}] 연속 실패로 포기");
                    
                    state = NodeState.Failure;
                }
                else
                {
                    state = NodeState.Running; // 재시도
                }
            }

            return state;
        }

        #region 거리 유지 로직
        
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
            bool needToMoveAway = currentDistance < (preferredDistance - tolerance);
            bool needToMoveCloser = currentDistance > (preferredDistance + tolerance);

            // 아레나 외각에 너무 가까운가? (80% 이상)
            bool nearArenaEdge = arenaUsageRatio > 0.8f;

            Vector3 moveDirection = Vector3.zero;

            if (needToMoveAway)
            {
                if (nearArenaEdge)
                {
                    // 🎯 핵심 해결책: 외각 근처에서는 측면 이동!
                    moveDirection = GetLateralMovementDirection(toEnemy, toCenter);
                    if (enableLogging)
                        Debug.Log($"[{nodeName}] 외각 근처 - 측면 이동");
                }
                else
                {
                    // 내부에서는 후퇴하되 중심 방향 고려
                    moveDirection = Vector3.Lerp(-toEnemy, toCenter, 0.3f).normalized;
                    if (enableLogging)
                        Debug.Log($"[{nodeName}] 내부 - 중심 고려 후퇴");
                }
            }
            else if (needToMoveCloser)
            {
                // 접근이 필요하면 적 방향으로
                moveDirection = toEnemy;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 거리 접근 필요");
            }
            else
            {
                // 적절한 거리 - 측면으로 살짝 이동하여 예측 불가능성 추가
                moveDirection = GetLateralMovementDirection(toEnemy, toCenter);
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적정 거리 - 측면 조정");
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
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 좌측 이동 선택 (중심 방향)");
            }
            else
            {
                lateralDirection = rightDirection;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 우측 이동 선택 (중심 방향)");
            }

            // 약간의 랜덤성 추가 (예측 불가능성)
            if (Time.time - lastMoveTime > 2f) // 2초마다 방향 재평가
            {
                if (Random.Range(0f, 1f) < 0.3f) // 30% 확률로 반대 방향
                {
                    lateralDirection = -lateralDirection;
                    if (enableLogging)
                        Debug.Log($"[{nodeName}] 랜덤 방향 변경");
                }
            }

            return lateralDirection;
        }

        /// <summary>
        /// 최적 거리 범위에 있는지 확인
        /// </summary>
        private bool IsInOptimalRange(float distance)
        {
            return Mathf.Abs(distance - preferredDistance) <= tolerance;
        }
        
        #endregion
        
        #region 추가 기능들
        
        /// <summary>
        /// 선호 거리 설정
        /// </summary>
        public void SetPreferredDistance(float distance)
        {
            preferredDistance = Mathf.Max(0.5f, distance);
            SetNodeName($"MaintainDistance Node ({preferredDistance:F1}±{tolerance:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 선호 거리 변경: {preferredDistance:F2}");
        }
        
        /// <summary>
        /// 허용 오차 설정
        /// </summary>
        public void SetTolerance(float newTolerance)
        {
            tolerance = Mathf.Max(0.1f, newTolerance);
            SetNodeName($"MaintainDistance Node ({preferredDistance:F1}±{tolerance:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 허용 오차 변경: {tolerance:F2}");
        }
        
        /// <summary>
        /// 현재 선호 거리 반환
        /// </summary>
        public float GetPreferredDistance() => preferredDistance;
        
        /// <summary>
        /// 현재 허용 오차 반환
        /// </summary>
        public float GetTolerance() => tolerance;
        
        /// <summary>
        /// 거리 유지 가능 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CanMaintainDistance(GameObservation observation)
        {
            return CheckBasicConditions() && 
                   observation.enemyHP > 0 && 
                   !IsInOptimalRange(observation.distanceToEnemy);
        }
        
        /// <summary>
        /// 현재 거리가 최적 범위에 있는지 확인
        /// </summary>
        public bool IsAtOptimalDistance(GameObservation observation)
        {
            return IsInOptimalRange(observation.distanceToEnemy);
        }
        
        /// <summary>
        /// 거리 편차 반환 (양수: 너무 가까움, 음수: 너무 멀음)
        /// </summary>
        public float GetDistanceDeviation(GameObservation observation)
        {
            return preferredDistance - observation.distanceToEnemy;
        }
        
        /// <summary>
        /// 거리 유지 품질 점수 (0.0 ~ 1.0)
        /// </summary>
        public float GetDistanceQuality(GameObservation observation)
        {
            float deviation = Mathf.Abs(GetDistanceDeviation(observation));
            if (deviation <= tolerance) return 1f;
            
            // tolerance를 넘으면 점수 감소
            float penalty = (deviation - tolerance) / preferredDistance;
            return Mathf.Clamp01(1f - penalty);
        }
        
        /// <summary>
        /// 이동 전략 분석
        /// </summary>
        public string GetMovementStrategy(GameObservation observation)
        {
            float currentDistance = observation.distanceToEnemy;
            float deviation = GetDistanceDeviation(observation);
            
            if (IsInOptimalRange(currentDistance))
                return "최적거리유지";
            else if (deviation > 0)
                return "후퇴필요";
            else
                return "접근필요";
        }
        
        /// <summary>
        /// 노드 초기화
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            lastMoveTime = 0f;
            lastMoveDirection = Vector3.zero;
            consecutiveFailures = 0;
        }
        
        /// <summary>
        /// 노드 리셋
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            lastMoveTime = 0f;
            lastMoveDirection = Vector3.zero;
            consecutiveFailures = 0;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 선호거리: {preferredDistance:F1}, 허용오차: {tolerance:F1}, 실패: {consecutiveFailures}";
        }
        
        #endregion
    }
}

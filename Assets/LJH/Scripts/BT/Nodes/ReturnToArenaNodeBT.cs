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
        private int consecutiveFailures = 0;
        private const int maxFailures = 3;

        public ReturnToArenaNodeBT() : base("ReturnToArena Node")
        {
        }

        public ReturnToArenaNodeBT(float safe, float speedMult = 1.2f) 
            : base($"ReturnToArena Node (Safe: {safe:F1})")
        {
            safeDistance = safe;
            moveSpeedMultiplier = speedMult;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 현재 아레나 중심으로부터의 거리
            float currentDistance = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = currentDistance / observation.arenaRadius;

            // 이미 안전 지역에 있다면 성공
            if (normalizedDistance <= safeDistance)
            {
                if (isReturning && enableLogging)
                    Debug.Log($"[{nodeName}] 안전 지역 도달! Current: {normalizedDistance:F2} <= Target: {safeDistance}");
                
                isReturning = false;
                consecutiveFailures = 0;
                state = NodeState.Success;
                return state;
            }

            // 복귀 시작
            if (!isReturning)
            {
                isReturning = true;
                returnStartTime = Time.time;
                consecutiveFailures = 0;
                
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 복귀 시작! Current distance: {normalizedDistance:F2}");
            }

            // 시간 초과 체크 (무한 루프 방지)
            if (Time.time - returnStartTime > maxReturnTime)
            {
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 복귀 시간 초과! 실패 처리");
                
                isReturning = false;
                state = NodeState.Failure;
                return state;
            }

            // 목표 위치 계산
            CalculateTargetPosition(observation);

            // 목표까지의 거리
            float distanceToTarget = Vector3.Distance(observation.selfPosition, targetPosition);

            // 목표 도달 확인
            if (distanceToTarget < reachThreshold)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 목표 지점 도달!");
                
                isReturning = false;
                consecutiveFailures = 0;
                state = NodeState.Success;
                return state;
            }

            // 이동 실행
            bool moveSuccess = ExecuteReturnMovement(observation);
            
            if (moveSuccess)
            {
                consecutiveFailures = 0;
                state = NodeState.Running; // 아직 이동 중
            }
            else
            {
                consecutiveFailures++;
                if (consecutiveFailures >= maxFailures)
                {
                    if (enableLogging)
                        Debug.LogWarning($"[{nodeName}] 연속 실패로 복귀 포기");
                    
                    isReturning = false;
                    state = NodeState.Failure;
                }
                else
                {
                    state = NodeState.Running; // 재시도
                }
            }

            return state;
        }

        #region 복귀 로직
        
        /// <summary>
        /// 목표 위치 계산
        /// </summary>
        private void CalculateTargetPosition(GameObservation observation)
        {
            // 아레나 중심 방향 계산
            Vector3 directionToCenter = (observation.arenaCenter - observation.selfPosition).normalized;

            // 목표 위치 계산 (중심에서 안전 거리만큼 떨어진 현재 방향의 지점)
            // 에이전트의 현재 방향을 고려하여 자연스러운 복귀 경로 설정
            Vector3 currentDirection = observation.selfPosition - observation.arenaCenter;
            currentDirection.y = 0; // Y축 제거 (평면 이동)
            currentDirection.Normalize();

            // 안전 지역 내의 목표 지점
            targetPosition = observation.arenaCenter + currentDirection * (observation.arenaRadius * safeDistance * 0.9f);
        }
        
        /// <summary>
        /// 복귀 이동 실행
        /// </summary>
        private bool ExecuteReturnMovement(GameObservation observation)
        {
            // 이동 방향 계산 (직접 중심으로 가는 것이 아닌 목표 지점으로)
            Vector3 moveDirection = (targetPosition - observation.selfPosition).normalized;
            moveDirection.y = 0; // 평면 이동 보장

            // 이동 실행
            AgentAction moveAction = AgentAction.Move(moveDirection);
            ActionResult result = agentController.ExecuteAction(moveAction);
            
            if (result.success)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 복귀 이동 중... Distance to safe zone: {GetNormalizedDistance(observation):F2}");
                
                return true;
            }
            else
            {
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 복귀 이동 실패: {result.message}");
                
                return false;
            }
        }
        
        #endregion
        
        #region 추가 기능들
        
        /// <summary>
        /// 안전 거리 설정
        /// </summary>
        public void SetSafeDistance(float distance)
        {
            safeDistance = Mathf.Clamp01(distance);
            SetNodeName($"ReturnToArena Node (Safe: {safeDistance:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 안전 거리 변경: {safeDistance:F2}");
        }
        
        /// <summary>
        /// 이동 속도 배율 설정
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0.1f, multiplier);
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 속도 배율 변경: {moveSpeedMultiplier:F2}");
        }
        
        /// <summary>
        /// 현재 안전 거리 반환
        /// </summary>
        public float GetSafeDistance() => safeDistance;
        
        /// <summary>
        /// 현재 정규화된 거리 반환
        /// </summary>
        public float GetNormalizedDistance(GameObservation observation)
        {
            float currentDistance = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            return currentDistance / observation.arenaRadius;
        }
        
        /// <summary>
        /// 복귀 필요 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool NeedsReturn(GameObservation observation)
        {
            return CheckBasicConditions() && GetNormalizedDistance(observation) > safeDistance;
        }
        
        /// <summary>
        /// 안전 지역에 있는지 확인
        /// </summary>
        public bool IsInSafeZone(GameObservation observation)
        {
            return GetNormalizedDistance(observation) <= safeDistance;
        }
        
        /// <summary>
        /// 목표 지점까지의 거리 반환
        /// </summary>
        public float GetDistanceToTarget(GameObservation observation)
        {
            if (!isReturning) return 0f;
            return Vector3.Distance(observation.selfPosition, targetPosition);
        }
        
        /// <summary>
        /// 예상 복귀 시간 계산 (초)
        /// </summary>
        public float GetEstimatedReturnTime(GameObservation observation, float moveSpeed = 1f)
        {
            if (IsInSafeZone(observation)) return 0f;
            
            float actualSpeed = moveSpeed * moveSpeedMultiplier;
            float distanceToReturn = GetDistanceToTarget(observation);
            
            return distanceToReturn / Mathf.Max(actualSpeed, 0.1f);
        }
        
        /// <summary>
        /// 복귀 진행률 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetReturnProgress(GameObservation observation)
        {
            if (!isReturning) return 0f;
            
            float currentDistance = GetNormalizedDistance(observation);
            float maxDistance = 1f; // 경계에서 시작
            float targetDistance = safeDistance;
            
            if (currentDistance <= targetDistance) return 1f;
            
            float totalDistance = maxDistance - targetDistance;
            float remainingDistance = currentDistance - targetDistance;
            
            return 1f - (remainingDistance / totalDistance);
        }
        
        /// <summary>
        /// 복귀 긴급도 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetReturnUrgency(GameObservation observation)
        {
            float normalized = GetNormalizedDistance(observation);
            if (normalized <= safeDistance) return 0f;
            
            return (normalized - safeDistance) / (1f - safeDistance);
        }
        
        /// <summary>
        /// 현재 복귀 중인지 확인
        /// </summary>
        public bool IsReturning() => isReturning;
        
        /// <summary>
        /// 현재 목표 위치 반환
        /// </summary>
        public Vector3 GetTargetPosition() => targetPosition;
        
        /// <summary>
        /// 복귀 상태 강제 리셋
        /// </summary>
        public void ForceStopReturn()
        {
            isReturning = false;
            consecutiveFailures = 0;
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 복귀 강제 중단");
        }
        
        /// <summary>
        /// 노드 초기화
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            isReturning = false;
            consecutiveFailures = 0;
        }
        
        /// <summary>
        /// 노드 리셋
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            isReturning = false;
            consecutiveFailures = 0;
            targetPosition = Vector3.zero;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 안전거리: {safeDistance:F2}, 복귀중: {isReturning}, 실패: {consecutiveFailures}";
        }
        
        #endregion

        #region 시각적 디버깅
        
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
        
        #endregion
    }
}

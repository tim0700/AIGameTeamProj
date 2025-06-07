using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// ML-Agents 호환 아레나 복귀 노드
    /// 시간 기반 로직을 프레임 기반으로 변경하여 ML-Agents 환경에서 안정적 동작
    /// </summary>
    public class ReturnToArenaNodeBT : BTNode
    {
        [Header("복귀 설정")]
        public float safeDistance = 0.7f;    // 목표: 아레나 반지름의 70% 지점
        public float moveSpeedMultiplier = 1.2f; // 일반 이동보다 빠르게
        public float reachThreshold = 0.5f;  // 목표 도달 판정 거리
        public int maxReturnFrames = 300;    // 최대 복귀 프레임 (5초 @ 60fps)

        private Vector3 targetPosition;
        private bool isReturning = false;
        private int returnFrameCounter = 0;
        
        // ML-Agents 호환을 위한 추가 변수들
        private int lastValidationFrame = -1;
        private bool hasValidArenaData = false;

        public ReturnToArenaNodeBT() { }

        public ReturnToArenaNodeBT(float safe, float speedMult = 1.2f)
        {
            safeDistance = safe;
            moveSpeedMultiplier = speedMult;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 🔧 ML-Agents 호환: 아레나 데이터 유효성 검사
            if (!ValidateArenaData(observation))
            {
                Debug.LogWarning("[ReturnToArena] 아레나 데이터 무효 - 기본 성공 처리");
                state = NodeState.Success; // 다른 노드 실행 허용
                return state;
            }

            // 현재 아레나 중심으로부터의 거리
            float currentDistance = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = currentDistance / observation.arenaRadius;

            // 이미 안전 지역에 있다면 성공
            if (normalizedDistance <= safeDistance)
            {
                if (isReturning)
                {
                    Debug.Log($"[ReturnToArena] 안전 지역 도달! Current: {normalizedDistance:F2} <= Target: {safeDistance}");
                    ResetReturnState();
                }
                state = NodeState.Success;
                return state;
            }

            // 복귀 시작
            if (!isReturning)
            {
                StartReturn(normalizedDistance);
            }

            // 프레임 기반 시간 초과 체크 (무한 루프 방지)
            returnFrameCounter++;
            if (returnFrameCounter > maxReturnFrames)
            {
                Debug.LogWarning($"[ReturnToArena] 복귀 프레임 초과! ({returnFrameCounter}/{maxReturnFrames}) 실패 처리");
                ResetReturnState();
                state = NodeState.Failure;
                return state;
            }

            // 복귀 이동 실행
            bool moveSuccess = ExecuteReturnMovement(observation);
            
            if (moveSuccess)
            {
                state = NodeState.Running;
            }
            else
            {
                Debug.LogWarning("[ReturnToArena] 복귀 이동 실패");
                state = NodeState.Failure;
            }

            return state;
        }

        #region ML-Agents 호환 로직

        /// <summary>
        /// 아레나 데이터 유효성 검사 (ML-Agents 호환)
        /// </summary>
        private bool ValidateArenaData(GameObservation observation)
        {
            // 현재 프레임에서 이미 검사했으면 캐시된 결과 사용
            if (lastValidationFrame == Time.frameCount)
            {
                return hasValidArenaData;
            }

            lastValidationFrame = Time.frameCount;
            
            // 아레나 중심이 영벡터가 아니고, 반지름이 0보다 큰지 확인
            bool centerValid = observation.arenaCenter != Vector3.zero;
            bool radiusValid = observation.arenaRadius > 0.1f;
            
            // 자신의 위치가 유효한지 확인
            bool selfPosValid = !float.IsNaN(observation.selfPosition.x) && 
                               !float.IsInfinity(observation.selfPosition.x);
            
            hasValidArenaData = centerValid && radiusValid && selfPosValid;
            
            if (!hasValidArenaData)
            {
                Debug.LogWarning($"[ReturnToArena] 아레나 데이터 무효: " +
                               $"Center={observation.arenaCenter}, " +
                               $"Radius={observation.arenaRadius}, " +
                               $"SelfPos={observation.selfPosition}");
            }
            
            return hasValidArenaData;
        }

        /// <summary>
        /// 복귀 시작
        /// </summary>
        private void StartReturn(float normalizedDistance)
        {
            isReturning = true;
            returnFrameCounter = 0;
            Debug.Log($"[ReturnToArena] 복귀 시작! Current distance: {normalizedDistance:F2}");
        }

        /// <summary>
        /// 복귀 상태 리셋
        /// </summary>
        private void ResetReturnState()
        {
            isReturning = false;
            returnFrameCounter = 0;
            targetPosition = Vector3.zero;
        }

        /// <summary>
        /// 복귀 이동 실행 (안전성 강화)
        /// </summary>
        private bool ExecuteReturnMovement(GameObservation observation)
        {
            // 아레나 중심 방향 계산
            Vector3 directionToCenter = (observation.arenaCenter - observation.selfPosition).normalized;

            // 목표 위치 계산 (중심에서 안전 거리만큼 떨어진 현재 방향의 지점)
            Vector3 currentDirection = observation.selfPosition - observation.arenaCenter;
            currentDirection.y = 0; // Y축 제거 (평면 이동)
            
            // 방향 벡터 정규화 시 안전성 검사
            if (currentDirection.magnitude < 0.01f)
            {
                // 거의 중심에 있는 경우 랜덤 방향으로 설정
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                currentDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle));
            }
            else
            {
                currentDirection = currentDirection.normalized;
            }

            // 안전 지역 내의 목표 지점
            targetPosition = observation.arenaCenter + currentDirection * (observation.arenaRadius * safeDistance * 0.9f);

            // 목표까지의 거리
            float distanceToTarget = Vector3.Distance(observation.selfPosition, targetPosition);

            // 목표 도달 확인
            if (distanceToTarget < reachThreshold)
            {
                Debug.Log("[ReturnToArena] 목표 지점 도달!");
                ResetReturnState();
                return true;
            }

            // 이동 방향 계산 (직접 중심으로 가는 것이 아닌 목표 지점으로)
            Vector3 moveDirection = (targetPosition - observation.selfPosition).normalized;
            
            // 방향 벡터 유효성 검사
            if (float.IsNaN(moveDirection.x) || float.IsInfinity(moveDirection.x))
            {
                Debug.LogWarning("[ReturnToArena] 무효한 이동 방향 벡터");
                return false;
            }
            
            moveDirection.y = 0; // 평면 이동 보장

            // 이동 실행
            AgentAction moveAction = AgentAction.Move(moveDirection);
            
            if (agentController != null)
            {
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    float currentNormalizedDistance = Vector3.Distance(observation.selfPosition, observation.arenaCenter) / observation.arenaRadius;
                    Debug.Log($"[ReturnToArena] 복귀 이동 중... 거리: {currentNormalizedDistance:F2}, 프레임: {returnFrameCounter}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[ReturnToArena] 이동 실패: {result.message}");
                    return false;
                }
            }

            return false;
        }

        #endregion

        #region 추가 기능들

        /// <summary>
        /// 최대 복귀 시간 설정 (프레임 기반)
        /// </summary>
        public void SetMaxReturnTime(float seconds, float targetFPS = 60f)
        {
            maxReturnFrames = Mathf.RoundToInt(seconds * targetFPS);
            Debug.Log($"[ReturnToArena] 최대 복귀 시간 변경: {seconds}초 ({maxReturnFrames} 프레임)");
        }

        /// <summary>
        /// 안전 거리 설정
        /// </summary>
        public void SetSafeDistance(float distance)
        {
            safeDistance = Mathf.Clamp01(distance);
            Debug.Log($"[ReturnToArena] 안전 거리 변경: {safeDistance:F2}");
        }

        /// <summary>
        /// 현재 복귀 중인지 확인
        /// </summary>
        public bool IsReturning() => isReturning;

        /// <summary>
        /// 복귀 진행률 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetReturnProgress()
        {
            if (!isReturning || maxReturnFrames == 0) return 0f;
            return (float)returnFrameCounter / maxReturnFrames;
        }

        /// <summary>
        /// 현재 목표 위치 반환
        /// </summary>
        public Vector3 GetTargetPosition() => targetPosition;

        #endregion

        #region 오버라이드 메서드

        /// <summary>
        /// 노드 초기화 시 상태 리셋
        /// </summary>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            ResetReturnState();
            lastValidationFrame = -1;
            hasValidArenaData = false;
        }

        #endregion

        #region 시각적 디버깅

        /// <summary>
        /// 시각적 디버깅을 위한 목표 위치 표시
        /// </summary>
        public void DrawDebugTarget()
        {
            if (isReturning && targetPosition != Vector3.zero)
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
            if (!hasValidArenaData) return;

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

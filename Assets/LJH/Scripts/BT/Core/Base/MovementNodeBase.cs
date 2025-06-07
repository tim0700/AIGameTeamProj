using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 이동 노드 특화 베이스 클래스
    /// 적에게 이동, 거리 유지, 아레나 복귀, 순찰 등 이동 관련 노드들의 기반
    /// </summary>
    public abstract class MovementNodeBase : BTNodeBase, IMovementNode
    {
        [Header("이동 노드 설정")]
        [SerializeField] protected MovementParameters movementParameters;
        [SerializeField] protected bool enablePathfinding = true;
        [SerializeField] protected bool enableCollisionAvoidance = true;
        
        // 이동 상태 관리
        protected Vector3 currentTarget = Vector3.zero;
        protected Vector3 lastPosition = Vector3.zero;
        protected float lastPathUpdateTime = 0f;
        protected bool isMoving = false;
        protected float movementStartTime = 0f;
        
        // 경로 계획
        protected Vector3[] plannedPath = null;
        protected int currentPathIndex = 0;
        protected float stuckDetectionTime = 0f;
        protected float lastMovementProgress = 0f;
        
        // 성능 추적
        protected float totalDistanceTraveled = 0f;
        protected int pathRecalculationCount = 0;
        protected int collisionAvoidanceCount = 0;
        
        // 예측 시스템
        protected Vector3 predictedTargetPosition = Vector3.zero;
        protected float lastPredictionTime = 0f;
        
        public MovementNodeBase() : base()
        {
            InitializeMovementParameters();
        }
        
        public MovementNodeBase(string name, string description = "") : base(name, description)
        {
            InitializeMovementParameters();
        }
        
        #region BTNodeBase 오버라이드
        
        protected override NodeState EvaluateNode(GameObservation observation)
        {
            // 1. 목표 도달 여부 확인
            if (IsTargetReached(observation))
            {
                OnTargetReached(observation);
                return NodeState.Success;
            }
            
            // 2. 이동 가능성 확인
            if (!CanMove(observation))
            {
                return NodeState.Failure;
            }
            
            // 3. 경로 업데이트 (필요시)
            if (ShouldUpdatePath(observation))
            {
                UpdatePath(observation);
            }
            
            // 4. 이동 실행
            bool moveSuccess = ExecuteMovement(observation);
            
            if (moveSuccess)
            {
                UpdateMovementTracking(observation);
                return NodeState.Running;
            }
            else
            {
                return NodeState.Failure;
            }
        }
        
        protected override string GetNodeType() => "Movement";
        
        protected override void InitializeParameters()
        {
            if (movementParameters == null)
            {
                movementParameters = CreateDefaultParameters();
            }
            parameters = movementParameters;
        }
        
        protected override void OnParametersChanged()
        {
            if (parameters is MovementParameters newParams)
            {
                movementParameters = newParams;
                OnMovementParametersChanged();
            }
        }
        
        protected override void OnReset()
        {
            isMoving = false;
            currentTarget = Vector3.zero;
            plannedPath = null;
            currentPathIndex = 0;
            stuckDetectionTime = 0f;
            lastMovementProgress = 0f;
            totalDistanceTraveled = 0f;
            pathRecalculationCount = 0;
            collisionAvoidanceCount = 0;
        }
        
        #endregion
        
        #region IMovementNode 구현
        
        public virtual Vector3 CalculateTargetPosition(GameObservation observation)
        {
            // 예측 기능 활성화시 목표 위치 예측
            if (movementParameters.enablePrediction && 
                Time.time - lastPredictionTime > movementParameters.pathUpdateInterval)
            {
                predictedTargetPosition = PredictTargetPosition(observation);
                lastPredictionTime = Time.time;
                return predictedTargetPosition;
            }
            
            // 기본 목표 위치 계산
            return GetBaseTargetPosition(observation);
        }
        
        public virtual Vector3 GetMoveDirection(GameObservation observation)
        {
            Vector3 targetPosition = CalculateTargetPosition(observation);
            
            // 경로 계획 사용시
            if (enablePathfinding && plannedPath != null && plannedPath.Length > 0)
            {
                return GetPathDirection(observation);
            }
            
            // 직선 이동
            Vector3 direction = (targetPosition - observation.selfPosition).normalized;
            
            // 장애물 회피
            if (enableCollisionAvoidance && HasPathObstacle(observation))
            {
                direction = GetAlternativePath(observation);
                collisionAvoidanceCount++;
            }
            
            return direction;
        }
        
        public virtual float GetStoppingDistance() => movementParameters?.stoppingDistance ?? 2f;
        
        public virtual bool IsTargetReached(GameObservation observation)
        {
            Vector3 targetPosition = CalculateTargetPosition(observation);
            float distance = Vector3.Distance(observation.selfPosition, targetPosition);
            return distance <= GetStoppingDistance();
        }
        
        public virtual float GetMoveSpeed(GameObservation observation)
        {
            float baseSpeed = movementParameters?.moveSpeed ?? 1f;
            
            // 적응적 속도 조정
            if (movementParameters.adaptiveSpeed)
            {
                baseSpeed *= GetAdaptiveSpeedMultiplier(observation);
            }
            
            // 거리에 따른 속도 조정
            float distanceToTarget = Vector3.Distance(observation.selfPosition, CalculateTargetPosition(observation));
            if (distanceToTarget < GetStoppingDistance() * 2f)
            {
                baseSpeed *= 0.5f; // 목표 근처에서 감속
            }
            
            return Mathf.Clamp(baseSpeed, 0.1f, 2f);
        }
        
        public virtual bool HasPathObstacle(GameObservation observation)
        {
            Vector3 targetPosition = CalculateTargetPosition(observation);
            Vector3 direction = (targetPosition - observation.selfPosition).normalized;
            float distance = Vector3.Distance(observation.selfPosition, targetPosition);
            
            // 아레나 경계 체크
            if (movementParameters.respectArenaBounds)
            {
                Vector3 checkPosition = observation.selfPosition + direction * Mathf.Min(distance, 2f);
                if (!IsValidMoveTarget(checkPosition, observation))
                {
                    return true;
                }
            }
            
            // 커스텀 장애물 체크
            return CheckCustomObstacles(observation, direction, distance);
        }
        
        public virtual Vector3 GetAlternativePath(GameObservation observation)
        {
            Vector3 targetPosition = CalculateTargetPosition(observation);
            Vector3 currentPosition = observation.selfPosition;
            Vector3 directDirection = (targetPosition - currentPosition).normalized;
            
            // 좌우 회피 방향 계산
            Vector3 rightDirection = Vector3.Cross(Vector3.up, directDirection).normalized;
            Vector3 leftDirection = -rightDirection;
            
            // 더 안전한 방향 선택
            Vector3 rightPos = currentPosition + rightDirection * movementParameters.evasionDistance;
            Vector3 leftPos = currentPosition + leftDirection * movementParameters.evasionDistance;
            
            bool rightSafe = IsValidMoveTarget(rightPos, observation);
            bool leftSafe = IsValidMoveTarget(leftPos, observation);
            
            if (rightSafe && leftSafe)
            {
                // 목표에 더 가까운 방향 선택
                float rightDistance = Vector3.Distance(rightPos, targetPosition);
                float leftDistance = Vector3.Distance(leftPos, targetPosition);
                return rightDistance < leftDistance ? rightDirection : leftDirection;
            }
            else if (rightSafe)
            {
                return rightDirection;
            }
            else if (leftSafe)
            {
                return leftDirection;
            }
            else
            {
                // 후진
                return -directDirection;
            }
        }
        
        public virtual int GetMovementPriority() => movementParameters?.priority ?? 5;
        
        public virtual float GetEstimatedTravelTime(GameObservation observation)
        {
            Vector3 targetPosition = CalculateTargetPosition(observation);
            float distance = Vector3.Distance(observation.selfPosition, targetPosition);
            float speed = GetMoveSpeed(observation);
            
            return distance / Mathf.Max(speed, 0.1f);
        }
        
        public virtual bool AllowEvasionDuringMovement() => movementParameters?.allowEvasion ?? true;
        
        public virtual void SetStoppingDistance(float distance)
        {
            if (movementParameters != null)
            {
                movementParameters.stoppingDistance = Mathf.Clamp(distance, 0.1f, 10f);
            }
        }
        
        public virtual void SetMovementParameters(float[] parameters)
        {
            if (movementParameters != null)
            {
                movementParameters.FromArray(parameters);
                OnMovementParametersChanged();
            }
        }
        
        public virtual float[] GetMovementParameters()
        {
            return movementParameters?.ToArray() ?? new float[0];
        }
        
        public virtual bool IsValidMoveTarget(Vector3 targetPosition, GameObservation observation)
        {
            if (!movementParameters.respectArenaBounds) return true;
            
            // 아레나 중심에서 목표 위치까지의 거리 확인
            float distanceFromCenter = Vector3.Distance(targetPosition, observation.arenaCenter);
            float maxDistance = observation.arenaRadius - movementParameters.boundaryPadding;
            
            return distanceFromCenter <= maxDistance;
        }
        
        #endregion
        
        #region 이동 실행 관리
        
        /// <summary>
        /// 이동 가능 여부 확인
        /// </summary>
        protected virtual bool CanMove(GameObservation observation)
        {
            if (!IsActive()) return false;
            if (observation.currentState == AgentState.Dead) return false;
            
            // 커스텀 이동 조건 확인
            return CheckCustomMoveConditions(observation);
        }
        
        /// <summary>
        /// 실제 이동 실행
        /// </summary>
        protected virtual bool ExecuteMovement(GameObservation observation)
        {
            if (agentController == null) return false;
            
            Vector3 moveDirection = GetMoveDirection(observation);
            float moveSpeed = GetMoveSpeed(observation);
            
            // AgentAction 생성 및 실행
            AgentAction moveAction = CreateMoveAction(observation, moveDirection, moveSpeed);
            ActionResult result = agentController.ExecuteAction(moveAction);
            
            // 이동 상태 업데이트
            if (result.success)
            {
                if (!isMoving)
                {
                    isMoving = true;
                    movementStartTime = Time.time;
                    OnMovementStarted(observation);
                }
                
                return true;
            }
            else
            {
                OnMovementFailed(observation, result);
                return false;
            }
        }
        
        /// <summary>
        /// 이동 추적 정보 업데이트
        /// </summary>
        protected virtual void UpdateMovementTracking(GameObservation observation)
        {
            // 이동 거리 누적
            if (lastPosition != Vector3.zero)
            {
                totalDistanceTraveled += Vector3.Distance(observation.selfPosition, lastPosition);
            }
            lastPosition = observation.selfPosition;
            
            // 진행 상황 추적 (막힘 감지용)
            float currentProgress = Vector3.Distance(observation.selfPosition, CalculateTargetPosition(observation));
            if (Mathf.Abs(currentProgress - lastMovementProgress) < 0.1f)
            {
                stuckDetectionTime += Time.deltaTime;
            }
            else
            {
                stuckDetectionTime = 0f;
            }
            lastMovementProgress = currentProgress;
        }
        
        #endregion
        
        #region 경로 계획 시스템
        
        /// <summary>
        /// 경로 업데이트 필요 여부 확인
        /// </summary>
        protected virtual bool ShouldUpdatePath(GameObservation observation)
        {
            float timeSinceLastUpdate = Time.time - lastPathUpdateTime;
            
            // 시간 기반 업데이트
            if (timeSinceLastUpdate > movementParameters.pathUpdateInterval) return true;
            
            // 막힘 감지시 업데이트
            if (stuckDetectionTime > 1f) return true;
            
            // 목표가 크게 변경된 경우
            Vector3 newTarget = CalculateTargetPosition(observation);
            if (Vector3.Distance(newTarget, currentTarget) > 2f) return true;
            
            return false;
        }
        
        /// <summary>
        /// 경로 업데이트
        /// </summary>
        protected virtual void UpdatePath(GameObservation observation)
        {
            currentTarget = CalculateTargetPosition(observation);
            lastPathUpdateTime = Time.time;
            pathRecalculationCount++;
            
            if (enablePathfinding)
            {
                plannedPath = CalculatePath(observation.selfPosition, currentTarget, observation);
                currentPathIndex = 0;
            }
            
            // 막힘 상태 리셋
            stuckDetectionTime = 0f;
        }
        
        /// <summary>
        /// 경로 계산
        /// </summary>
        protected virtual Vector3[] CalculatePath(Vector3 start, Vector3 target, GameObservation observation)
        {
            switch (movementParameters.pathfindingType)
            {
                case PathfindingType.Direct:
                    return new Vector3[] { target };
                
                case PathfindingType.Smooth:
                    return CalculateSmoothPath(start, target, observation);
                
                case PathfindingType.Tactical:
                    return CalculateTacticalPath(start, target, observation);
                
                default:
                    return new Vector3[] { target };
            }
        }
        
        /// <summary>
        /// 경로 방향 계산
        /// </summary>
        protected virtual Vector3 GetPathDirection(GameObservation observation)
        {
            if (plannedPath == null || currentPathIndex >= plannedPath.Length)
            {
                return Vector3.zero;
            }
            
            Vector3 currentWaypoint = plannedPath[currentPathIndex];
            Vector3 direction = (currentWaypoint - observation.selfPosition).normalized;
            
            // 웨이포인트에 도달했으면 다음 웨이포인트로
            if (Vector3.Distance(observation.selfPosition, currentWaypoint) < 1f)
            {
                currentPathIndex++;
                if (currentPathIndex < plannedPath.Length)
                {
                    currentWaypoint = plannedPath[currentPathIndex];
                    direction = (currentWaypoint - observation.selfPosition).normalized;
                }
            }
            
            return direction;
        }
        
        #endregion
        
        #region 예측 및 적응 시스템
        
        /// <summary>
        /// 목표 위치 예측
        /// </summary>
        protected virtual Vector3 PredictTargetPosition(GameObservation observation)
        {
            Vector3 baseTarget = GetBaseTargetPosition(observation);
            
            // 기본적으로는 현재 목표 위치 반환
            // 하위 클래스에서 적의 이동 예측 등을 구현할 수 있음
            return baseTarget;
        }
        
        /// <summary>
        /// 적응적 속도 배율 계산
        /// </summary>
        protected virtual float GetAdaptiveSpeedMultiplier(GameObservation observation)
        {
            // 기본 구현: HP가 낮으면 빠르게, 높으면 천천히
            float hpRatio = observation.selfHP / 100f;
            return Mathf.Lerp(1.5f, 0.8f, hpRatio);
        }
        
        #endregion
        
        #region 경로 계산 헬퍼 메서드들
        
        /// <summary>
        /// 부드러운 경로 계산
        /// </summary>
        protected virtual Vector3[] CalculateSmoothPath(Vector3 start, Vector3 target, GameObservation observation)
        {
            // 간단한 베지어 곡선 기반 경로
            Vector3 midPoint = (start + target) * 0.5f;
            Vector3 offset = Vector3.Cross((target - start).normalized, Vector3.up) * 2f;
            Vector3 controlPoint = midPoint + offset;
            
            return new Vector3[] { controlPoint, target };
        }
        
        /// <summary>
        /// 전술적 경로 계산
        /// </summary>
        protected virtual Vector3[] CalculateTacticalPath(Vector3 start, Vector3 target, GameObservation observation)
        {
            // 적을 고려한 우회 경로
            Vector3 enemyPos = observation.enemyPosition;
            Vector3 toEnemy = (enemyPos - start).normalized;
            Vector3 perpendicular = Vector3.Cross(toEnemy, Vector3.up).normalized;
            
            // 적을 피해서 우회하는 경로
            Vector3 avoidancePoint = start + perpendicular * movementParameters.safeDistance;
            
            return new Vector3[] { avoidancePoint, target };
        }
        
        #endregion
        
        #region 가상 메서드들 (하위 클래스에서 구현)
        
        /// <summary>
        /// 기본 이동 파라미터 생성
        /// </summary>
        protected virtual MovementParameters CreateDefaultParameters()
        {
            var defaultParams = new MovementParameters();
            defaultParams.parameterName = $"{nodeName}_Parameters";
            defaultParams.description = $"{nodeName} 이동 파라미터";
            return defaultParams;
        }
        
        /// <summary>
        /// 기본 목표 위치 계산 (하위 클래스에서 구현)
        /// </summary>
        protected abstract Vector3 GetBaseTargetPosition(GameObservation observation);
        
        /// <summary>
        /// AgentAction 생성
        /// </summary>
        protected virtual AgentAction CreateMoveAction(GameObservation observation, Vector3 direction, float speed)
        {
            return AgentAction.Move(direction);
        }
        
        /// <summary>
        /// 커스텀 이동 조건 확인
        /// </summary>
        protected virtual bool CheckCustomMoveConditions(GameObservation observation) => true;
        
        /// <summary>
        /// 커스텀 장애물 확인
        /// </summary>
        protected virtual bool CheckCustomObstacles(GameObservation observation, Vector3 direction, float distance) => false;
        
        /// <summary>
        /// 이동 파라미터 변경 시 호출
        /// </summary>
        protected virtual void OnMovementParametersChanged()
        {
            // 경로 재계산 필요
            plannedPath = null;
        }
        
        /// <summary>
        /// 이동 시작 시 호출
        /// </summary>
        protected virtual void OnMovementStarted(GameObservation observation) { }
        
        /// <summary>
        /// 목표 도달 시 호출
        /// </summary>
        protected virtual void OnTargetReached(GameObservation observation)
        {
            isMoving = false;
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 목표에 도달했습니다. 총 이동거리: {totalDistanceTraveled:F1}");
            }
        }
        
        /// <summary>
        /// 이동 실패 시 호출
        /// </summary>
        protected virtual void OnMovementFailed(GameObservation observation, ActionResult result)
        {
            if (enableDetailedLogging)
            {
                Debug.LogWarning($"[{nodeName}] 이동 실패: {result.message}");
            }
        }
        
        /// <summary>
        /// 이동 파라미터 초기화
        /// </summary>
        protected virtual void InitializeMovementParameters()
        {
            if (movementParameters == null)
            {
                movementParameters = CreateDefaultParameters();
            }
        }
        
        #endregion
    }
}

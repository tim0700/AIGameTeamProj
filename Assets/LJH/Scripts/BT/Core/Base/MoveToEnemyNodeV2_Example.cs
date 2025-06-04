using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 개선된 적 추적 이동 노드 (MovementNodeBase 기반)
    /// 기존 MoveToEnemyNode와 호환성을 유지하면서 고급 기능 추가
    /// </summary>
    public class MoveToEnemyNodeV2 : MovementNodeBase
    {
        [Header("적 추적 설정")]
        [SerializeField] private bool enablePredictiveMovement = true;
        [SerializeField] private bool maintainOptimalDistance = true;
        [SerializeField] private bool enableFlankingMovement = false;
        
        // 추적 상태
        private Vector3 lastEnemyPosition = Vector3.zero;
        private Vector3 enemyVelocity = Vector3.zero;
        private float lastEnemyUpdateTime = 0f;
        
        // 플랭킹 설정
        private FlankingDirection currentFlankingDirection = FlankingDirection.None;
        private float flankingStartTime = 0f;
        
        // 기존 생성자와 호환성 유지
        public MoveToEnemyNodeV2(float stoppingDistance = 2f) : base("MoveToEnemyNode", "적에게 이동하는 노드")
        {
            // 기존 파라미터를 새로운 시스템으로 변환
            if (movementParameters == null)
            {
                movementParameters = new MovementParameters();
            }
            movementParameters.stoppingDistance = stoppingDistance;
            movementParameters.pathfindingType = PathfindingType.Tactical;
            movementParameters.enablePrediction = true;
        }
        
        public MoveToEnemyNodeV2() : this(2f) { }
        
        #region MovementNodeBase 구현
        
        protected override Vector3 GetBaseTargetPosition(GameObservation observation)
        {
            Vector3 enemyPosition = observation.enemyPosition;
            
            // 적의 이동 예측
            if (enablePredictiveMovement)
            {
                UpdateEnemyVelocity(observation);
                enemyPosition = PredictEnemyPosition(observation);
            }
            
            // 최적 거리 유지
            if (maintainOptimalDistance)
            {
                enemyPosition = AdjustForOptimalDistance(enemyPosition, observation);
            }
            
            // 플랭킹 이동
            if (enableFlankingMovement)
            {
                enemyPosition = ApplyFlankingMovement(enemyPosition, observation);
            }
            
            return enemyPosition;
        }
        
        protected override Vector3 PredictTargetPosition(GameObservation observation)
        {
            if (!enablePredictiveMovement) return GetBaseTargetPosition(observation);
            
            Vector3 predictedEnemyPos = PredictEnemyPosition(observation);
            float predictionTime = movementParameters.predictionTime;
            
            // 우리의 이동 시간도 고려하여 만날 지점 계산
            Vector3 interceptPoint = CalculateInterceptPoint(observation.selfPosition, predictedEnemyPos, observation);
            
            return interceptPoint;
        }
        
        protected override bool CheckCustomMoveConditions(GameObservation observation)
        {
            // 적이 너무 멀리 있으면 이동하지 않음
            if (observation.distanceToEnemy > movementParameters.maxRange)
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 적이 너무 멀리 있음. 거리: {observation.distanceToEnemy:F2}");
                return false;
            }
            
            // 적이 죽었으면 이동하지 않음
            if (observation.enemyHP <= 0)
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 적이 사망하여 추적 중지");
                return false;
            }
            
            return true;
        }
        
        protected override bool CheckCustomObstacles(GameObservation observation, Vector3 direction, float distance)
        {
            // 적과 너무 가까워지는 것 방지 (최소 거리 확인)
            Vector3 futurePosition = observation.selfPosition + direction * distance;
            float futureDistance = Vector3.Distance(futurePosition, observation.enemyPosition);
            
            if (futureDistance < 0.5f) // 너무 가까움
            {
                return true;
            }
            
            return false;
        }
        
        protected override void OnTargetReached(GameObservation observation)
        {
            base.OnTargetReached(observation);
            
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 적에게 도달! 최종 거리: {observation.distanceToEnemy:F2}");
            }
            
            // 플랭킹 상태 리셋
            if (enableFlankingMovement)
            {
                currentFlankingDirection = FlankingDirection.None;
            }
        }
        
        protected override void OnMovementFailed(GameObservation observation, ActionResult result)
        {
            base.OnMovementFailed(observation, result);
            
            // 이동 실패시 플랭킹 방향 변경
            if (enableFlankingMovement)
            {
                SwitchFlankingDirection();
            }
        }
        
        #endregion
        
        #region 적 추적 특화 기능들
        
        /// <summary>
        /// 적의 속도 업데이트
        /// </summary>
        private void UpdateEnemyVelocity(GameObservation observation)
        {
            float currentTime = Time.time;
            
            if (lastEnemyPosition != Vector3.zero && lastEnemyUpdateTime > 0)
            {
                float deltaTime = currentTime - lastEnemyUpdateTime;
                if (deltaTime > 0)
                {
                    Vector3 displacement = observation.enemyPosition - lastEnemyPosition;
                    enemyVelocity = displacement / deltaTime;
                }
            }
            
            lastEnemyPosition = observation.enemyPosition;
            lastEnemyUpdateTime = currentTime;
        }
        
        /// <summary>
        /// 적 위치 예측
        /// </summary>
        private Vector3 PredictEnemyPosition(GameObservation observation)
        {
            float predictionTime = movementParameters.predictionTime;
            Vector3 predictedPosition = observation.enemyPosition + enemyVelocity * predictionTime;
            
            // 아레나 경계 내로 제한
            if (movementParameters.respectArenaBounds)
            {
                Vector3 arenaCenter = observation.arenaCenter;
                float arenaRadius = observation.arenaRadius;
                
                float distanceFromCenter = Vector3.Distance(predictedPosition, arenaCenter);
                if (distanceFromCenter > arenaRadius)
                {
                    Vector3 direction = (predictedPosition - arenaCenter).normalized;
                    predictedPosition = arenaCenter + direction * arenaRadius;
                }
            }
            
            return predictedPosition;
        }
        
        /// <summary>
        /// 최적 거리로 조정
        /// </summary>
        private Vector3 AdjustForOptimalDistance(Vector3 enemyPosition, GameObservation observation)
        {
            Vector3 selfPosition = observation.selfPosition;
            Vector3 direction = (enemyPosition - selfPosition).normalized;
            float currentDistance = Vector3.Distance(selfPosition, enemyPosition);
            float optimalDistance = movementParameters.stoppingDistance;
            
            // 너무 가까우면 약간 뒤로, 너무 멀면 가까이
            if (currentDistance < optimalDistance * 0.5f)
            {
                // 너무 가까움 - 약간 뒤로
                return enemyPosition - direction * optimalDistance;
            }
            else if (currentDistance > optimalDistance * 2f)
            {
                // 너무 멀음 - 더 가까이
                return enemyPosition - direction * (optimalDistance * 0.8f);
            }
            
            return enemyPosition - direction * optimalDistance;
        }
        
        /// <summary>
        /// 플랭킹 이동 적용
        /// </summary>
        private Vector3 ApplyFlankingMovement(Vector3 enemyPosition, GameObservation observation)
        {
            // 플랭킹 방향 결정
            if (currentFlankingDirection == FlankingDirection.None || 
                Time.time - flankingStartTime > 3f) // 3초마다 방향 변경
            {
                DetermineFlankingDirection(observation);
            }
            
            Vector3 selfPosition = observation.selfPosition;
            Vector3 toEnemy = (enemyPosition - selfPosition).normalized;
            Vector3 flankingOffset = Vector3.zero;
            
            switch (currentFlankingDirection)
            {
                case FlankingDirection.Left:
                    flankingOffset = Vector3.Cross(toEnemy, Vector3.up).normalized * 2f;
                    break;
                case FlankingDirection.Right:
                    flankingOffset = Vector3.Cross(Vector3.up, toEnemy).normalized * 2f;
                    break;
                case FlankingDirection.Behind:
                    flankingOffset = -toEnemy * 1f;
                    break;
            }
            
            Vector3 flankingPosition = enemyPosition + flankingOffset;
            
            // 아레나 경계 확인
            if (IsValidMoveTarget(flankingPosition, observation))
            {
                return flankingPosition;
            }
            else
            {
                // 유효하지 않으면 다른 방향 시도
                SwitchFlankingDirection();
                return enemyPosition;
            }
        }
        
        /// <summary>
        /// 플랭킹 방향 결정
        /// </summary>
        private void DetermineFlankingDirection(GameObservation observation)
        {
            // 랜덤하게 플랭킹 방향 선택 (실제로는 전술적 판단 필요)
            FlankingDirection[] directions = { FlankingDirection.Left, FlankingDirection.Right, FlankingDirection.Behind };
            currentFlankingDirection = directions[Random.Range(0, directions.Length)];
            flankingStartTime = Time.time;
            
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 플랭킹 방향 변경: {currentFlankingDirection}");
            }
        }
        
        /// <summary>
        /// 플랭킹 방향 전환
        /// </summary>
        private void SwitchFlankingDirection()
        {
            switch (currentFlankingDirection)
            {
                case FlankingDirection.Left:
                    currentFlankingDirection = FlankingDirection.Right;
                    break;
                case FlankingDirection.Right:
                    currentFlankingDirection = FlankingDirection.Behind;
                    break;
                case FlankingDirection.Behind:
                    currentFlankingDirection = FlankingDirection.Left;
                    break;
                default:
                    currentFlankingDirection = FlankingDirection.Left;
                    break;
            }
            
            flankingStartTime = Time.time;
        }
        
        /// <summary>
        /// 요격 지점 계산
        /// </summary>
        private Vector3 CalculateInterceptPoint(Vector3 selfPos, Vector3 predictedEnemyPos, GameObservation observation)
        {
            float selfSpeed = GetMoveSpeed(observation);
            float distanceToTarget = Vector3.Distance(selfPos, predictedEnemyPos);
            float timeToReach = distanceToTarget / Mathf.Max(selfSpeed, 0.1f);
            
            // 적이 그 시간 동안 더 이동할 위치 계산
            Vector3 finalInterceptPoint = predictedEnemyPos + enemyVelocity * timeToReach;
            
            return finalInterceptPoint;
        }
        
        /// <summary>
        /// 추적 효율성 분석
        /// </summary>
        public TrackingEfficiency AnalyzeTrackingEfficiency(GameObservation observation)
        {
            float distanceToTarget = observation.distanceToEnemy;
            float optimalDistance = movementParameters.stoppingDistance;
            
            if (distanceToTarget <= optimalDistance * 1.2f)
                return TrackingEfficiency.Excellent;
            else if (distanceToTarget <= optimalDistance * 2f)
                return TrackingEfficiency.Good;
            else if (distanceToTarget <= optimalDistance * 3f)
                return TrackingEfficiency.Fair;
            else
                return TrackingEfficiency.Poor;
        }
        
        #endregion
        
        #region 기존 API 호환성
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public void SetStoppingDistance(float distance)
        {
            movementParameters.stoppingDistance = distance;
        }
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public float GetStoppingDistance()
        {
            return movementParameters.stoppingDistance;
        }
        
        /// <summary>
        /// 예측 이동 활성화/비활성화
        /// </summary>
        public void SetPredictiveMovement(bool enable)
        {
            enablePredictiveMovement = enable;
            movementParameters.enablePrediction = enable;
        }
        
        /// <summary>
        /// 플랭킹 이동 활성화/비활성화
        /// </summary>
        public void SetFlankingMovement(bool enable)
        {
            enableFlankingMovement = enable;
            if (!enable)
            {
                currentFlankingDirection = FlankingDirection.None;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 플랭킹 방향
    /// </summary>
    public enum FlankingDirection
    {
        None,   // 플랭킹 없음
        Left,   // 좌측 플랭킹
        Right,  // 우측 플랭킹
        Behind  // 후방 플랭킹
    }
    
    /// <summary>
    /// 추적 효율성
    /// </summary>
    public enum TrackingEfficiency
    {
        Poor,       // 비효율적
        Fair,       // 보통
        Good,       // 좋음
        Excellent   // 우수
    }
}

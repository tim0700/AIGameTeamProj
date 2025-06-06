using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적에게 이동하는 노드
    /// </summary>
    public class MoveToEnemyNode : BTNode
    {
        private float stoppingDistance;
        private Vector3 lastEnemyPosition = Vector3.zero;
        private bool isMoving = false;

        public MoveToEnemyNode(float stoppingDistance = 2f) 
            : base($"MoveToEnemy Node (Stop: {stoppingDistance:F1})")
        {
            this.stoppingDistance = stoppingDistance;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 적이 죽었으면 이동하지 않음
            if (observation.enemyHP <= 0)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 사망하여 이동 중지");
                
                isMoving = false;
                state = NodeState.Failure;
                return state;
            }

            // 이미 충분히 가까이 있으면 성공
            if (observation.distanceToEnemy <= stoppingDistance)
            {
                if (isMoving && enableLogging)
                    Debug.Log($"[{nodeName}] 목표 거리 도달! 현재 거리: {observation.distanceToEnemy:F2}");
                
                isMoving = false;
                state = NodeState.Success;
                return state;
            }

            // 적 방향으로 이동
            Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            
            // 가장 적절한 이동 방향 결정 (4방향)
            ActionType moveType = GetOptimalMoveType(directionToEnemy);
            
            AgentAction moveAction = AgentAction.Move(directionToEnemy);
            ActionResult result = agentController.ExecuteAction(moveAction);
            
            if (result.success)
            {
                if (!isMoving && enableLogging)
                    Debug.Log($"[{nodeName}] 적에게 이동 시작. 목표 거리: {stoppingDistance:F2}");
                
                isMoving = true;
                lastEnemyPosition = observation.enemyPosition;
                
                state = NodeState.Running; // 계속 이동 중
                
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적에게 이동 중... 거리: {observation.distanceToEnemy:F2}");
            }
            else
            {
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 이동 실패: {result.message}");
                
                state = NodeState.Failure;
            }

            return state;
        }

        #region 이동 로직 헬퍼들
        
        /// <summary>
        /// 최적 이동 타입 결정
        /// </summary>
        private ActionType GetOptimalMoveType(Vector3 direction)
        {
            // 가장 강한 방향 성분에 따라 4방향 중 선택
            if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
            {
                return direction.z > 0 ? ActionType.MoveForward : ActionType.MoveBack;
            }
            else
            {
                return direction.x > 0 ? ActionType.MoveRight : ActionType.MoveLeft;
            }
        }
        
        /// <summary>
        /// 적이 크게 이동했는지 확인
        /// </summary>
        private bool HasEnemyMovedSignificantly(GameObservation observation)
        {
            if (lastEnemyPosition == Vector3.zero) return false;
            
            float enemyMovement = Vector3.Distance(observation.enemyPosition, lastEnemyPosition);
            return enemyMovement > 1f; // 1미터 이상 이동 시 유의미한 이동으로 판단
        }
        
        #endregion
        
        #region 추가 기능들
        
        /// <summary>
        /// 정지 거리 설정
        /// </summary>
        public void SetStoppingDistance(float distance)
        {
            stoppingDistance = Mathf.Max(0.1f, distance);
            SetNodeName($"MoveToEnemy Node (Stop: {stoppingDistance:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 정지 거리 변경: {stoppingDistance:F2}");
        }
        
        /// <summary>
        /// 현재 정지 거리 반환
        /// </summary>
        public float GetStoppingDistance() => stoppingDistance;
        
        /// <summary>
        /// 이동 가능 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CanMoveToEnemy(GameObservation observation)
        {
            return CheckBasicConditions() && 
                   observation.enemyHP > 0 && 
                   observation.distanceToEnemy > stoppingDistance;
        }
        
        /// <summary>
        /// 목표 도달 여부 확인
        /// </summary>
        public bool IsAtTarget(GameObservation observation)
        {
            return observation.distanceToEnemy <= stoppingDistance;
        }
        
        /// <summary>
        /// 예상 도달 시간 계산 (초)
        /// </summary>
        public float GetEstimatedArrivalTime(GameObservation observation, float moveSpeed = 1f)
        {
            if (IsAtTarget(observation)) return 0f;
            
            float remainingDistance = observation.distanceToEnemy - stoppingDistance;
            return remainingDistance / Mathf.Max(moveSpeed, 0.1f);
        }
        
        /// <summary>
        /// 이동 진행률 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetMoveProgress(GameObservation observation, float maxDistance = 10f)
        {
            float remainingDistance = Mathf.Max(0f, observation.distanceToEnemy - stoppingDistance);
            float coverredDistance = maxDistance - remainingDistance;
            return Mathf.Clamp01(coverredDistance / maxDistance);
        }
        
        /// <summary>
        /// 이동 상태 리셋
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            isMoving = false;
            lastEnemyPosition = Vector3.zero;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 정지거리: {stoppingDistance:F2}, 이동중: {isMoving}";
        }
        
        #endregion
    }
}

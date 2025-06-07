using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적에게 이동하는 이동 노드
    /// 적과의 거리를 줄여 공격 범위 내로 진입하는 전술적 이동 노드
    /// 
    /// 동작 원리:
    /// - 적과의 현재 거리를 정지 거리와 비교
    /// - 이미 충분히 가까이 있으면 즉시 Success 반환
    /// - 그렇지 않으면 적 방향으로 이동 시작
    /// - 이동 중에는 Running 상태 유지
    /// 
    /// 전략적 활용:
    /// - 공격 전 사전 위치 선점
    /// - 원거리 위치에서 공격 위치로 이동
    /// - 추격 및 압박 전술
    /// - 전장 주도권 확보
    /// 
    /// 에너지 최적화:
    /// - 4방향 이동 시스템 사용
    /// - 가장 효율적인 이동 경로 선택
    /// - 직선 거리 기반 최적화
    /// </summary>
    public class MoveToEnemyNode : BTNode
    {
        /// <summary>
        /// 적에게 접근을 중단할 최소 거리
        /// 이 거리 이내에 도달하면 이동 성공으로 간주
        /// </summary>
        private float stoppingDistance;

        /// <summary>
        /// 적 추적 이동 노드 생성자
        /// </summary>
        /// <param name="stoppingDistance">정지 거리 (기본값: 2.0 유닛)</param>
        public MoveToEnemyNode(float stoppingDistance = 2f)
        {
            this.stoppingDistance = stoppingDistance;
        }

        /// <summary>
        /// 적 추적 이동 로직 실행
        /// 거리 확인 → 방향 계산 → 이동 실행의 순서로 진행
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>목표 도달시 Success, 이동 중이면 Running, 실패시 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // === 1단계: 목표 거리 도달 여부 확인 ===
            // 이미 충분히 가까이 있으면 이동 성공
            if (observation.distanceToEnemy <= stoppingDistance)
            {
                state = NodeState.Success;
                Debug.Log($"{agentController?.GetAgentName() ?? "Agent"} 적에게 도달 완료! 최종 거리: {observation.distanceToEnemy:F2}");
                return state;
            }

            // === 2단계: 적 방향 이동 방향 계산 ===
            Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            
            // 디버깅용: 가장 적절한 4방향 이동 타입 결정
            ActionType moveType = GetMoveType(directionToEnemy);
            
            // === 3단계: 이동 액션 실행 ===
            if (agentController != null)
            {
                // AgentController를 통한 이동 액션 생성 및 실행
                AgentAction moveAction = AgentAction.Move(directionToEnemy);
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    // 이동 성공 - 계속 이동 중
                    state = NodeState.Running;
                    Debug.Log($"{agentController.GetAgentName()} 적에게 이동 중... 거리: {observation.distanceToEnemy:F2} (목표: {stoppingDistance:F2})");
                }
                else
                {
                    // 이동 실패
                    state = NodeState.Failure;
                    Debug.LogWarning($"{agentController.GetAgentName()} 적에게 이동 실패: {result.message}");
                }
            }
            else
            {
                // AgentController가 null인 경우
                state = NodeState.Failure;
                Debug.LogError("MoveToEnemyNode: AgentController가 null입니다. Initialize()가 호출되었는지 확인하세요.");
            }

            return state;
        }

        /// <summary>
        /// 3D 방향 벡터를 4방향 이동 타입으로 변환
        /// 가장 강한 방향 성분을 기준으로 최적 이동 방향 선택
        /// </summary>
        /// <param name="direction">정규화된 3D 방향 벡터</param>
        /// <returns>최적화된 4방향 이동 타입</returns>
        private ActionType GetMoveType(Vector3 direction)
        {
            // Z축(Forward/Back) vs X축(Left/Right) 비교로 주도 방향 결정
            if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
            {
                // Z축이 더 강하면 전진/후진 선택
                return direction.z > 0 ? ActionType.MoveForward : ActionType.MoveBack;
            }
            else
            {
                // X축이 더 강하면 좌우 이동 선택
                return direction.x > 0 ? ActionType.MoveRight : ActionType.MoveLeft;
            }
        }
        
        /// <summary>
        /// 정지 거리 설정 (런타임 조정용)
        /// </summary>
        /// <param name="distance">새로운 정지 거리</param>
        public void SetStoppingDistance(float distance)
        {
            stoppingDistance = Mathf.Max(0.1f, distance); // 최소 거리 보장
        }
        
        /// <summary>
        /// 현재 정지 거리 반환
        /// </summary>
        /// <returns>설정된 정지 거리</returns>
        public float GetStoppingDistance()
        {
            return stoppingDistance;
        }
        
        /// <summary>
        /// 이동 완료 예상 시간 계산
        /// 현재 거리와 기본 이동 속도를 기반으로 예측
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <param name="baseSpeed">기본 이동 속도 (기본: 1.0)</param>
        /// <returns>예상 이동 시간 (초)</returns>
        public float EstimateMovementTime(GameObservation observation, float baseSpeed = 1f)
        {
            float remainingDistance = Mathf.Max(0f, observation.distanceToEnemy - stoppingDistance);
            return remainingDistance / Mathf.Max(baseSpeed, 0.1f);
        }
        
        /// <summary>
        /// 이동 진행도 계산 (0.0 ~ 1.0)
        /// 시작 위치에서 목표까지의 진행 정도 반환
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <param name="initialDistance">시작 시점의 거리</param>
        /// <returns>진행도 (0.0 = 시작, 1.0 = 완료)</returns>
        public float GetMovementProgress(GameObservation observation, float initialDistance)
        {
            float currentDistance = observation.distanceToEnemy;
            float targetDistance = stoppingDistance;
            
            if (initialDistance <= targetDistance) return 1f;
            
            float remainingDistance = currentDistance - targetDistance;
            float totalDistance = initialDistance - targetDistance;
            
            return Mathf.Clamp01(1f - (remainingDistance / totalDistance));
        }
    }
}

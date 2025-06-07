using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격 행동을 수행하는 액션 노드
    /// 적을 향해 즉시 공격을 시도하는 BT의 핵심 액션 노드
    /// 
    /// 실행 조건:
    /// - 적이 공격 범위 내에 있어야 함
    /// - 공격 쿨다운이 완료되어야 함
    /// - AgentController가 유효해야 함
    /// 
    /// 실행 과정:
    /// 1. 범위 및 쿨다운 조건 검사
    /// 2. 적 방향으로 즉시 회전 (조준)
    /// 3. AgentController를 통한 공격 액션 실행
    /// 4. 결과에 따른 상태 반환
    /// 
    /// 특징:
    /// - 즉시 실행형 노드 (Running 상태 없음)
    /// - 자동 조준 기능 내장
    /// - 상세한 실행 로깅
    /// </summary>
    public class AttackNode : BTNode
    {
        /// <summary>
        /// 공격 가능 범위 (단위: Unity 유닛)
        /// 이 거리 이내에 적이 있어야 공격 가능
        /// </summary>
        private float attackRange;

        /// <summary>
        /// 공격 노드 생성자
        /// </summary>
        /// <param name="range">공격 범위 (기본값: 2.0 유닛)</param>
        public AttackNode(float range = 2f)
        {
            this.attackRange = range;
        }

        /// <summary>
        /// 공격 로직 실행
        /// 조건 검사 → 조준 → 공격 실행의 순서로 진행
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>공격 성공시 Success, 조건 미충족 또는 실패시 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // === 1단계: 공격 범위 확인 ===
            if (observation.distanceToEnemy > attackRange)
            {
                state = NodeState.Failure;
                return state;
            }

            // === 2단계: 공격 쿨다운 확인 ===
            if (!observation.cooldowns.CanAttack)
            {
                state = NodeState.Failure;
                return state;
            }

            // === 3단계: 공격 전 적을 향해 즉시 조준 ===
            // 정확한 공격을 위해 적 방향으로 즉시 회전
            if (agentController != null && observation.distanceToEnemy <= attackRange)
            {
                Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
                
                // 유효한 방향 벡터인지 확인
                if (directionToEnemy.magnitude > 0.1f)
                {
                    // Y축 제거하여 평면 회전만 수행 (3D 게임에서 일반적)
                    directionToEnemy.y = 0;
                    directionToEnemy = directionToEnemy.normalized;
                    
                    // 목표 회전값 계산 및 즉시 적용
                    Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                    agentController.transform.rotation = targetRotation;
                    
                    Debug.Log($"{agentController.GetAgentName()} 공격 전 적 방향 조준 완료 (거리: {observation.distanceToEnemy:F2})");
                }
            }

            // === 4단계: 공격 실행 ===
            if (agentController != null)
            {
                // AgentController를 통한 공격 액션 생성 및 실행
                AgentAction attackAction = AgentAction.Attack;
                ActionResult result = agentController.ExecuteAction(attackAction);

                // 공격 결과 처리
                if (result.success)
                {
                    // 공격 성공
                    state = NodeState.Success;
                    Debug.Log($"{agentController.GetAgentName()} 공격 성공! 대상: {result.target?.name ?? "Unknown"}");
                    
                    // TODO: 향후 RL 에이전트와의 상호작용을 위한 확장 포인트
                    // if (result.target.TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
                    // {
                    //     defAgent.RegisterOpponentAttack();
                    //     Debug.Log("[BT → RL] 공격 성공 → 수비 에이전트에게 RegisterOpponentAttack 호출됨");
                    // }
                }
                else
                {
                    // 공격 실패
                    state = NodeState.Failure;
                    Debug.Log($"{agentController.GetAgentName()} 공격 실패: {result.message}");
                }
            }
            else
            {
                // AgentController가 null인 경우 (초기화 오류)
                state = NodeState.Failure;
                Debug.LogError("AttackNode: AgentController가 null입니다. Initialize()가 호출되었는지 확인하세요.");
            }

            return state;
        }
        
        /// <summary>
        /// 공격 범위 설정 (런타임 조정용)
        /// </summary>
        /// <param name="range">새로운 공격 범위</param>
        public void SetAttackRange(float range)
        {
            attackRange = Mathf.Max(0.1f, range); // 최소 범위 보장
        }
        
        /// <summary>
        /// 현재 공격 범위 반환
        /// </summary>
        /// <returns>현재 설정된 공격 범위</returns>
        public float GetAttackRange()
        {
            return attackRange;
        }
        
        /// <summary>
        /// 공격 실행 가능 여부 확인 (실제 실행 없이 조건만 검사)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>공격 실행 가능 여부</returns>
        public bool CanExecuteAttack(GameObservation observation)
        {
            return observation.distanceToEnemy <= attackRange && 
                   observation.cooldowns.CanAttack &&
                   agentController != null;
        }
    }
}

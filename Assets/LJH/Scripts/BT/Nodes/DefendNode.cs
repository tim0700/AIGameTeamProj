using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 방어 행동을 수행하는 액션 노드
    /// 적의 공격에 대비하여 방어 자세를 취하는 핵심 액션
    /// 
    /// 실행 조건:
    /// - 방어 쿨다운이 완료되어야 함
    /// - AgentController가 유효해야 함
    /// 
    /// 사용 사례:
    /// - 적 공격 예상시 사전 방어
    /// - 저체력 상태에서 생존 전략
    /// - 시간 벌기를 위한 수비적 행동
    /// - 쿨다운 대기 시간 동안 보호
    /// 
    /// 특징:
    /// - 즉시 실행형 노드
    /// - 간단한 조건 검사
    /// - 낮은 실행 비용
    /// </summary>
    public class DefendNode : BTNode
    {
        public override NodeState Evaluate(GameObservation observation)
        {
            // 방어 쿨타임 확인
            if (!observation.cooldowns.CanDefend)
            {
                state = NodeState.Failure;
                return state;
            }

            // 방어 실행
            if (agentController != null)
            {
                AgentAction defendAction = AgentAction.Defend;
                ActionResult result = agentController.ExecuteAction(defendAction);
                
                if (result.success)
                {
                    state = NodeState.Success;
                    Debug.Log($"{agentController.GetAgentName()} 방어 성공!");
                }
                else
                {
                    state = NodeState.Failure;
                    Debug.Log($"{agentController.GetAgentName()} 방어 실패: {result.message}");
                }
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }
    }
}

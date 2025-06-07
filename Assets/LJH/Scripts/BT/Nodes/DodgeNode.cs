using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 회피 행동을 수행하는 노드
    /// </summary>
    public class DodgeNode : BTNode
    {
        public override NodeState Evaluate(GameObservation observation)
        {
            // 회피 쿨타임 확인
            if (!observation.cooldowns.CanDodge)
            {
                state = NodeState.Failure;
                return state;
            }

            // 회피 실행
            if (agentController != null)
            {
                AgentAction dodgeAction = AgentAction.Dodge;
                ActionResult result = agentController.ExecuteAction(dodgeAction);
                
                if (result.success)
                {
                    state = NodeState.Success;
                    Debug.Log($"{agentController.GetAgentName()} 회피 성공!");
                }
                else
                {
                    state = NodeState.Failure;
                    Debug.Log($"{agentController.GetAgentName()} 회피 실패: {result.message}");
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

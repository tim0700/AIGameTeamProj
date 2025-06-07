using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 방어 행동을 수행하는 노드
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

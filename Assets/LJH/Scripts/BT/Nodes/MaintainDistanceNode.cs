using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적과 일정 거리를 유지하는 노드 (수비형 전략)
    /// </summary>
    public class MaintainDistanceNode : BTNode
    {
        private float preferredDistance;
        private float tolerance;

        public MaintainDistanceNode(float preferredDistance = 4f, float tolerance = 1f)
        {
            this.preferredDistance = preferredDistance;
            this.tolerance = tolerance;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            float currentDistance = observation.distanceToEnemy;
            
            // 적절한 거리에 있으면 성공
            if (Mathf.Abs(currentDistance - preferredDistance) <= tolerance)
            {
                state = NodeState.Success;
                return state;
            }

            Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            Vector3 moveDirection;

            if (currentDistance < preferredDistance)
            {
                // 너무 가까우면 후퇴
                moveDirection = -directionToEnemy;
                Debug.Log($"{agentController?.GetAgentName()} 거리 유지를 위해 후퇴");
            }
            else
            {
                // 너무 멀면 접근
                moveDirection = directionToEnemy;
                Debug.Log($"{agentController?.GetAgentName()} 거리 유지를 위해 접근");
            }

            if (agentController != null)
            {
                AgentAction moveAction = AgentAction.Move(moveDirection);
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    state = NodeState.Running;
                }
                else
                {
                    state = NodeState.Failure;
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

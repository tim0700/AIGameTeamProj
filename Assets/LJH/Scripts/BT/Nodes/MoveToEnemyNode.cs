using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적에게 이동하는 노드
    /// </summary>
    public class MoveToEnemyNode : BTNode
    {
        private float stoppingDistance;

        public MoveToEnemyNode(float stoppingDistance = 2f)
        {
            this.stoppingDistance = stoppingDistance;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 이미 충분히 가까이 있으면 성공
            if (observation.distanceToEnemy <= stoppingDistance)
            {
                state = NodeState.Success;
                return state;
            }

            // 적 방향으로 이동
            Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            
            // 가장 적절한 이동 방향 결정 (4방향)
            ActionType moveType = GetMoveType(directionToEnemy);
            
            if (agentController != null)
            {
                AgentAction moveAction = AgentAction.Move(directionToEnemy);
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    state = NodeState.Running; // 계속 이동 중
                    Debug.Log($"{agentController.GetAgentName()} 적에게 이동 중... 거리: {observation.distanceToEnemy:F2}");
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

        private ActionType GetMoveType(Vector3 direction)
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
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격 행동을 수행하는 노드
    /// </summary>
    public class AttackNode : BTNode
    {
        private float attackRange;

        public AttackNode(float range = 2f)
        {
            this.attackRange = range;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 공격 범위 확인
            if (observation.distanceToEnemy > attackRange)
            {
                state = NodeState.Failure;
                return state;
            }

            // 쿨타임 확인
            if (!observation.cooldowns.CanAttack)
            {
                state = NodeState.Failure;
                return state;
            }

            // 공격 실행 (AgentController를 통해)
            if (agentController != null)
            {
                AgentAction attackAction = AgentAction.Attack;
                ActionResult result = agentController.ExecuteAction(attackAction);
                
                if (result.success)
                {
                    state = NodeState.Success;
                    Debug.Log($"{agentController.GetAgentName()} 공격 성공!");
                }
                else
                {
                    state = NodeState.Failure;
                    Debug.Log($"{agentController.GetAgentName()} 공격 실패: {result.message}");
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

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

            // 🎯 공격 전 적을 향해 즉시 회전
            if (agentController != null && observation.distanceToEnemy <= attackRange)
            {
                Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
                if (directionToEnemy.magnitude > 0.1f)
                {
                    // Y축 제거 (평면 회전)
                    directionToEnemy.y = 0;
                    directionToEnemy = directionToEnemy.normalized;
                    
                    // 즉시 회전 (비동기 아님)
                    Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                    agentController.transform.rotation = targetRotation;
                    
                    Debug.Log($"{agentController.GetAgentName()} 공격 전 적 방향 회전 완료");
                }
            }

            // 공격 실행 (AgentController를 통해)
            if (agentController != null)
            {
                AgentAction attackAction = AgentAction.Attack;
                ActionResult result = agentController.ExecuteAction(attackAction);

                if (result.success)
                //  if (result.target.TryGetComponent<PlayerRLAgent_Defense>(out var defAgent))
                //     {
                //         defAgent.RegisterOpponentAttack();
                //         Debug.Log("[BT → RL] 공격 성공 → 수비 에이전트에게 RegisterOpponentAttack 호출됨");
                //     }
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

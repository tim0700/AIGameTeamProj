using UnityEngine;

namespace BehaviorTree
{
    public class AttackAction : ActionNode
    {
        private BTAgent btAgent;

        public AttackAction(GameObject agent) : base(agent)
        {
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            if (!btAgent.IsAttackReady())
            {
                state = NodeState.FAILURE;
                return state;
            }

            GameObject enemy = (GameObject)GetData("enemy");
            if (enemy == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Face enemy before attacking
            Vector3 direction = (enemy.transform.position - agent.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                agent.transform.rotation = Quaternion.LookRotation(direction);
            }

            // Perform attack
            btAgent.lastAttackTime = Time.time;
            btAgent.isAttacking = true;

            // Record attack attempt
            if (SimulationDataCollector.Instance != null)
            {
                SimulationDataCollector.Instance.RecordAttackAttempt(btAgent);
            }

            // Trigger attack animation
            if (btAgent.animator != null)
            {
                btAgent.animator.SetTrigger("Attack");
            }

            // Deal damage to enemy if in range
            float distance = Vector3.Distance(agent.transform.position, enemy.transform.position);
            if (distance <= btAgent.attackRange)
            {
                BTAgent enemyAgent = enemy.GetComponent<BTAgent>();
                if (enemyAgent != null && !enemyAgent.IsDefending() && !enemyAgent.IsEvading())
                {
                    enemyAgent.TakeDamage(btAgent.attackDamage);
                    
                    // Record successful attack
                    if (SimulationDataCollector.Instance != null)
                    {
                        SimulationDataCollector.Instance.RecordAttackSuccess(btAgent, btAgent.attackDamage);
                    }
                }
            }

            Debug.Log($"{agent.name} attacks!");

            state = NodeState.SUCCESS;
            return state;
        }
    }
}

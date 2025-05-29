using UnityEngine;

namespace BehaviorTree
{
    public class EnemyInRangeCondition : ConditionNode
    {
        private float range;
        private BTAgent btAgent;

        public EnemyInRangeCondition(GameObject agent, float range) : base(agent)
        {
            this.range = range;
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            GameObject enemy = btAgent.FindEnemy();
            
            if (enemy == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = Vector3.Distance(agent.transform.position, enemy.transform.position);
            
            if (distance <= range)
            {
                btAgent.enemy = enemy;
                SetData("enemy", enemy);
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }
}

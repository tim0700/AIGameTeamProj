using UnityEngine;

namespace BehaviorTree
{
    public class EnemyStateCondition : ConditionNode
    {
        public enum EnemyState
        {
            Attacking,
            NotAttacking,
            Vulnerable
        }

        private EnemyState checkState;
        private BTAgent btAgent;

        public EnemyStateCondition(GameObject agent, EnemyState checkState) : base(agent)
        {
            this.checkState = checkState;
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            GameObject enemy = (GameObject)GetData("enemy");
            if (enemy == null)
            {
                enemy = btAgent.FindEnemy();
                if (enemy == null)
                {
                    state = NodeState.FAILURE;
                    return state;
                }
            }

            BTAgent enemyAgent = enemy.GetComponent<BTAgent>();
            if (enemyAgent == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            bool conditionMet = false;

            switch (checkState)
            {
                case EnemyState.Attacking:
                    conditionMet = enemyAgent.IsAttacking();
                    break;
                case EnemyState.NotAttacking:
                    conditionMet = !enemyAgent.IsAttacking();
                    break;
                case EnemyState.Vulnerable:
                    // Enemy is vulnerable right after attack
                    conditionMet = !enemyAgent.IsAttacking() && !enemyAgent.IsDefending() && !enemyAgent.IsEvading();
                    break;
            }

            state = conditionMet ? NodeState.SUCCESS : NodeState.FAILURE;
            return state;
        }
    }
}

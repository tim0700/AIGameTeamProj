using UnityEngine;

namespace BehaviorTree.Nodes
{
    // 적이 감지 범위 내에 있는지 확인
    public class CheckEnemyInRange : ConditionNode
    {
        public CheckEnemyInRange(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            Transform enemy = agent.FindNearestEnemy();
            
            if (enemy != null)
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }

    // 적이 공격 범위 내에 있는지 확인
    public class CheckEnemyInAttackRange : ConditionNode
    {
        public CheckEnemyInAttackRange(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (agent.currentTarget != null && agent.IsTargetInAttackRange())
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }

    // 체력이 일정 수치 이하인지 확인
    public class CheckLowHealth : ConditionNode
    {
        private float threshold;

        public CheckLowHealth(Agent agent, float healthThreshold = 0.3f) : base(agent)
        {
            this.threshold = healthThreshold;
        }

        public override NodeState Evaluate()
        {
            if (agent.GetHealthPercentage() <= threshold)
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }

    // 공격 가능한지 확인 (쿨타임)
    public class CheckCanAttack : ConditionNode
    {
        public CheckCanAttack(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (agent.CanAttack())
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }

    // 방어 가능한지 확인 (쿨타임)
    public class CheckCanDefend : ConditionNode
    {
        public CheckCanDefend(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (agent.CanDefend())
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }

    // 회피 가능한지 확인 (쿨타임)
    public class CheckCanDodge : ConditionNode
    {
        public CheckCanDodge(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (agent.CanDodge())
            {
                state = NodeState.SUCCESS;
            }
            else
            {
                state = NodeState.FAILURE;
            }

            return state;
        }
    }
    
    // 랜덤 확률 체크
    public class RandomChance : ConditionNode
    {
        private float chance;
        
        public RandomChance(float probability) : base(null)
        {
            this.chance = probability;
        }
        
        public override NodeState Evaluate()
        {
            if (Random.value <= chance)
            {
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

using UnityEngine;

namespace BehaviorTree
{
    public class HealthBelowThresholdCondition : ConditionNode
    {
        private float threshold;
        private BTAgent btAgent;

        public HealthBelowThresholdCondition(GameObject agent, float threshold) : base(agent)
        {
            this.threshold = threshold;
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            float healthPercentage = btAgent.GetHealthPercentage();
            
            if (healthPercentage <= threshold)
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

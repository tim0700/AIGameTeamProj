using UnityEngine;

namespace BehaviorTree
{
    public abstract class ConditionNode : Node
    {
        protected GameObject agent;

        public ConditionNode(GameObject agent)
        {
            this.agent = agent;
        }

        public override abstract NodeState Evaluate();
    }
}

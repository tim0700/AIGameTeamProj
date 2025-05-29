using UnityEngine;

namespace BehaviorTree
{
    public abstract class ActionNode : Node
    {
        protected GameObject agent;

        public ActionNode(GameObject agent)
        {
            this.agent = agent;
        }

        public override abstract NodeState Evaluate();
    }
}

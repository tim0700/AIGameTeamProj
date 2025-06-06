using UnityEngine;

namespace LJH.BT
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    public abstract class BTNode : IBTNode
    {
        protected NodeState state;
        protected AgentController agentController;

        public NodeState GetState() => state;

        public virtual void Initialize(AgentController controller)
        {
            agentController = controller;
        }

        public abstract NodeState Evaluate(GameObservation observation);
    }
}

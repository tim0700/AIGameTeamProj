using UnityEngine;

namespace BehaviorTree
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    public abstract class Node
    {
        protected NodeState state;
        public Node parent;
        protected Agent agent;  // 에이전트 참조

        public NodeState State { get { return state; } }

        public Node()
        {
            parent = null;
        }

        public Node(Agent agent)
        {
            this.agent = agent;
            parent = null;
        }

        public abstract NodeState Evaluate();

        public virtual void SetAgent(Agent agent)
        {
            this.agent = agent;
        }
    }
}

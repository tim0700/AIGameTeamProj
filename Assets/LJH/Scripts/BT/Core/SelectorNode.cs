using System.Collections.Generic;

namespace LJH.BT
{
    public class SelectorNode : BTNode
    {
        private List<BTNode> children;

        public SelectorNode(List<BTNode> children)
        {
            this.children = children;
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            foreach (BTNode child in children)
            {
                child.Initialize(controller);
            }
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            foreach (BTNode child in children)
            {
                switch (child.Evaluate(observation))
                {
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                }
            }

            state = NodeState.Failure;
            return state;
        }
    }
}

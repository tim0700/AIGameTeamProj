using System.Collections.Generic;

namespace BehaviorTree
{
    public class Inverter : Node
    {
        public Inverter(Node child) : base(new List<Node> { child }) { }

        public override NodeState Evaluate()
        {
            switch (children[0].Evaluate())
            {
                case NodeState.FAILURE:
                    state = NodeState.SUCCESS;
                    return state;
                case NodeState.SUCCESS:
                    state = NodeState.FAILURE;
                    return state;
                case NodeState.RUNNING:
                    state = NodeState.RUNNING;
                    return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}

using System.Collections.Generic;

namespace BehaviorTree
{
    public class Parallel : Node
    {
        private int successThreshold;

        public Parallel(int successThreshold = 1) : base()
        {
            this.successThreshold = successThreshold;
        }

        public Parallel(List<Node> children, int successThreshold = 1) : base(children)
        {
            this.successThreshold = successThreshold;
        }

        public override NodeState Evaluate()
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.SUCCESS:
                        successCount++;
                        break;
                    case NodeState.FAILURE:
                        failureCount++;
                        break;
                    case NodeState.RUNNING:
                        continue;
                }
            }

            if (successCount >= successThreshold)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            if (failureCount > children.Count - successThreshold)
            {
                state = NodeState.FAILURE;
                return state;
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}

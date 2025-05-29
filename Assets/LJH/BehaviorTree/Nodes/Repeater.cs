using System.Collections.Generic;

namespace BehaviorTree
{
    public class Repeater : Node
    {
        private int repeatCount;
        private int currentCount;
        private bool repeatForever;

        public Repeater(Node child, int repeatCount = -1) : base(new List<Node> { child })
        {
            this.repeatCount = repeatCount;
            this.repeatForever = repeatCount < 0;
            this.currentCount = 0;
        }

        public override NodeState Evaluate()
        {
            if (!repeatForever && currentCount >= repeatCount)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            NodeState childState = children[0].Evaluate();

            switch (childState)
            {
                case NodeState.RUNNING:
                    state = NodeState.RUNNING;
                    break;
                case NodeState.SUCCESS:
                    currentCount++;
                    if (repeatForever || currentCount < repeatCount)
                    {
                        state = NodeState.RUNNING;
                    }
                    else
                    {
                        state = NodeState.SUCCESS;
                    }
                    break;
                case NodeState.FAILURE:
                    state = NodeState.FAILURE;
                    break;
            }

            return state;
        }

        public void Reset()
        {
            currentCount = 0;
        }
    }
}

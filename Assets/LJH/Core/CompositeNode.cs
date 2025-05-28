using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeNode : Node
    {
        protected List<Node> children = new List<Node>();

        public CompositeNode() : base() { }
        public CompositeNode(Agent agent) : base(agent) { }

        public void AddChild(Node child)
        {
            child.parent = this;
            child.SetAgent(this.agent);
            children.Add(child);
        }

        public override void SetAgent(Agent agent)
        {
            base.SetAgent(agent);
            foreach (Node child in children)
            {
                child.SetAgent(agent);
            }
        }
    }

    // Selector: 자식 중 하나가 성공할 때까지 순서대로 실행
    public class Selector : CompositeNode
    {
        public Selector() : base() { }
        public Selector(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            foreach (Node child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue;
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;
                    default:
                        continue;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }

    // Sequence: 모든 자식이 성공할 때까지 순서대로 실행
    public class Sequence : CompositeNode
    {
        public Sequence() : base() { }
        public Sequence(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node child in children)
            {
                switch (child.Evaluate())
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        anyChildRunning = true;
                        continue;
                    default:
                        state = NodeState.SUCCESS;
                        return state;
                }
            }

            state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }
}

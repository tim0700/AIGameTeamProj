namespace BehaviorTree
{
    public abstract class ActionNode : Node
    {
        public ActionNode() : base() { }
        public ActionNode(Agent agent) : base(agent) { }

        public abstract override NodeState Evaluate();
    }

    public abstract class ConditionNode : Node
    {
        public ConditionNode() : base() { }
        public ConditionNode(Agent agent) : base(agent) { }

        public abstract override NodeState Evaluate();
    }
}

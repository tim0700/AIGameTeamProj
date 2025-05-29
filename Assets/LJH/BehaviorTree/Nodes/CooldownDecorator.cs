using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class CooldownDecorator : Node
    {
        private float cooldownTime;
        private float lastExecutionTime = -999f;

        public CooldownDecorator(Node child, float cooldownTime) : base(new List<Node> { child })
        {
            this.cooldownTime = cooldownTime;
        }

        public override NodeState Evaluate()
        {
            if (Time.time - lastExecutionTime < cooldownTime)
            {
                state = NodeState.FAILURE;
                return state;
            }

            NodeState childState = children[0].Evaluate();

            if (childState == NodeState.SUCCESS)
            {
                lastExecutionTime = Time.time;
            }

            state = childState;
            return state;
        }
    }
}

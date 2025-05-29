using UnityEngine;

namespace BehaviorTree
{
    public class DefenseAction : ActionNode
    {
        private BTAgent btAgent;

        public DefenseAction(GameObject agent) : base(agent)
        {
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            if (!btAgent.IsDefenseReady())
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Perform defense
            btAgent.lastDefenseTime = Time.time;
            btAgent.isDefending = true;

            // Record defense attempt
            if (SimulationDataCollector.Instance != null)
            {
                SimulationDataCollector.Instance.RecordDefenseAttempt(btAgent);
            }

            // Trigger defense animation
            if (btAgent.animator != null)
            {
                btAgent.animator.SetTrigger("Defense");
            }

            Debug.Log($"{agent.name} defends!");

            state = NodeState.SUCCESS;
            return state;
        }
    }
}

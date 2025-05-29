using UnityEngine;

namespace BehaviorTree
{
    public class EvasionAction : ActionNode
    {
        private BTAgent btAgent;
        private float evasionDistance = 3f;

        public EvasionAction(GameObject agent) : base(agent)
        {
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            if (!btAgent.IsEvasionReady())
            {
                state = NodeState.FAILURE;
                return state;
            }

            GameObject enemy = (GameObject)GetData("enemy");
            Vector3 evasionDirection;

            if (enemy != null)
            {
                // Evade perpendicular to enemy direction
                Vector3 toEnemy = (enemy.transform.position - agent.transform.position).normalized;
                evasionDirection = Vector3.Cross(toEnemy, Vector3.up);
                
                // Randomly choose left or right
                if (Random.Range(0f, 1f) > 0.5f)
                {
                    evasionDirection *= -1f;
                }
            }
            else
            {
                // Random evasion direction if no enemy
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                evasionDirection = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            }

            // Perform evasion
            btAgent.lastEvasionTime = Time.time;
            btAgent.isEvading = true;

            // Record evasion attempt
            if (SimulationDataCollector.Instance != null)
            {
                SimulationDataCollector.Instance.RecordEvasionAttempt(btAgent);
            }

            // Quick dodge movement
            agent.transform.position += evasionDirection * evasionDistance;

            // Trigger evasion animation
            if (btAgent.animator != null)
            {
                btAgent.animator.SetTrigger("Evasion");
            }

            Debug.Log($"{agent.name} evades!");

            state = NodeState.SUCCESS;
            return state;
        }
    }
}

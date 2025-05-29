using UnityEngine;

namespace BehaviorTree
{
    public class PatrolAction : ActionNode
    {
        private BTAgent btAgent;
        private Vector3 targetPosition;
        private float patrolRadius = 5f;
        private float moveSpeed;
        private bool hasTarget = false;

        public PatrolAction(GameObject agent, float moveSpeed) : base(agent)
        {
            this.btAgent = agent.GetComponent<BTAgent>();
            this.moveSpeed = moveSpeed;
        }

        public override NodeState Evaluate()
        {
            // Generate new patrol target if needed
            if (!hasTarget || Vector3.Distance(agent.transform.position, targetPosition) < 1f)
            {
                GenerateNewTarget();
            }

            // Move towards target
            Vector3 direction = (targetPosition - agent.transform.position).normalized;
            direction.y = 0;

            agent.transform.position += direction * moveSpeed * 0.5f * Time.deltaTime; // Slower patrol speed
            
            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            // Set animation parameters
            if (btAgent.animator != null)
            {
                btAgent.animator.SetFloat("MoveSpeed", 0.5f);
                btAgent.animator.SetFloat("MoveX", direction.x * 0.5f);
                btAgent.animator.SetFloat("MoveZ", direction.z * 0.5f);
            }

            state = NodeState.RUNNING;
            return state;
        }

        private void GenerateNewTarget()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(2f, patrolRadius);
            
            targetPosition = agent.transform.position + new Vector3(
                Mathf.Sin(angle) * distance,
                0,
                Mathf.Cos(angle) * distance
            );
            
            hasTarget = true;
        }
    }
}

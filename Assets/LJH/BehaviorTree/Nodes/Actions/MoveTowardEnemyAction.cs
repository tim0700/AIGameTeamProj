using UnityEngine;

namespace BehaviorTree
{
    public class MoveTowardEnemyAction : ActionNode
    {
        private BTAgent btAgent;
        private float moveSpeed;

        public MoveTowardEnemyAction(GameObject agent, float moveSpeed) : base(agent)
        {
            this.btAgent = agent.GetComponent<BTAgent>();
            this.moveSpeed = moveSpeed;
        }

        public override NodeState Evaluate()
        {
            GameObject enemy = (GameObject)GetData("enemy");
            if (enemy == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Vector3 direction = (enemy.transform.position - agent.transform.position).normalized;
            direction.y = 0; // Keep movement on horizontal plane

            // Move towards enemy
            agent.transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotate to face enemy
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 10f);
            }

            // Set animation parameters if needed
            if (btAgent.animator != null)
            {
                btAgent.animator.SetFloat("MoveSpeed", 1.0f);
                btAgent.animator.SetFloat("MoveX", direction.x);
                btAgent.animator.SetFloat("MoveZ", direction.z);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}

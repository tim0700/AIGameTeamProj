using UnityEngine;

namespace BehaviorTree
{
    public class MoveAwayFromEnemyAction : ActionNode
    {
        private BTAgent btAgent;
        private float moveSpeed;

        public MoveAwayFromEnemyAction(GameObject agent, float moveSpeed) : base(agent)
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

            Vector3 direction = (agent.transform.position - enemy.transform.position).normalized;
            direction.y = 0; // Keep movement on horizontal plane

            // Move away from enemy
            agent.transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Still face the enemy while backing away
            Vector3 lookDirection = (enemy.transform.position - agent.transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 10f);
            }

            // Set animation parameters if needed
            if (btAgent.Animator != null)
            {
                btAgent.Animator.SetFloat("MoveSpeed", 1.0f);
                btAgent.Animator.SetFloat("MoveX", -direction.x);
                btAgent.Animator.SetFloat("MoveZ", -direction.z);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}

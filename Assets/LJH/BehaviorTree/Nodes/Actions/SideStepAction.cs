using UnityEngine;

namespace BehaviorTree
{
    public class SideStepAction : ActionNode
    {
        private BTAgent btAgent;
        private float moveSpeed;
        private float sideStepDirection = 1f; // 1 for right, -1 for left

        public SideStepAction(GameObject agent, float moveSpeed) : base(agent)
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

            // Change direction randomly sometimes
            if (Random.Range(0f, 1f) < 0.1f * Time.deltaTime)
            {
                sideStepDirection *= -1f;
            }

            // Calculate perpendicular direction for side movement
            Vector3 toEnemy = (enemy.transform.position - agent.transform.position).normalized;
            Vector3 sideDirection = Vector3.Cross(toEnemy, Vector3.up) * sideStepDirection;
            sideDirection.y = 0;

            // Move sideways
            agent.transform.position += sideDirection * moveSpeed * Time.deltaTime;
            
            // Face the enemy
            if (toEnemy != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(toEnemy);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, lookRotation, Time.deltaTime * 10f);
            }

            // Set animation parameters if needed
            if (btAgent.Animator != null)
            {
                btAgent.Animator.SetFloat("MoveSpeed", 1.0f);
                btAgent.Animator.SetFloat("MoveX", sideDirection.x);
                btAgent.Animator.SetFloat("MoveZ", sideDirection.z);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}

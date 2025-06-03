using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 순찰 행동을 수행하는 노드 (기본 행동)
    /// </summary>
    public class PatrolNode : BTNode
    {
        private Vector3 patrolCenter;
        private float patrolRadius;
        private Vector3 currentTarget;
        private float targetReachDistance = 1f;

        public PatrolNode(float radius = 3f)
        {
            this.patrolRadius = radius;
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            patrolCenter = controller.transform.position;
            GenerateNewTarget();
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 현재 타겟에 도달했는지 확인
            float distanceToTarget = Vector3.Distance(observation.selfPosition, currentTarget);
            
            if (distanceToTarget <= targetReachDistance)
            {
                GenerateNewTarget();
            }

            // 타겟 방향으로 이동
            Vector3 directionToTarget = (currentTarget - observation.selfPosition).normalized;

            if (agentController != null)
            {
                AgentAction moveAction = AgentAction.Move(directionToTarget);
                ActionResult result = agentController.ExecuteAction(moveAction);
                
                if (result.success)
                {
                    state = NodeState.Running;
                    Debug.Log($"{agentController.GetAgentName()} 순찰 중...");
                }
                else
                {
                    state = NodeState.Failure;
                }
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }

        private void GenerateNewTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentTarget = patrolCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
    }
}

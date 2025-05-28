using UnityEngine;

namespace BehaviorTree.Nodes
{
    // 적에게 접근
    public class MoveToTarget : ActionNode
    {
        private float acceptableDistance;

        public MoveToTarget(Agent agent, float distance = 2f) : base(agent)
        {
            this.acceptableDistance = distance;
        }

        public override NodeState Evaluate()
        {
            if (agent.currentTarget == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = agent.GetDistanceToTarget();
            
            if (distance <= acceptableDistance)
            {
                agent.MoveTo(Vector3.zero); // 정지
                state = NodeState.SUCCESS;
                return state;
            }

            Vector3 direction = agent.GetDirectionToTarget();
            agent.MoveTo(direction);
            
            state = NodeState.RUNNING;
            return state;
        }
    }

    // 적에게서 거리 유지 (후퇴)
    public class KeepDistance : ActionNode
    {
        private float desiredDistance;

        public KeepDistance(Agent agent, float distance = 4f) : base(agent)
        {
            this.desiredDistance = distance;
        }

        public override NodeState Evaluate()
        {
            if (agent.currentTarget == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = agent.GetDistanceToTarget();
            
            if (distance >= desiredDistance)
            {
                agent.MoveTo(Vector3.zero); // 정지
                state = NodeState.SUCCESS;
                return state;
            }

            // 적에서 멀어지는 방향으로 이동
            Vector3 direction = -agent.GetDirectionToTarget();
            agent.MoveTo(direction);
            
            state = NodeState.RUNNING;
            return state;
        }
    }

    // 랜덤한 방향으로 이동 (순찰)
    public class Patrol : ActionNode
    {
        private Vector3 targetPosition;
        private float patrolRange;
        private Vector3 originalPosition;
        private bool hasTarget = false;

        public Patrol(Agent agent, float range = 5f) : base(agent)
        {
            this.patrolRange = range;
            this.originalPosition = agent.transform.position;
        }

        public override NodeState Evaluate()
        {
            if (!hasTarget || Vector3.Distance(agent.transform.position, targetPosition) < 1f)
            {
                // 새로운 순찰 지점 설정
                Vector2 randomCircle = Random.insideUnitCircle * patrolRange;
                targetPosition = originalPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                hasTarget = true;
            }

            Vector3 direction = (targetPosition - agent.transform.position).normalized;
            agent.MoveTo(direction);
            
            state = NodeState.RUNNING;
            return state;
        }
    }

    // 공격 실행
    public class AttackTarget : ActionNode
    {
        public AttackTarget(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (agent.currentTarget == null || !agent.CanAttack())
            {
                state = NodeState.FAILURE;
                return state;
            }

            agent.LookAtTarget(agent.currentTarget);
            agent.Attack();
            
            state = NodeState.SUCCESS;
            return state;
        }
    }

    // 방어 실행
    public class DefendAction : ActionNode
    {
        public DefendAction(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (!agent.CanDefend())
            {
                state = NodeState.FAILURE;
                return state;
            }

            agent.Defend();
            
            state = NodeState.SUCCESS;
            return state;
        }
    }

    // 회피 실행
    public class DodgeAction : ActionNode
    {
        public DodgeAction(Agent agent) : base(agent) { }

        public override NodeState Evaluate()
        {
            if (!agent.CanDodge() || agent.currentTarget == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // 4방향 회피: 좌, 우, 뒤로 회피 (전방은 적에게 가기 때문에 제외)
            Vector3 dodgeDirection = Vector3.zero;
            float randomValue = Random.value;
            
            if (randomValue < 0.33f)
            {
                // 왼쪽으로 회피
                dodgeDirection = -agent.transform.right;
            }
            else if (randomValue < 0.66f)
            {
                // 오른쪽으로 회피
                dodgeDirection = agent.transform.right;
            }
            else
            {
                // 뒤로 회피
                dodgeDirection = -agent.transform.forward;
            }

            agent.Dodge(dodgeDirection);
            
            state = NodeState.SUCCESS;
            return state;
        }
    }

    // 원형으로 이동 (적 주위를 돌기)
    public class CircleAroundTarget : ActionNode
    {
        private float circleRadius;
        private bool clockwise;

        public CircleAroundTarget(Agent agent, float radius = 3f, bool clockwise = true) : base(agent)
        {
            this.circleRadius = radius;
            this.clockwise = clockwise;
        }

        public override NodeState Evaluate()
        {
            if (agent.currentTarget == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Vector3 directionToTarget = agent.GetDirectionToTarget();
            Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized;
            
            if (!clockwise)
                perpendicularDirection = -perpendicularDirection;

            // 원하는 위치 계산
            Vector3 desiredPosition = agent.currentTarget.position + (-directionToTarget * circleRadius);
            Vector3 moveDirection = perpendicularDirection;

            agent.MoveTo(moveDirection);
            
            state = NodeState.RUNNING;
            return state;
        }
    }
}

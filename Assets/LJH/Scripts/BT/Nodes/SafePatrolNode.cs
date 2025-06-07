using UnityEngine;

namespace LJH.BT
{
    public class SafePatrolNode : BTNode
    {
        public float patrolRadius = 3f;
        public float safeZoneRadius = 0.75f;
        public float reachThreshold = 0.5f;
        public int waitFrames = 60;
        
        private Vector3 patrolTarget;
        private bool isMovingToTarget = false;
        private int waitFrameCounter = 0;
        private bool isWaiting = false;
        private int consecutiveFailures = 0;
        private const int maxFailures = 3;
        
        private int lastValidationFrame = -1;
        private bool hasValidArenaData = false;

        public SafePatrolNode() { }

        public SafePatrolNode(float radius, float safeZone = 0.75f)
        {
            patrolRadius = radius;
            safeZoneRadius = safeZone;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            if (agentController == null)
            {
                state = NodeState.Failure;
                return state;
            }

            if (!ValidateArenaData(observation))
            {
                Debug.LogWarning("[SafePatrol] 아레나 데이터 무효");
                state = NodeState.Success;
                return state;
            }

            if (isWaiting)
            {
                waitFrameCounter++;
                if (waitFrameCounter >= waitFrames)
                {
                    isWaiting = false;
                    isMovingToTarget = false;
                    waitFrameCounter = 0;
                    state = NodeState.Success;
                    return state;
                }
                state = NodeState.Running;
                return state;
            }

            if (!isMovingToTarget || !IsTargetSafe(observation))
            {
                if (SetSafePatrolTarget(observation))
                {
                    isMovingToTarget = true;
                    consecutiveFailures = 0;
                }
                else
                {
                    state = NodeState.Failure;
                    return state;
                }
            }

            float distanceToTarget = Vector3.Distance(observation.selfPosition, patrolTarget);
            
            if (distanceToTarget < reachThreshold)
            {
                isWaiting = true;
                waitFrameCounter = 0;
                state = NodeState.Running;
                return state;
            }

            bool moveSuccess = ExecutePatrolMovement(observation);
            
            if (moveSuccess)
            {
                consecutiveFailures = 0;
                state = NodeState.Running;
            }
            else
            {
                consecutiveFailures++;
                if (consecutiveFailures >= maxFailures)
                {
                    isMovingToTarget = false;
                    state = NodeState.Failure;
                }
                else
                {
                    state = NodeState.Running;
                }
            }

            return state;
        }

        private bool ValidateArenaData(GameObservation observation)
        {
            if (lastValidationFrame == Time.frameCount)
            {
                return hasValidArenaData;
            }

            lastValidationFrame = Time.frameCount;
            
            bool centerValid = observation.arenaCenter != Vector3.zero;
            bool radiusValid = observation.arenaRadius > 0.1f;
            bool selfPosValid = !float.IsNaN(observation.selfPosition.x) && 
                               !float.IsInfinity(observation.selfPosition.x);
            
            hasValidArenaData = centerValid && radiusValid && selfPosValid;
            return hasValidArenaData;
        }

        private bool ExecutePatrolMovement(GameObservation observation)
        {
            Vector3 moveDirection = (patrolTarget - observation.selfPosition).normalized;
            
            if (float.IsNaN(moveDirection.x) || float.IsInfinity(moveDirection.x))
            {
                return false;
            }
            
            moveDirection.y = 0;
            moveDirection = moveDirection.normalized;
            
            AgentAction moveAction = AgentAction.Move(moveDirection);
            ActionResult result = agentController.ExecuteAction(moveAction);
            
            return result.success;
        }

        private bool SetSafePatrolTarget(GameObservation observation)
        {
            int attempts = 0;
            const int maxAttempts = 10;
            
            do
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.5f, patrolRadius);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );
                
                patrolTarget = observation.selfPosition + offset;
                attempts++;
                
            } while (!IsPositionSafe(patrolTarget, observation) && attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                try
                {
                    Vector3 directionToCenter = (observation.arenaCenter - observation.selfPosition).normalized;
                    float safeDistance = observation.arenaRadius * safeZoneRadius * 0.8f;
                    patrolTarget = observation.arenaCenter + directionToCenter * safeDistance;
                    return IsPositionSafe(patrolTarget, observation);
                }
                catch
                {
                    return false;
                }
            }
            
            return true;
        }

        private bool IsPositionSafe(Vector3 position, GameObservation observation)
        {
            try
            {
                float distanceFromCenter = Vector3.Distance(position, observation.arenaCenter);
                float safeZoneActualRadius = observation.arenaRadius * safeZoneRadius;
                return distanceFromCenter <= safeZoneActualRadius;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTargetSafe(GameObservation observation)
        {
            if (patrolTarget == Vector3.zero) return false;
            return IsPositionSafe(patrolTarget, observation);
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            isMovingToTarget = false;
            isWaiting = false;
            consecutiveFailures = 0;
            waitFrameCounter = 0;
            lastValidationFrame = -1;
            hasValidArenaData = false;
        }

        // override 키워드 없는 일반 메서드들
        public void ResetNode()
        {
            state = NodeState.Running;
            isMovingToTarget = false;
            isWaiting = false;
            consecutiveFailures = 0;
            waitFrameCounter = 0;
            patrolTarget = Vector3.zero;
            lastValidationFrame = -1;
            hasValidArenaData = false;
        }

        public string GetNodeStatus()
        {
            return $"상태: {state}, 순찰중: {isMovingToTarget}, 대기: {isWaiting} ({waitFrameCounter}/{waitFrames})";
        }

        public bool IsPatrolling() => isMovingToTarget;
        public bool IsWaiting() => isWaiting;
        public Vector3 GetCurrentTarget() => patrolTarget;
    }
}

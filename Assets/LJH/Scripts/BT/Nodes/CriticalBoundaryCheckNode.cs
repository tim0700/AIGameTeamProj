using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 극한 상황에서만 작동하는 아레나 경계 체크 노드
    /// 95% 이상의 아레나 경계에서만 Success 반환
    /// </summary>
    public class CriticalBoundaryCheckNode : BTNode
    {
        private float criticalThreshold = 0.95f; // 95% 이상에서만 작동

        public CriticalBoundaryCheckNode(float threshold = 0.95f)
        {
            criticalThreshold = threshold;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 현재 위치와 아레나 중심점 간의 거리 계산
            float distanceFromCenter = Vector3.Distance(
                observation.selfPosition, 
                observation.arenaCenter
            );

            // 정규화된 거리 (0~1, 1이 경계)
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;

            // 극한 상황에서만 Success 반환
            if (normalizedDistance >= criticalThreshold)
            {
                Debug.LogError($"[CriticalBoundary] 극한 상황! Distance: {normalizedDistance:F2} (>= {criticalThreshold}) - 즉시 복귀 필요!");
                state = NodeState.Success;
            }
            else
            {
                // 95% 미만에서는 다른 행동 허용
                state = NodeState.Failure;
            }

            return state;
        }
    }
}

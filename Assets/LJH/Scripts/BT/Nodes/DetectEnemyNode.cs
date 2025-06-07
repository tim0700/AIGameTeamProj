using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적의 거리를 감지하는 노드
    /// </summary>
    public class DetectEnemyNode : BTNode
    {
        private float detectionRange;

        public DetectEnemyNode(float range)
        {
            this.detectionRange = range;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            if (observation.distanceToEnemy <= detectionRange)
            {
                state = NodeState.Success;
                //Debug.Log($"적 감지됨! 거리: {observation.distanceToEnemy:F2}");
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }
    }
}

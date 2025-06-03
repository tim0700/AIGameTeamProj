using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 체력 상태를 확인하는 노드
    /// </summary>
    public class CheckHPNode : BTNode
    {
        private float threshold;
        private bool checkSelf;

        public CheckHPNode(float threshold, bool checkSelf = true)
        {
            this.threshold = threshold;
            this.checkSelf = checkSelf;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            
            if (currentHP <= threshold)
            {
                state = NodeState.Success;
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }
    }
}

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
        private bool inverted;

        public CheckHPNode(float threshold, bool checkSelf = true, bool inverted = false)
        {
            this.threshold = threshold;
            this.checkSelf = checkSelf;
            this.inverted = inverted;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            
            bool condition = currentHP <= threshold;
            
            // inverted 옵션에 따라 조건 반전
            if (inverted)
                condition = !condition;
            
            if (condition)
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

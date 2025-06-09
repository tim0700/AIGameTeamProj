using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT 노드의 실행 상태를 나타내는 enum
    /// </summary>
    public enum NodeState
    {
        Running,  // 실행 중
        Success,  // 성공
        Failure   // 실패
    }

    /// <summary>
    /// Behavior Tree 노드의 기본 추상 클래스
    /// 모든 BT 노드는 이 클래스를 상속받아야 함
    /// </summary>
    public abstract class BTNode
    {
        protected NodeState state;
        protected AgentController agentController;

        public NodeState GetState() => state;

        public virtual void Initialize(AgentController controller)
        {
            agentController = controller;
        }

        public abstract NodeState Evaluate(GameObservation observation);
    }
}

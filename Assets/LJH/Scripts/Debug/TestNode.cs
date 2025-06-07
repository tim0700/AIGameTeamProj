using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// RandomSelectorNode 테스트를 위한 간단한 테스트 노드
    /// 설정된 결과를 반환하며 실행 로그를 출력
    /// </summary>
    public class TestNode : BTNode
    {
        private string nodeName;
        private NodeState returnState;
        private bool shouldLog;

        public TestNode(string name, NodeState returnState, bool shouldLog = true)
        {
            this.nodeName = name;
            this.returnState = returnState;
            this.shouldLog = shouldLog;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            if (shouldLog)
            {
                Debug.Log($"[TestNode] {nodeName} executed → returning {returnState}");
            }
            
            state = returnState;
            return state;
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            if (shouldLog)
            {
                Debug.Log($"[TestNode] {nodeName} initialized");
            }
        }

        /// <summary>
        /// 노드 이름 반환 (디버깅용)
        /// </summary>
        public string GetNodeName()
        {
            return nodeName;
        }
    }
}

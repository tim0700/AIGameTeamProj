using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 자식 노드들을 랜덤한 순서로 시도하는 Selector 노드
    /// 기존 SelectorNode와 동일한 로직이지만 매번 다른 순서로 실행
    /// </summary>
    public class RandomSelectorNode : BTNode
    {
        private List<BTNode> children;
        
        [Header("디버그 설정")]
        public bool enableDebugLog = false;

        public RandomSelectorNode(List<BTNode> children)
        {
            this.children = children ?? new List<BTNode>();
        }

        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            
            if (enableDebugLog)
                Debug.Log($"RandomSelectorNode initialized with {children.Count} children");
            
            foreach (BTNode child in children)
            {
                child.Initialize(controller);
            }
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            // 자식들을 랜덤하게 섞기 (LINQ 사용)
            var shuffledChildren = children.OrderBy(x => Random.Range(0f, 1f)).ToList();
            
            if (enableDebugLog)
            {
                Debug.Log($"RandomSelector executing {shuffledChildren.Count} children in random order");
            }

            // 기존 Selector 로직: 첫 번째 Success/Running 반환
            foreach (BTNode child in shuffledChildren)
            {
                NodeState childResult = child.Evaluate(observation);
                
                if (enableDebugLog)
                {
                    Debug.Log($"Child {child.GetType().Name} returned: {childResult}");
                }
                
                switch (childResult)
                {
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    // Failure인 경우 다음 자식 시도
                }
            }

            // 모든 자식이 Failure를 반환한 경우
            state = NodeState.Failure;
            return state;
        }

        /// <summary>
        /// 자식 노드 개수 반환 (디버깅용)
        /// </summary>
        public int GetChildrenCount()
        {
            return children.Count;
        }

        /// <summary>
        /// 특정 자식 노드 반환 (디버깅용)
        /// </summary>
        public BTNode GetChild(int index)
        {
            if (index >= 0 && index < children.Count)
                return children[index];
            return null;
        }
    }
}

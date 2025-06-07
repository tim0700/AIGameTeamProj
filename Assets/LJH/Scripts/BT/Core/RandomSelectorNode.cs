using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// Random Selector Node (랜덤 OR 논리)
    /// 자식 노드들을 랜덤한 순서로 시도하는 Selector 노드
    /// 기존 SelectorNode와 동일한 로직이지만 매번 다른 순서로 실행하여 예측 불가능성 제공
    /// 
    /// 동작 원리:
    /// - 매번 평가시마다 자식 노드들을 랜덤하게 섞음
    /// - 섞인 순서대로 첫 번째 Success/Running을 만날 때까지 시도
    /// - 모든 자식이 Failure면 Failure 반환
    /// 
    /// 사용 사례:
    /// - AI의 예측 불가능성 증가 (같은 상황에서 다른 행동 선택)
    /// - 우선순위가 동등한 여러 행동 중 랜덤 선택
    /// - 플레이어가 AI 패턴을 학습하는 것 방지
    /// - 다양한 전략 시도를 통한 적응성 향상
    /// 
    /// 설계 고려사항:
    /// - 과도한 랜덤성은 AI가 일관성 없어 보일 수 있음
    /// - 중요한 안전 행동(경계 복귀 등)은 별도 처리 권장
    /// - 디버깅이 어려우므로 로깅 기능 활용 필요
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

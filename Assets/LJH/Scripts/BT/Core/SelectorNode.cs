using System.Collections.Generic;

namespace LJH.BT
{
    /// <summary>
    /// Selector Node (OR 논리)
    /// 자식 노드들을 순서대로 시도하여 첫 번째 성공하는 노드에서 멈춤
    /// 
    /// 동작 원리:
    /// - 자식들을 왼쪽부터 오른쪽으로 순서대로 실행
    /// - 첫 번째 Success 또는 Running을 만나면 즉시 반환
    /// - 모든 자식이 Failure를 반환하면 Failure 반환
    /// 
    /// 사용 사례:
    /// - 여러 행동 옵션 중 하나 선택 (공격 or 방어 or 회피)
    /// - 우선순위 기반 행동 선택
    /// - 대안 행동 체인 (A 실패시 B, B 실패시 C)
    /// 
    /// 설계 패턴:
    /// - 높은 우선순위 행동을 앞쪽에 배치
    /// - 항상 성공하는 fallback 행동을 마지막에 배치
    /// </summary>
    public class SelectorNode : BTNode
    {
        /// <summary>
        /// 자식 노드 목록
        /// 순서대로 평가되며, 우선순위를 나타냄
        /// </summary>
        private List<BTNode> children;

        /// <summary>
        /// Selector 노드 생성자
        /// </summary>
        /// <param name="children">자식 노드 목록 (우선순위 순서)</param>
        public SelectorNode(List<BTNode> children)
        {
            this.children = children ?? new List<BTNode>();
        }

        /// <summary>
        /// 노드와 모든 자식 노드들을 초기화
        /// </summary>
        /// <param name="controller">에이전트 컨트롤러</param>
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            
            // 모든 자식 노드 초기화
            foreach (BTNode child in children)
            {
                child.Initialize(controller);
            }
        }

        /// <summary>
        /// Selector 로직 실행
        /// 자식 노드들을 순서대로 시도하여 첫 성공/실행중인 노드에서 멈춤
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>첫 번째 성공/실행중인 결과, 또는 모두 실패시 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // 자식이 없으면 즉시 실패
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            // 자식 노드들을 순서대로 평가
            foreach (BTNode child in children)
            {
                NodeState childResult = child.Evaluate(observation);
                
                switch (childResult)
                {
                    case NodeState.Success:
                        // 첫 번째 성공한 자식이 있으면 즉시 성공 반환
                        state = NodeState.Success;
                        return state;
                        
                    case NodeState.Running:
                        // 실행 중인 자식이 있으면 계속 실행
                        state = NodeState.Running;
                        return state;
                        
                    case NodeState.Failure:
                        // 실패한 경우 다음 자식 시도
                        continue;
                }
            }

            // 모든 자식이 실패한 경우
            state = NodeState.Failure;
            return state;
        }
        
        /// <summary>
        /// 자식 노드 개수 반환 (디버깅용)
        /// </summary>
        /// <returns>자식 노드 수</returns>
        public int GetChildrenCount() => children.Count;
        
        /// <summary>
        /// 특정 인덱스의 자식 노드 반환 (디버깅용)
        /// </summary>
        /// <param name="index">자식 노드 인덱스</param>
        /// <returns>자식 노드, 또는 null</returns>
        public BTNode GetChild(int index)
        {
            if (index >= 0 && index < children.Count)
                return children[index];
            return null;
        }
        
        /// <summary>
        /// 자식 노드 추가 (런타임 동적 구성용)
        /// </summary>
        /// <param name="child">추가할 자식 노드</param>
        public void AddChild(BTNode child)
        {
            if (child != null)
            {
                children.Add(child);
                // 이미 초기화된 경우 새 자식도 초기화
                if (agentController != null)
                {
                    child.Initialize(agentController);
                }
            }
        }
    }
}

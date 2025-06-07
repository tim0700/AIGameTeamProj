using System.Collections.Generic;

namespace LJH.BT
{
    /// <summary>
    /// Sequence Node (AND 논리)
    /// 모든 자식 노드가 성공해야만 성공을 반환하는 컴포지트 노드
    /// 
    /// 동작 원리:
    /// - 자식들을 왼쪽부터 오른쪽으로 순서대로 실행
    /// - 단 하나라도 Failure를 반환하면 즉시 Failure 반환
    /// - 단 하나라도 Running을 반환하면 Running 반환
    /// - 모든 자식이 Success를 반환해야 Success 반환
    /// 
    /// 사용 사례:
    /// - 순차적 행동 체인 (이동 → 공격 → 후퇴)
    /// - 조건 검사 + 행동 실행 (체력 체크 → 치료)
    /// - 복잡한 행동의 단계별 구현
    /// 
    /// 설계 패턴:
    /// - 조건 검사를 첫 번째에 배치
    /// - 실행 비용이 높은 행동을 뒤쪽에 배치
    /// - 각 단계가 논리적으로 의존적이도록 구성
    /// </summary>
    public class SequenceNode : BTNode
    {
        /// <summary>
        /// 자식 노드 목록
        /// 순서대로 모두 성공해야 하는 필수 단계들
        /// </summary>
        private List<BTNode> children;

        /// <summary>
        /// Sequence 노드 생성자
        /// </summary>
        /// <param name="children">자식 노드 목록 (실행 순서)</param>
        public SequenceNode(List<BTNode> children)
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
        /// Sequence 로직 실행
        /// 모든 자식이 성공해야 Success, 하나라도 실패하면 Failure
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>모든 자식 성공시 Success, 하나라도 실패시 Failure, 실행중이면 Running</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // 자식이 없으면 즉시 성공 (빈 시퀀스는 성공)
            if (children.Count == 0)
            {
                state = NodeState.Success;
                return state;
            }

            bool anyRunning = false;

            // 모든 자식 노드를 순서대로 평가
            foreach (BTNode child in children)
            {
                NodeState childResult = child.Evaluate(observation);
                
                switch (childResult)
                {
                    case NodeState.Failure:
                        // 하나라도 실패하면 전체 실패
                        state = NodeState.Failure;
                        return state;
                        
                    case NodeState.Running:
                        // 실행 중인 자식이 있음을 기록
                        anyRunning = true;
                        break;
                        
                    case NodeState.Success:
                        // 이 자식은 성공, 다음 자식 계속 검사
                        break;
                }
            }

            // 결과 결정:
            // - 실행 중인 자식이 있으면 Running
            // - 모든 자식이 성공하면 Success
            state = anyRunning ? NodeState.Running : NodeState.Success;
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
        
        /// <summary>
        /// 현재 실행 중인 자식 노드의 인덱스 반환 (디버깅용)
        /// </summary>
        /// <returns>실행 중인 자식 인덱스, 또는 -1</returns>
        public int GetCurrentRunningChildIndex()
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].GetState() == NodeState.Running)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

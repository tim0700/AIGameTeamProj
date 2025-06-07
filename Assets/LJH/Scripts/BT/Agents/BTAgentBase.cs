using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT 에이전트의 기본 클래스
    /// </summary>
    public abstract class BTAgentBase : MonoBehaviour, IBattleAgent
    {
        [Header("BT 에이전트 설정")]
        public string agentName = "BT Agent";
        public string agentType = "BT";

        protected BTNode rootNode;
        protected AgentController controller;
        
        // Public getter for controller
        public AgentController Controller => controller;

        public virtual void Initialize(AgentController controller)
        {
            this.controller = controller;
            BuildBehaviorTree();
            
            if (rootNode != null)
            {
                rootNode.Initialize(controller);
            }
        }

        public virtual AgentAction DecideAction(GameObservation observation)
        {
            if (rootNode != null)
            {
                NodeState result = rootNode.Evaluate(observation);
                
                // BT 실행 결과에 따른 기본 처리
                switch (result)
                {
                    case NodeState.Success:
                        // BT가 성공적으로 실행됨
                        break;
                    case NodeState.Running:
                        // BT가 계속 실행 중
                        break;
                    case NodeState.Failure:
                        // BT 실행 실패, 기본 행동으로 대체
                        return AgentAction.Idle;
                }
            }

            // BT에서 실제 행동이 실행되므로 Idle 반환
            return AgentAction.Idle;
        }

        public virtual void OnActionResult(ActionResult result)
        {
            // BT 에이전트는 개별 노드에서 행동을 처리하므로 
            // 여기서는 결과를 로깅하거나 학습에 활용할 수 있음
            if (!result.success)
            {
                Debug.LogWarning($"{agentName} 행동 실패: {result.message}");
            }
        }

        public virtual void OnEpisodeEnd(EpisodeResult result)
        {
            Debug.Log($"{agentName} 에피소드 종료 - 승리: {result.won}, 최종 HP: {result.finalHP}");
        }

        public string GetAgentName() => agentName;
        public string GetAgentType() => agentType;

        /// <summary>
        /// 하위 클래스에서 구현해야 하는 BT 구조 생성 메서드
        /// </summary>
        protected abstract void BuildBehaviorTree();
    }
}

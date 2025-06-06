using UnityEngine;

namespace LJH.BT
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// BT 노드의 기본 베이스 클래스
    /// 모든 BT 노드가 상속받아야 하는 핵심 기능 제공
    /// </summary>
    public abstract class BTNode : IBTNode
    {
        protected NodeState state;
        protected AgentController agentController;
        
        // 기본 속성들
        protected string nodeName;
        protected bool isEnabled = true;
        protected bool isInitialized = false;
        
        // 디버깅용
        protected bool enableLogging = false;
        
        #region 생성자들
        
        public BTNode()
        {
            nodeName = GetType().Name;
        }
        
        public BTNode(string name)
        {
            nodeName = name;
        }
        
        #endregion
        
        #region IBTNode 구현
        
        public NodeState GetState() => state;

        public virtual void Initialize(AgentController controller)
        {
            agentController = controller;
            isInitialized = true;
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 노드가 초기화되었습니다.");
        }

        public abstract NodeState Evaluate(GameObservation observation);
        
        #endregion
        
        #region 유틸리티 메서드들
        
        /// <summary>
        /// 노드 이름 반환
        /// </summary>
        public virtual string GetNodeName() => nodeName;
        
        /// <summary>
        /// 노드 이름 설정
        /// </summary>
        public virtual void SetNodeName(string name)
        {
            nodeName = name;
        }
        
        /// <summary>
        /// 노드 활성화 여부 확인
        /// </summary>
        public virtual bool IsEnabled() => isEnabled;
        
        /// <summary>
        /// 노드 활성화/비활성화
        /// </summary>
        public virtual void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 노드가 {(enabled ? "활성화" : "비활성화")}되었습니다.");
        }
        
        /// <summary>
        /// 노드 초기화 여부 확인
        /// </summary>
        public virtual bool IsInitialized() => isInitialized;
        
        /// <summary>
        /// 로깅 활성화/비활성화
        /// </summary>
        public virtual void SetLogging(bool enable)
        {
            enableLogging = enable;
        }
        
        /// <summary>
        /// 노드 리셋 (새 에피소드 시작시)
        /// </summary>
        public virtual void Reset()
        {
            state = NodeState.Running;
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 노드가 리셋되었습니다.");
        }
        
        /// <summary>
        /// 노드 상태를 문자열로 반환 (디버깅용)
        /// </summary>
        public virtual string GetStatusString()
        {
            return $"[{nodeName}] 상태: {state}, 활성화: {isEnabled}, 초기화: {isInitialized}";
        }
        
        #endregion
        
        #region 헬퍼 메서드들
        
        /// <summary>
        /// 안전한 Evaluate 래퍼 (예외 처리 포함)
        /// </summary>
        protected virtual NodeState SafeEvaluate(GameObservation observation)
        {
            // 기본 조건 확인
            if (!isEnabled)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 비활성화된 노드");
                return NodeState.Failure;
            }
            
            if (!isInitialized)
            {
                Debug.LogWarning($"[{nodeName}] 초기화되지 않은 노드");
                return NodeState.Failure;
            }
            
            if (agentController == null)
            {
                Debug.LogError($"[{nodeName}] AgentController가 null입니다");
                return NodeState.Failure;
            }
            
            try
            {
                // 실제 평가 실행
                NodeState result = Evaluate(observation);
                state = result;
                
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 실행 결과: {result}");
                
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{nodeName}] 실행 중 오류 발생: {ex.Message}");
                state = NodeState.Failure;
                return NodeState.Failure;
            }
        }
        
        /// <summary>
        /// AgentController 유효성 확인
        /// </summary>
        protected bool ValidateController()
        {
            if (agentController == null)
            {
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] AgentController가 null입니다");
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// 기본 실행 조건 확인
        /// </summary>
        protected bool CheckBasicConditions()
        {
            return isEnabled && isInitialized && ValidateController();
        }
        
        #endregion
    }
}

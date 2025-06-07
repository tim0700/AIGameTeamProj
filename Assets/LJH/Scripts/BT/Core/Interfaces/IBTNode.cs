using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 모든 BT 노드가 구현해야 하는 기본 인터페이스
    /// 기존 BTNode 클래스와 호환성을 보장하면서 확장된 기능 제공
    /// </summary>
    public interface IBTNode
    {
        /// <summary>
        /// 노드 평가 및 실행 (기존 BTNode와 동일)
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>노드 실행 결과</returns>
        NodeState Evaluate(GameObservation observation);
        
        /// <summary>
        /// 노드 초기화 (기존 BTNode와 동일)
        /// </summary>
        /// <param name="controller">에이전트 컨트롤러</param>
        void Initialize(AgentController controller);
        
        /// <summary>
        /// 현재 노드 상태 반환 (기존 BTNode와 동일)
        /// </summary>
        /// <returns>현재 노드 상태</returns>
        NodeState GetState();
        
        /// <summary>
        /// 노드 이름 반환 (디버깅 및 로깅용)
        /// </summary>
        /// <returns>노드 이름</returns>
        string GetNodeName();
        
        /// <summary>
        /// 노드 메타데이터 반환 (ML 학습 및 성능 분석용)
        /// </summary>
        /// <returns>노드 메타데이터</returns>
        NodeMetadata GetMetadata();
        
        /// <summary>
        /// 메타데이터 업데이트 (통계 수집용)
        /// </summary>
        /// <param name="result">실행 결과</param>
        /// <param name="executionTime">실행 시간 (ms)</param>
        void UpdateMetadata(NodeState result, float executionTime);
        
        /// <summary>
        /// 노드 리셋 (새 에피소드 시작시)
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 노드가 현재 활성화되어 있는지 확인
        /// </summary>
        /// <returns>활성화 여부</returns>
        bool IsActive();
        
        /// <summary>
        /// 노드 설명 반환 (디버깅용)
        /// </summary>
        /// <returns>노드 설명</returns>
        string GetDescription();
    }
}
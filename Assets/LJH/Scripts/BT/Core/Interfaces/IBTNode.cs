using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 모든 BT 노드가 구현해야 하는 기본 인터페이스
    /// 기존 BTNode 클래스와 완전 호환성 보장
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
    }
}

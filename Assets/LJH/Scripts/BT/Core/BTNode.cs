using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT 노드의 실행 상태를 나타내는 열거형
    /// Behavior Tree의 기본 상태 체계
    /// </summary>
    public enum NodeState
    {
        /// <summary>
        /// 실행 중 - 노드가 아직 작업을 완료하지 못함
        /// 다음 프레임에서 계속 실행됨
        /// </summary>
        Running,
        
        /// <summary>
        /// 성공 - 노드가 목표를 달성함
        /// 부모 노드에게 성공을 알림
        /// </summary>
        Success,
        
        /// <summary>
        /// 실패 - 노드가 목표를 달성하지 못함
        /// 부모 노드에게 실패를 알림
        /// </summary>
        Failure
    }

    /// <summary>
    /// Behavior Tree의 기본 노드 클래스
    /// 모든 BT 노드는 이 클래스를 상속받아 구현됨
    /// 
    /// 주요 역할:
    /// - 노드의 기본 생명주기 관리 (초기화, 평가)
    /// - AgentController와의 연결
    /// - 노드 상태 추적
    /// 
    /// 사용 패턴:
    /// 1. Initialize() - 노드 초기화
    /// 2. Evaluate() - 매 프레임 실행
    /// 3. GetState() - 현재 상태 확인
    /// </summary>
    public abstract class BTNode
    {
        /// <summary>
        /// 현재 노드의 실행 상태
        /// Running, Success, Failure 중 하나
        /// </summary>
        protected NodeState state;
        
        /// <summary>
        /// 실제 게임 행동을 실행하는 컨트롤러
        /// 노드가 게임 세계에 영향을 주기 위해 사용
        /// </summary>
        protected AgentController agentController;

        /// <summary>
        /// 현재 노드의 상태를 반환
        /// </summary>
        /// <returns>현재 NodeState (Running/Success/Failure)</returns>
        public NodeState GetState() => state;

        /// <summary>
        /// 노드를 초기화하고 AgentController를 연결
        /// BT 구성 시 한 번만 호출됨
        /// </summary>
        /// <param name="controller">게임 행동을 실행할 컨트롤러</param>
        public virtual void Initialize(AgentController controller)
        {
            agentController = controller;
        }

        /// <summary>
        /// 노드의 핵심 로직을 실행하는 추상 메서드
        /// 하위 클래스에서 반드시 구현해야 함
        /// 
        /// 구현 가이드라인:
        /// - 빠른 실행: 한 프레임 내에서 완료되어야 함
        /// - 상태 업데이트: state 필드를 적절히 설정
        /// - 일관성: 동일한 입력에 대해 일관된 결과 반환
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>노드 실행 결과 (Running/Success/Failure)</returns>
        public abstract NodeState Evaluate(GameObservation observation);
    }
}

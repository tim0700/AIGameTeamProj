using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 액션 실행 특화 노드 인터페이스
    /// 공격, 방어, 회피 등 실제 게임 상태를 변경하는 행동들에 사용
    /// </summary>
    public interface IActionNode : IBTNode
    {
        /// <summary>
        /// 액션 실행 가능 여부 검사
        /// 쿨다운, 거리, 상태 등을 종합적으로 판단
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>실행 가능 여부</returns>
        bool CanExecute(GameObservation observation);
        
        /// <summary>
        /// 액션 실행 (실제 게임 상태 변경)
        /// AgentController를 통해 실제 행동 수행
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>실행 결과</returns>
        ActionResult ExecuteAction(GameObservation observation);
        
        /// <summary>
        /// 액션 타입 반환
        /// Attack, Defend, Dodge 등
        /// </summary>
        /// <returns>액션 타입</returns>
        ActionType GetActionType();
        
        /// <summary>
        /// 액션 실행 비용 반환 (ML 의사결정용)
        /// 체력 소모, 쿨다운 등을 고려한 상대적 비용
        /// </summary>
        /// <returns>실행 비용 (0.0 ~ 1.0)</returns>
        float GetExecutionCost();
        
        /// <summary>
        /// 액션 효과 반환 (ML 보상 계산용)
        /// 예상 데미지, 방어 효과 등
        /// </summary>
        /// <returns>예상 효과값</returns>
        float GetExpectedEffect();
        
        /// <summary>
        /// 액션 지속 시간 반환
        /// 애니메이션 재생 시간, 쿨다운 등
        /// </summary>
        /// <returns>지속 시간 (초)</returns>
        float GetDuration();
        
        /// <summary>
        /// 액션 중단 가능 여부
        /// 실행 중인 액션을 중단할 수 있는지 확인
        /// </summary>
        /// <returns>중단 가능 여부</returns>
        bool CanInterrupt();
        
        /// <summary>
        /// 액션 중단 실행
        /// 현재 실행 중인 액션을 강제로 중단
        /// </summary>
        /// <returns>중단 성공 여부</returns>
        bool InterruptAction();
        
        /// <summary>
        /// 액션 연계 가능성 확인
        /// 다른 액션과의 콤보 가능성
        /// </summary>
        /// <param name="nextActionType">다음 액션 타입</param>
        /// <returns>연계 가능 여부</returns>
        bool CanChainWith(ActionType nextActionType);
        
        /// <summary>
        /// 액션 실행 조건 반환 (디버깅용)
        /// 현재 실행할 수 없는 이유 설명
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>실행 조건 설명</returns>
        string GetExecutionConditions(GameObservation observation);
        
        /// <summary>
        /// 액션 파라미터 조정 (ML 최적화용)
        /// 데미지 배율, 범위 등 조정 가능한 파라미터
        /// </summary>
        /// <param name="parameters">새로운 파라미터 값들</param>
        void SetActionParameters(float[] parameters);
        
        /// <summary>
        /// 현재 액션 파라미터 반환
        /// </summary>
        /// <returns>현재 파라미터 값들</returns>
        float[] GetActionParameters();
    }
}
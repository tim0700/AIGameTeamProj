using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 조건 검사 특화 노드 인터페이스
    /// HP 체크, 쿨다운 체크, 적 탐지, 경계 체크 등에 사용
    /// </summary>
    public interface IConditionNode : IBTNode
    {
        /// <summary>
        /// 순수 조건 검사 (부작용 없는 검사)
        /// Evaluate()와 달리 상태 변경 없이 조건만 확인
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>조건 만족 여부</returns>
        bool CheckCondition(GameObservation observation);
        
        /// <summary>
        /// 조건 설명 반환 (디버깅 및 UI 표시용)
        /// 예: "HP < 30%", "Attack Cooldown Ready", "Enemy Detected"
        /// </summary>
        /// <returns>조건 설명</returns>
        string GetConditionDescription();
        
        /// <summary>
        /// 조건 임계값 반환 (ML 최적화용)
        /// HP 임계값, 거리 임계값 등 조정 가능한 값들
        /// </summary>
        /// <returns>조건 임계값들</returns>
        float[] GetConditionThresholds();
        
        /// <summary>
        /// 조건 임계값 설정 (ML 최적화용)
        /// 학습 과정에서 동적으로 임계값 조정
        /// </summary>
        /// <param name="thresholds">새로운 임계값들</param>
        void SetConditionThresholds(float[] thresholds);
        
        /// <summary>
        /// 조건 우선순위 반환 (복합 조건 평가시 사용)
        /// 높은 우선순위 조건을 먼저 검사하여 성능 최적화
        /// </summary>
        /// <returns>우선순위 (높을수록 먼저 평가)</returns>
        int GetPriority();
        
        /// <summary>
        /// 조건 검사 비용 반환 (성능 최적화용)
        /// 복잡한 계산이 필요한 조건은 높은 비용
        /// </summary>
        /// <returns>상대적 검사 비용</returns>
        float GetCheckCost();
        
        /// <summary>
        /// 빠른 조건 검사 (간단한 사전 검사)
        /// 전체 CheckCondition() 호출 전 빠른 필터링용
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>사전 조건 만족 여부</returns>
        bool QuickCheck(GameObservation observation);
        
        /// <summary>
        /// 조건 검사 결과 캐싱 여부
        /// 동일한 프레임 내에서 중복 검사 방지
        /// </summary>
        /// <returns>캐싱 가능 여부</returns>
        bool IsCacheable();
    }
}
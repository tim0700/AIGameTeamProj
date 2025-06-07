using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// HP(체력) 상태를 확인하는 조건 노드
    /// 자신 또는 적의 체력이 특정 임계값을 충족하는지 검사
    /// 
    /// 사용 사례:
    /// - 저체력 상태에서 방어/회피 행동 트리거
    /// - 적 체력 낮을 때 공격적 행동 선택
    /// - 체력 기반 전략 변경 조건
    /// - 응급 상황 감지 및 대응
    /// 
    /// 특징:
    /// - 빠른 실행 (연산 비용 낮음)
    /// - 유연한 조건 설정 (inverted 옵션)
    /// - 자신/적 둘 다 지원
    /// - 런타임 임계값 조정 가능
    /// </summary>
    public class CheckHPNode : BTNode
    {
        /// <summary>
        /// HP 비교 임계값
        /// 이 값과 현재 HP를 비교하여 조건 성립 여부 결정
        /// </summary>
        private float threshold;
        
        /// <summary>
        /// 검사 대상 선택
        /// true: 자신의 HP 검사, false: 적의 HP 검사
        /// </summary>
        private bool checkSelf;
        
        /// <summary>
        /// 조건 반전 옵션
        /// true: 비교 결과를 반전시킴 (>로 비교 효과)
        /// false: 일반 비교 (<=로 비교)
        /// </summary>
        private bool inverted;

        /// <summary>
        /// HP 체크 노드 생성자
        /// </summary>
        /// <param name="threshold">HP 비교 임계값</param>
        /// <param name="checkSelf">자신 HP 검사 여부 (기본: true)</param>
        /// <param name="inverted">조건 반전 여부 (기본: false)</param>
        public CheckHPNode(float threshold, bool checkSelf = true, bool inverted = false)
        {
            this.threshold = threshold;
            this.checkSelf = checkSelf;
            this.inverted = inverted;
        }

        /// <summary>
        /// HP 조건 검사 실행
        /// 설정된 조건에 따라 체력 상태를 평가
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>조건 충족시 Success, 미충족시 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // 검사 대상 선택: 자신 또는 적의 HP
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            
            // 기본 비교: 현재 HP가 임계값 이하인지 확인
            bool condition = currentHP <= threshold;
            
            // inverted 옵션 적용: 조건을 반전 (> 비교 효과)
            if (inverted)
                condition = !condition;
            
            // 결과 결정 및 로깅
            if (condition)
            {
                state = NodeState.Success;
                
                // 상세 로깅 (디버깅 모드에서 유용)
                string target = checkSelf ? "자신" : "적";
                string comparison = inverted ? ">" : "<=";
                Debug.Log($"[HP체크] {target} HP {currentHP:F1} {comparison} {threshold:F1} 조건 충족");
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }
        
        /// <summary>
        /// 임계값 설정 (런타임 조정용)
        /// </summary>
        /// <param name="newThreshold">새로운 HP 임계값</param>
        public void SetThreshold(float newThreshold)
        {
            threshold = Mathf.Max(0f, newThreshold); // 음수 방지
        }
        
        /// <summary>
        /// 현재 임계값 반환
        /// </summary>
        /// <returns>설정된 HP 임계값</returns>
        public float GetThreshold()
        {
            return threshold;
        }
        
        /// <summary>
        /// 검사 대상 설정 (런타임 조정용)
        /// </summary>
        /// <param name="self">자신 HP 검사 여부</param>
        public void SetCheckSelf(bool self)
        {
            checkSelf = self;
        }
        
        /// <summary>
        /// 조건 반전 설정 (런타임 조정용)
        /// </summary>
        /// <param name="invert">반전 여부</param>
        public void SetInverted(bool invert)
        {
            inverted = invert;
        }
        
        /// <summary>
        /// 조건 평가 미리보기 (실제 실행 없이 결과만 확인)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>예상 조건 충족 여부</returns>
        public bool PreviewCondition(GameObservation observation)
        {
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            bool condition = currentHP <= threshold;
            return inverted ? !condition : condition;
        }
        
        /// <summary>
        /// 노드 설정 정보 반환 (디버깅용)
        /// </summary>
        /// <returns>노드 설정 설명</returns>
        public string GetDescription()
        {
            string target = checkSelf ? "자신" : "적";
            string comparison = inverted ? ">" : "<=";
            return $"{target} HP {comparison} {threshold:F1}";
        }
    }
}

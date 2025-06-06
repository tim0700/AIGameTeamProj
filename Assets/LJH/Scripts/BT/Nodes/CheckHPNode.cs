using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 체력 상태를 확인하는 노드
    /// </summary>
    public class CheckHPNode : BTNode
    {
        private float threshold;
        private bool checkSelf;
        private bool inverted;

        public CheckHPNode(float threshold, bool checkSelf = true, bool inverted = false) 
            : base($"CheckHP Node ({(checkSelf ? "Self" : "Enemy")} {(inverted ? ">" : "<=")} {threshold})")
        {
            this.threshold = threshold;
            this.checkSelf = checkSelf;
            this.inverted = inverted;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            
            bool condition = currentHP <= threshold;
            
            // inverted 옵션에 따라 조건 반전
            if (inverted)
                condition = !condition;
            
            if (condition)
            {
                state = NodeState.Success;
                if (enableLogging)
                {
                    string target = checkSelf ? "자신" : "적";
                    string comparison = inverted ? ">" : "<=";
                    Debug.Log($"[{nodeName}] 조건 만족: {target} HP({currentHP:F1}) {comparison} {threshold:F1}");
                }
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                {
                    string target = checkSelf ? "자신" : "적";
                    string comparison = inverted ? ">" : "<=";
                    Debug.Log($"[{nodeName}] 조건 불만족: {target} HP({currentHP:F1}) {comparison} {threshold:F1}");
                }
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// HP 임계값 설정
        /// </summary>
        public void SetThreshold(float newThreshold)
        {
            threshold = Mathf.Max(0f, newThreshold);
            
            // 노드명도 업데이트
            string target = checkSelf ? "Self" : "Enemy";
            string comparison = inverted ? ">" : "<=";
            SetNodeName($"CheckHP Node ({target} {comparison} {threshold})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 임계값 변경: {threshold:F1}");
        }
        
        /// <summary>
        /// 현재 HP 임계값 반환
        /// </summary>
        public float GetThreshold() => threshold;
        
        /// <summary>
        /// 검사 대상 변경 (자신/적)
        /// </summary>
        public void SetCheckTarget(bool checkSelf)
        {
            this.checkSelf = checkSelf;
            
            // 노드명도 업데이트
            string target = checkSelf ? "Self" : "Enemy";
            string comparison = inverted ? ">" : "<=";
            SetNodeName($"CheckHP Node ({target} {comparison} {threshold})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 검사 대상 변경: {(checkSelf ? "자신" : "적")}");
        }
        
        /// <summary>
        /// 조건 반전 여부 설정
        /// </summary>
        public void SetInverted(bool inverted)
        {
            this.inverted = inverted;
            
            // 노드명도 업데이트
            string target = checkSelf ? "Self" : "Enemy";
            string comparison = inverted ? ">" : "<=";
            SetNodeName($"CheckHP Node ({target} {comparison} {threshold})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 조건 반전: {inverted}");
        }
        
        /// <summary>
        /// HP 상태 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CheckCondition(GameObservation observation)
        {
            if (!CheckBasicConditions()) return false;
            
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            bool condition = currentHP <= threshold;
            
            return inverted ? !condition : condition;
        }
        
        /// <summary>
        /// 현재 HP 값 반환
        /// </summary>
        public float GetCurrentHP(GameObservation observation)
        {
            return checkSelf ? observation.selfHP : observation.enemyHP;
        }
        
        /// <summary>
        /// HP 비율 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetHPRatio(GameObservation observation, float maxHP = 100f)
        {
            float currentHP = GetCurrentHP(observation);
            return Mathf.Clamp01(currentHP / maxHP);
        }
        
        /// <summary>
        /// 위험 상태 여부 확인 (임계값의 50% 이하)
        /// </summary>
        public bool IsInDanger(GameObservation observation)
        {
            float currentHP = GetCurrentHP(observation);
            return currentHP <= (threshold * 0.5f);
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            string target = checkSelf ? "Self" : "Enemy";
            string comparison = inverted ? ">" : "<=";
            return base.GetStatusString() + $", 조건: {target} HP {comparison} {threshold:F1}";
        }
        
        #endregion
    }
}

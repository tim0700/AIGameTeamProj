using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 개선된 HP 체크 노드 (ConditionNodeBase 기반)
    /// 기존 CheckHPNode와 호완성을 유지하면서 고급 기능 추가
    /// </summary>
    public class CheckHPNodeV2 : ConditionNodeBase
    {
        [Header("HP 체크 설정")]
        [SerializeField] private bool checkSelf = true;
        [SerializeField] private bool usePercentage = true;
        [SerializeField] private bool enableEmergencyMode = true;
        
        // 응급 모드 임계값
        [SerializeField] private float emergencyThreshold = 10f;
        private bool isInEmergencyMode = false;
        
        // 기존 생성자와 호환성 유지
        public CheckHPNodeV2(float threshold, bool checkSelf = true) : base("CheckHPNode", "HP 상태를 확인하는 노드")
        {
            this.checkSelf = checkSelf;
            
            // 기존 파라미터를 새로운 시스템으로 변환
            if (conditionParameters == null)
            {
                conditionParameters = new ConditionParameters();
            }
            conditionParameters.primaryThreshold = threshold;
            conditionParameters.checkSelf = checkSelf;
            conditionParameters.checkEnemy = !checkSelf;
            conditionParameters.comparisonType = ComparisonType.LessEqual;
        }
        
        public CheckHPNodeV2() : this(30f, true) { }
        
        #region ConditionNodeBase 구현
        
        protected override float GetConditionValue(GameObservation observation)
        {
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            
            // 백분율 모드인 경우 0-100% 범위로 변환
            if (usePercentage)
            {
                float maxHP = 100f; // 기본 최대 HP (실제로는 설정에서 가져와야 함)
                return (currentHP / maxHP) * 100f;
            }
            
            return currentHP;
        }
        
        protected override string GetValueDescription()
        {
            string target = checkSelf ? "자신 HP" : "적 HP";
            string unit = usePercentage ? "%" : "포인트";
            return $"{target} ({unit})";
        }
        
        protected override bool PerformQuickCheck(GameObservation observation)
        {
            // 빠른 사전 검사: 대략적인 HP 확인
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            float roughThreshold = conditionParameters.primaryThreshold;
            
            // 매우 높거나 낮은 경우 빠른 판단
            if (currentHP < roughThreshold * 0.5f) return true;  // 확실히 낮음
            if (currentHP > roughThreshold * 2f) return false;   // 확실히 높음
            
            return true; // 애매한 경우 정확한 검사 필요
        }
        
        protected override void OnConditionParametersChanged()
        {
            base.OnConditionParametersChanged();
            
            // 응급 모드 임계값 조정
            if (enableEmergencyMode)
            {
                emergencyThreshold = conditionParameters.primaryThreshold * 0.3f;
            }
        }
        
        #endregion
        
        #region HP 체크 특화 기능들
        
        /// <summary>
        /// 응급 상황 확인
        /// </summary>
        public bool IsInEmergencyMode(GameObservation observation)
        {
            if (!enableEmergencyMode) return false;
            
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            bool wasInEmergency = isInEmergencyMode;
            
            if (usePercentage)
            {
                float hpPercentage = (currentHP / 100f) * 100f;
                isInEmergencyMode = hpPercentage <= emergencyThreshold;
            }
            else
            {
                isInEmergencyMode = currentHP <= emergencyThreshold;
            }
            
            // 응급 모드 진입/탈출 로깅
            if (isInEmergencyMode != wasInEmergency && enableDetailedLogging)
            {
                string target = checkSelf ? "아군" : "적";
                string status = isInEmergencyMode ? "진입" : "탈출";
                Debug.Log($"[{nodeName}] {target} 응급 모드 {status}! 현재 HP: {currentHP:F1}");
            }
            
            return isInEmergencyMode;
        }
        
        /// <summary>
        /// HP 변화 트렌드 분석
        /// </summary>
        public HPTrend AnalyzeHPTrend(GameObservation observation)
        {
            // 간단한 트렌드 분석 (실제로는 히스토리 필요)
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            float threshold = conditionParameters.primaryThreshold;
            
            if (currentHP < threshold * 0.5f)
                return HPTrend.Critical;
            else if (currentHP < threshold)
                return HPTrend.Declining;
            else if (currentHP > threshold * 1.5f)
                return HPTrend.Healthy;
            else
                return HPTrend.Stable;
        }
        
        /// <summary>
        /// HP 기반 권장 행동 반환
        /// </summary>
        public RecommendedAction GetRecommendedAction(GameObservation observation)
        {
            HPTrend trend = AnalyzeHPTrend(observation);
            bool isEmergency = IsInEmergencyMode(observation);
            
            if (isEmergency)
            {
                return checkSelf ? RecommendedAction.Retreat : RecommendedAction.PressAttack;
            }
            
            switch (trend)
            {
                case HPTrend.Critical:
                    return checkSelf ? RecommendedAction.Heal : RecommendedAction.Finish;
                case HPTrend.Declining:
                    return checkSelf ? RecommendedAction.Defend : RecommendedAction.Maintain;
                case HPTrend.Stable:
                    return RecommendedAction.Continue;
                case HPTrend.Healthy:
                    return checkSelf ? RecommendedAction.Aggressive : RecommendedAction.Careful;
                default:
                    return RecommendedAction.Continue;
            }
        }
        
        /// <summary>
        /// 상세 HP 상태 정보 반환
        /// </summary>
        public HPStatusInfo GetDetailedStatus(GameObservation observation)
        {
            float currentHP = checkSelf ? observation.selfHP : observation.enemyHP;
            float threshold = conditionParameters.primaryThreshold;
            
            return new HPStatusInfo
            {
                currentHP = currentHP,
                threshold = threshold,
                percentage = usePercentage ? (currentHP / 100f) * 100f : currentHP,
                isEmergency = IsInEmergencyMode(observation),
                trend = AnalyzeHPTrend(observation),
                recommendedAction = GetRecommendedAction(observation),
                isConditionMet = CheckCondition(observation)
            };
        }
        
        #endregion
        
        #region 기존 API 호환성
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public void SetThreshold(float threshold)
        {
            conditionParameters.primaryThreshold = threshold;
        }
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public float GetThreshold()
        {
            return conditionParameters.primaryThreshold;
        }
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public void SetCheckSelf(bool checkSelf)
        {
            this.checkSelf = checkSelf;
            conditionParameters.checkSelf = checkSelf;
            conditionParameters.checkEnemy = !checkSelf;
        }
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public bool GetCheckSelf()
        {
            return checkSelf;
        }
        
        #endregion
    }
    
    /// <summary>
    /// HP 변화 트렌드
    /// </summary>
    public enum HPTrend
    {
        Critical,   // 매우 위험
        Declining,  // 감소 중
        Stable,     // 안정
        Healthy     // 건강
    }
    
    /// <summary>
    /// 권장 행동
    /// </summary>
    public enum RecommendedAction
    {
        Retreat,        // 후퇴
        Heal,          // 회복
        Defend,        // 방어
        Continue,      // 계속
        Maintain,      // 유지
        Aggressive,    // 공격적
        Careful,       // 신중
        PressAttack,   // 추격
        Finish         // 마무리
    }
    
    /// <summary>
    /// HP 상태 정보
    /// </summary>
    [System.Serializable]
    public struct HPStatusInfo
    {
        public float currentHP;
        public float threshold;
        public float percentage;
        public bool isEmergency;
        public HPTrend trend;
        public RecommendedAction recommendedAction;
        public bool isConditionMet;
        
        public override string ToString()
        {
            return $"HP: {currentHP:F1} ({percentage:F1}%), 응급: {isEmergency}, 트렌드: {trend}, 권장: {recommendedAction}";
        }
    }
}

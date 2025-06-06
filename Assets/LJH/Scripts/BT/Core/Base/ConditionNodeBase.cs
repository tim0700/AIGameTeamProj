using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 조건 노드 특화 베이스 클래스
    /// HP 체크, 쿨다운 체크, 적 탐지, 경계 체크 등 조건 검사를 수행하는 노드들의 기반
    /// </summary>
    public abstract class ConditionNodeBase : BTNodeBase, IConditionNode
    {
        [Header("조건 노드 설정")]
        [SerializeField] protected ConditionParameters conditionParameters;
        [SerializeField] protected bool useQuickCheck = true;
        [SerializeField] protected bool enableAdaptiveThreshold = false;
        
        // 조건 검사 캐싱
        protected bool? lastConditionResult = null;
        protected float lastConditionCheckTime = -1f;
        protected GameObservation lastConditionObservation;
        
        // 적응형 임계값 시스템
        protected float adaptiveThreshold = 0f;
        protected int adaptationSampleCount = 0;
        protected float adaptationSum = 0f;
        
        // 성능 최적화
        protected int consecutiveFailures = 0;
        protected float lastQuickCheckTime = -1f;
        
        public ConditionNodeBase() : base()
        {
            InitializeConditionParameters();
        }
        
        public ConditionNodeBase(string name, string description = "") : base(name, description)
        {
            InitializeConditionParameters();
        }
        
        #region BTNodeBase 오버라이드
        
        protected override NodeState EvaluateNode(GameObservation observation)
        {
            // 1. 빠른 사전 검사 (선택적)
            if (useQuickCheck && !QuickCheck(observation))
            {
                consecutiveFailures++;
                return NodeState.Failure;
            }
            
            // 2. 조건 검사 캐시 확인
            if (IsCacheable() && IsConditionCacheValid(observation))
            {
                return lastConditionResult.Value ? NodeState.Success : NodeState.Failure;
            }
            
            // 3. 실제 조건 검사
            bool conditionMet = CheckCondition(observation);
            
            // 4. 적응형 임계값 업데이트
            if (enableAdaptiveThreshold)
            {
                UpdateAdaptiveThreshold(observation, conditionMet);
            }
            
            // 5. 결과 캐싱
            if (IsCacheable())
            {
                UpdateConditionCache(observation, conditionMet);
            }
            
            // 6. 연속 실패 카운터 리셋
            if (conditionMet)
            {
                consecutiveFailures = 0;
            }
            else
            {
                consecutiveFailures++;
            }
            
            return conditionMet ? NodeState.Success : NodeState.Failure;
        }
        
        protected override string GetNodeType() => "Condition";
        
        protected override void InitializeParameters()
        {
            if (conditionParameters == null)
            {
                conditionParameters = CreateDefaultParameters();
            }
            parameters = conditionParameters;
        }
        
        protected override void OnParametersChanged()
        {
            if (parameters is ConditionParameters newParams)
            {
                conditionParameters = newParams;
                OnConditionParametersChanged();
            }
        }
        
        protected override void OnReset()
        {
            lastConditionResult = null;
            lastConditionCheckTime = -1f;
            consecutiveFailures = 0;
            lastQuickCheckTime = -1f;
            
            if (enableAdaptiveThreshold)
            {
                ResetAdaptiveThreshold();
            }
        }
        
        protected override bool IsObservationSimilar(GameObservation current, GameObservation cached)
        {
            // 조건 노드는 더 엄격한 유사성 검사 필요
            return GetObservationHash(current) == GetObservationHash(cached);
        }
        
        #endregion
        
        #region IConditionNode 구현
        
        public virtual bool CheckCondition(GameObservation observation)
        {
            // 커스텀 조건 검사 로직 호출
            float value = GetConditionValue(observation);
            float threshold = GetEffectiveThreshold();
            
            bool result = EvaluateCondition(value, threshold);
            
            // 결과 반전 옵션 적용
            if (conditionParameters.invertResult)
            {
                result = !result;
            }
            
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 조건 검사 - 값: {value:F2}, 임계값: {threshold:F2}, 결과: {result}");
            }
            
            return result;
        }
        
        public virtual string GetConditionDescription()
        {
            float threshold = GetEffectiveThreshold();
            string comparison = GetComparisonSymbol();
            string valueDescription = GetValueDescription();
            
            string description = $"{valueDescription} {comparison} {threshold:F1}";
            
            if (conditionParameters.invertResult)
            {
                description = $"NOT ({description})";
            }
            
            return description;
        }
        
        public virtual float[] GetConditionThresholds()
        {
            return conditionParameters?.ToArray() ?? new float[0];
        }
        
        public virtual void SetConditionThresholds(float[] thresholds)
        {
            if (conditionParameters != null)
            {
                conditionParameters.FromArray(thresholds);
                OnConditionParametersChanged();
            }
        }
        
        public virtual int GetPriority() => conditionParameters?.priority ?? 5;
        
        public virtual float GetCheckCost() => conditionParameters?.checkCost ?? 0.1f;
        
        public virtual bool QuickCheck(GameObservation observation)
        {
            // 빠른 사전 검사 (간단한 조건만 확인)
            if (!IsActive()) return false;
            
            // 연속 실패가 너무 많으면 건너뛰기
            if (consecutiveFailures > 5) return false;
            
            // 커스텀 빠른 검사
            return PerformQuickCheck(observation);
        }
        
        public virtual bool IsCacheable() => conditionParameters?.enableCaching ?? true;
        
        #endregion
        
        #region 조건 검사 캐싱
        
        /// <summary>
        /// 조건 검사 캐시가 유효한지 확인
        /// </summary>
        protected virtual bool IsConditionCacheValid(GameObservation observation)
        {
            if (!lastConditionResult.HasValue) return false;
            
            float currentTime = Time.time;
            bool timeValid = (currentTime - lastConditionCheckTime) < cacheValidDuration;
            bool observationSimilar = IsObservationSimilar(observation, lastConditionObservation);
            
            return timeValid && observationSimilar;
        }
        
        /// <summary>
        /// 조건 검사 결과 캐시 업데이트
        /// </summary>
        protected virtual void UpdateConditionCache(GameObservation observation, bool result)
        {
            lastConditionResult = result;
            lastConditionCheckTime = Time.time;
            lastConditionObservation = observation;
        }
        
        #endregion
        
        #region 적응형 임계값 시스템
        
        /// <summary>
        /// 적응형 임계값 업데이트
        /// </summary>
        protected virtual void UpdateAdaptiveThreshold(GameObservation observation, bool conditionMet)
        {
            if (!conditionParameters.adaptiveThreshold) return;
            
            float currentValue = GetConditionValue(observation);
            adaptationSum += currentValue;
            adaptationSampleCount++;
            
            // 일정 샘플 수집 후 임계값 조정
            if (adaptationSampleCount >= 10)
            {
                float averageValue = adaptationSum / adaptationSampleCount;
                float targetThreshold = conditionParameters.primaryThreshold;
                
                // 학습률을 적용하여 점진적 조정
                adaptiveThreshold = Mathf.Lerp(adaptiveThreshold, averageValue, conditionParameters.learningRate);
                
                // 샘플 데이터 리셋
                adaptationSum = 0f;
                adaptationSampleCount = 0;
                
                if (enableDetailedLogging)
                {
                    Debug.Log($"[{nodeName}] 적응형 임계값 업데이트: {adaptiveThreshold:F2}");
                }
            }
        }
        
        /// <summary>
        /// 적응형 임계값 리셋
        /// </summary>
        protected virtual void ResetAdaptiveThreshold()
        {
            adaptiveThreshold = conditionParameters.primaryThreshold;
            adaptationSum = 0f;
            adaptationSampleCount = 0;
        }
        
        /// <summary>
        /// 효과적인 임계값 반환 (적응형 또는 고정)
        /// </summary>
        protected virtual float GetEffectiveThreshold()
        {
            if (enableAdaptiveThreshold && conditionParameters.adaptiveThreshold)
            {
                return adaptiveThreshold;
            }
            return conditionParameters.primaryThreshold;
        }
        
        #endregion
        
        #region 유틸리티 메서드들
        
        /// <summary>
        /// 관찰 데이터의 해시값 계산 (캐싱용)
        /// </summary>
        protected virtual int GetObservationHash(GameObservation observation)
        {
            // 조건 검사에 중요한 값들만 해시 계산
            return $"{observation.selfHP:F1}_{observation.enemyHP:F1}_{observation.distanceToEnemy:F1}_{observation.currentState}".GetHashCode();
        }
        
        /// <summary>
        /// 비교 연산자 수행
        /// </summary>
        protected virtual bool EvaluateCondition(float value, float threshold)
        {
            float tolerance = conditionParameters.toleranceRange;
            
            if (conditionParameters.useAbsoluteValue)
            {
                value = Mathf.Abs(value);
            }
            
            switch (conditionParameters.comparisonType)
            {
                case ComparisonType.Less:
                    return value < (threshold - tolerance);
                case ComparisonType.LessEqual:
                    return value <= (threshold + tolerance);
                case ComparisonType.Equal:
                    return Mathf.Abs(value - threshold) <= tolerance;
                case ComparisonType.GreaterEqual:
                    return value >= (threshold - tolerance);
                case ComparisonType.Greater:
                    return value > (threshold + tolerance);
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 비교 연산자 기호 반환
        /// </summary>
        protected virtual string GetComparisonSymbol()
        {
            switch (conditionParameters.comparisonType)
            {
                case ComparisonType.Less: return "<";
                case ComparisonType.LessEqual: return "<=";
                case ComparisonType.Equal: return "==";
                case ComparisonType.GreaterEqual: return ">=";
                case ComparisonType.Greater: return ">";
                default: return "?";
            }
        }
        
        #endregion
        
        #region 가상 메서드들 (하위 클래스에서 구현)
        
        /// <summary>
        /// 기본 조건 파라미터 생성
        /// </summary>
        protected virtual ConditionParameters CreateDefaultParameters()
        {
            var defaultParams = new ConditionParameters();
            defaultParams.parameterName = $"{nodeName}_Parameters";
            defaultParams.description = $"{nodeName} 조건 파라미터";
            return defaultParams;
        }
        
        /// <summary>
        /// 조건 검사 대상 값 반환 (하위 클래스에서 구현)
        /// </summary>
        protected abstract float GetConditionValue(GameObservation observation);
        
        /// <summary>
        /// 값에 대한 설명 반환 (디버깅용)
        /// </summary>
        protected abstract string GetValueDescription();
        
        /// <summary>
        /// 빠른 사전 검사 수행 (하위 클래스에서 오버라이드 가능)
        /// </summary>
        protected virtual bool PerformQuickCheck(GameObservation observation) => true;
        
        /// <summary>
        /// 조건 파라미터 변경 시 호출
        /// </summary>
        protected virtual void OnConditionParametersChanged()
        {
            // 적응형 임계값 리셋
            if (enableAdaptiveThreshold)
            {
                ResetAdaptiveThreshold();
            }
        }
        
        /// <summary>
        /// 조건 파라미터 초기화
        /// </summary>
        protected virtual void InitializeConditionParameters()
        {
            if (conditionParameters == null)
            {
                conditionParameters = CreateDefaultParameters();
            }
        }
        
        #endregion
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 조건 노드용 파라미터 클래스
    /// HP 체크, 쿨다운 체크, 적 탐지, 경계 체크 등 조건 검사 노드들이 사용
    /// </summary>
    [System.Serializable]
    public class ConditionParameters : INodeParameters
    {
        [Header("조건 기본 설정")]
        public string parameterName = "ConditionParameters";
        public string description = "조건 노드 파라미터";
        
        [Header("임계값 설정")]
        [Range(0f, 100f)]
        public float primaryThreshold = 30f;        // 주 임계값 (HP, 거리 등)
        
        [Range(0f, 100f)]
        public float secondaryThreshold = 0f;       // 보조 임계값
        
        [Range(0f, 10f)]
        public float toleranceRange = 0f;           // 허용 오차 범위
        
        [Header("검사 대상")]
        public bool checkSelf = true;               // 자신 상태 검사
        public bool checkEnemy = false;             // 적 상태 검사
        public bool checkEnvironment = false;       // 환경 상태 검사
        
        [Header("검사 방식")]
        public ComparisonType comparisonType = ComparisonType.LessEqual;  // 비교 방식
        public bool useAbsoluteValue = false;       // 절대값 사용 여부
        public bool invertResult = false;           // 결과 반전 여부
        
        [Header("성능 최적화")]
        [Range(1, 10)]
        public int priority = 5;                    // 검사 우선순위
        
        [Range(0f, 1f)]
        public float checkCost = 0.1f;              // 상대적 검사 비용
        
        public bool enableCaching = true;           // 결과 캐싱 사용
        public bool enableQuickCheck = true;        // 빠른 사전 검사 사용
        
        [Header("적응형 임계값")]
        public bool adaptiveThreshold = false;      // 적응적 임계값 조정
        
        [Range(0f, 1f)]
        public float learningRate = 0.1f;           // 학습률 (적응형 모드)
        
        [Range(0f, 1f)]
        public float confidence = 0.8f;             // 신뢰도 임계값
        
        [Header("ML 최적화")]
        public bool isOptimizable = true;           // ML 최적화 대상 여부
        public bool trackSuccessRate = true;        // 성공률 추적
        public bool trackExecutionTime = true;      // 실행 시간 추적
        
        public string GetParameterName() => parameterName;
        public string GetDescription() => description;
        
        public bool IsValid()
        {
            return primaryThreshold >= 0f && 
                   secondaryThreshold >= 0f && 
                   toleranceRange >= 0f && 
                   priority >= 1 && 
                   checkCost >= 0f && 
                   learningRate >= 0f && learningRate <= 1f && 
                   confidence >= 0f && confidence <= 1f;
        }
        
        public void ResetToDefault()
        {
            primaryThreshold = 30f;
            secondaryThreshold = 0f;
            toleranceRange = 0f;
            checkSelf = true;
            checkEnemy = false;
            checkEnvironment = false;
            comparisonType = ComparisonType.LessEqual;
            useAbsoluteValue = false;
            invertResult = false;
            priority = 5;
            checkCost = 0.1f;
            enableCaching = true;
            enableQuickCheck = true;
            adaptiveThreshold = false;
            learningRate = 0.1f;
            confidence = 0.8f;
            isOptimizable = true;
            trackSuccessRate = true;
            trackExecutionTime = true;
        }
        
        public float[] ToArray()
        {
            return new float[]
            {
                primaryThreshold,
                secondaryThreshold,
                toleranceRange,
                checkSelf ? 1f : 0f,
                checkEnemy ? 1f : 0f,
                checkEnvironment ? 1f : 0f,
                (float)comparisonType,
                useAbsoluteValue ? 1f : 0f,
                invertResult ? 1f : 0f,
                priority,
                checkCost,
                enableCaching ? 1f : 0f,
                enableQuickCheck ? 1f : 0f,
                adaptiveThreshold ? 1f : 0f,
                learningRate,
                confidence,
                trackSuccessRate ? 1f : 0f,
                trackExecutionTime ? 1f : 0f
            };
        }
        
        public void FromArray(float[] values)
        {
            if (values == null || values.Length < GetParameterCount()) return;
            
            var constraints = GetConstraints();
            
            primaryThreshold = constraints.ClampValue(0, values[0]);
            secondaryThreshold = constraints.ClampValue(1, values[1]);
            toleranceRange = constraints.ClampValue(2, values[2]);
            checkSelf = values[3] > 0.5f;
            checkEnemy = values[4] > 0.5f;
            checkEnvironment = values[5] > 0.5f;
            comparisonType = (ComparisonType)(int)constraints.ClampValue(6, values[6]);
            useAbsoluteValue = values[7] > 0.5f;
            invertResult = values[8] > 0.5f;
            priority = (int)constraints.ClampValue(9, values[9]);
            checkCost = constraints.ClampValue(10, values[10]);
            enableCaching = values[11] > 0.5f;
            enableQuickCheck = values[12] > 0.5f;
            adaptiveThreshold = values[13] > 0.5f;
            learningRate = constraints.ClampValue(14, values[14]);
            confidence = constraints.ClampValue(15, values[15]);
            trackSuccessRate = values[16] > 0.5f;
            trackExecutionTime = values[17] > 0.5f;
        }
        
        public int GetParameterCount() => 18;
        
        public ParameterConstraints GetConstraints()
        {
            var constraints = ParameterConstraints.Create(GetParameterCount());
            
            constraints.minValues = new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
            constraints.maxValues = new float[] { 100f, 100f, 10f, 1f, 1f, 1f, 4f, 1f, 1f, 10f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            constraints.isInteger = new bool[] { false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, false, false, false };
            constraints.paramNames = new string[]
            {
                "primaryThreshold", "secondaryThreshold", "toleranceRange", "checkSelf", "checkEnemy",
                "checkEnvironment", "comparisonType", "useAbsoluteValue", "invertResult", "priority",
                "checkCost", "enableCaching", "enableQuickCheck", "adaptiveThreshold", "learningRate",
                "confidence", "trackSuccessRate", "trackExecutionTime"
            };
            
            return constraints;
        }
        
        public INodeParameters Clone()
        {
            return new ConditionParameters
            {
                parameterName = this.parameterName,
                description = this.description,
                primaryThreshold = this.primaryThreshold,
                secondaryThreshold = this.secondaryThreshold,
                toleranceRange = this.toleranceRange,
                checkSelf = this.checkSelf,
                checkEnemy = this.checkEnemy,
                checkEnvironment = this.checkEnvironment,
                comparisonType = this.comparisonType,
                useAbsoluteValue = this.useAbsoluteValue,
                invertResult = this.invertResult,
                priority = this.priority,
                checkCost = this.checkCost,
                enableCaching = this.enableCaching,
                enableQuickCheck = this.enableQuickCheck,
                adaptiveThreshold = this.adaptiveThreshold,
                learningRate = this.learningRate,
                confidence = this.confidence,
                isOptimizable = this.isOptimizable,
                trackSuccessRate = this.trackSuccessRate,
                trackExecutionTime = this.trackExecutionTime
            };
        }
    }
    
    /// <summary>
    /// 조건 비교 방식 열거형
    /// </summary>
    public enum ComparisonType
    {
        Less = 0,           // <
        LessEqual = 1,      // <=
        Equal = 2,          // ==
        GreaterEqual = 3,   // >=
        Greater = 4         // >
    }
}

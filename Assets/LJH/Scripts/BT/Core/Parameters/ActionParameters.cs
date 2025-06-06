using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 액션 노드용 파라미터 클래스
    /// 공격, 방어, 회피 등 액션 수행 노드들이 사용
    /// </summary>
    [System.Serializable]
    public class ActionParameters : INodeParameters
    {
        [Header("액션 기본 설정")]
        public string parameterName = "ActionParameters";
        public string description = "액션 노드 파라미터";
        
        [Header("범위 및 거리")]
        [Range(0.5f, 10f)]
        public float range = 2f;                    // 액션 실행 범위
        
        [Range(0f, 5f)]
        public float optimalRange = 1.5f;           // 최적 실행 범위
        
        [Header("실행 조건")]
        [Range(0f, 1f)]
        public float cooldownThreshold = 0.1f;      // 쿨다운 임계값
        
        [Range(0f, 100f)]
        public float hpThreshold = 0f;              // 최소 HP 임계값
        
        [Header("실행 특성")]
        [Range(0.1f, 5f)]
        public float executionTime = 1f;            // 예상 실행 시간
        
        [Range(0f, 1f)]
        public float interruptChance = 0.2f;        // 중단 가능성
        
        [Range(0f, 2f)]
        public float intensity = 1f;                // 실행 강도 (데미지 배율 등)
        
        [Header("효과 및 비용")]
        [Range(0f, 1f)]
        public float executionCost = 0.3f;          // 상대적 실행 비용
        
        [Range(0f, 2f)]
        public float expectedEffect = 1f;           // 예상 효과값
        
        [Header("연계 및 우선순위")]
        [Range(1, 10)]
        public int priority = 5;                    // 액션 우선순위
        
        public bool allowChaining = true;           // 연계 액션 허용
        public bool canInterrupt = true;            // 중단 가능 여부
        
        [Header("ML 최적화")]
        public bool isOptimizable = true;           // ML 최적화 대상 여부
        public bool adaptiveRange = false;          // 적응적 범위 조정
        public bool adaptiveIntensity = false;      // 적응적 강도 조정
        
        public string GetParameterName() => parameterName;
        public string GetDescription() => description;
        
        public bool IsValid()
        {
            return range > 0f && 
                   optimalRange <= range && 
                   cooldownThreshold >= 0f && 
                   hpThreshold >= 0f && 
                   executionTime > 0f &&
                   intensity > 0f;
        }
        
        public void ResetToDefault()
        {
            range = 2f;
            optimalRange = 1.5f;
            cooldownThreshold = 0.1f;
            hpThreshold = 0f;
            executionTime = 1f;
            interruptChance = 0.2f;
            intensity = 1f;
            executionCost = 0.3f;
            expectedEffect = 1f;
            priority = 5;
            allowChaining = true;
            canInterrupt = true;
            isOptimizable = true;
            adaptiveRange = false;
            adaptiveIntensity = false;
        }
        
        public float[] ToArray()
        {
            return new float[]
            {
                range,
                optimalRange,
                cooldownThreshold,
                hpThreshold,
                executionTime,
                interruptChance,
                intensity,
                executionCost,
                expectedEffect,
                priority,
                allowChaining ? 1f : 0f,
                canInterrupt ? 1f : 0f,
                adaptiveRange ? 1f : 0f,
                adaptiveIntensity ? 1f : 0f
            };
        }
        
        public void FromArray(float[] values)
        {
            if (values == null || values.Length < GetParameterCount()) return;
            
            var constraints = GetConstraints();
            
            range = constraints.ClampValue(0, values[0]);
            optimalRange = constraints.ClampValue(1, values[1]);
            cooldownThreshold = constraints.ClampValue(2, values[2]);
            hpThreshold = constraints.ClampValue(3, values[3]);
            executionTime = constraints.ClampValue(4, values[4]);
            interruptChance = constraints.ClampValue(5, values[5]);
            intensity = constraints.ClampValue(6, values[6]);
            executionCost = constraints.ClampValue(7, values[7]);
            expectedEffect = constraints.ClampValue(8, values[8]);
            priority = (int)constraints.ClampValue(9, values[9]);
            allowChaining = values[10] > 0.5f;
            canInterrupt = values[11] > 0.5f;
            adaptiveRange = values[12] > 0.5f;
            adaptiveIntensity = values[13] > 0.5f;
        }
        
        public int GetParameterCount() => 14;
        
        public ParameterConstraints GetConstraints()
        {
            var constraints = ParameterConstraints.Create(GetParameterCount());
            
            // 범위 설정
            constraints.minValues = new float[] { 0.5f, 0f, 0f, 0f, 0.1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f };
            constraints.maxValues = new float[] { 10f, 5f, 1f, 100f, 5f, 1f, 2f, 1f, 2f, 10f, 1f, 1f, 1f, 1f };
            constraints.isInteger = new bool[] { false, false, false, false, false, false, false, false, false, true, false, false, false, false };
            constraints.paramNames = new string[]
            {
                "range", "optimalRange", "cooldownThreshold", "hpThreshold",
                "executionTime", "interruptChance", "intensity", "executionCost",
                "expectedEffect", "priority", "allowChaining", "canInterrupt",
                "adaptiveRange", "adaptiveIntensity"
            };
            
            return constraints;
        }
        
        public INodeParameters Clone()
        {
            return new ActionParameters
            {
                parameterName = this.parameterName,
                description = this.description,
                range = this.range,
                optimalRange = this.optimalRange,
                cooldownThreshold = this.cooldownThreshold,
                hpThreshold = this.hpThreshold,
                executionTime = this.executionTime,
                interruptChance = this.interruptChance,
                intensity = this.intensity,
                executionCost = this.executionCost,
                expectedEffect = this.expectedEffect,
                priority = this.priority,
                allowChaining = this.allowChaining,
                canInterrupt = this.canInterrupt,
                isOptimizable = this.isOptimizable,
                adaptiveRange = this.adaptiveRange,
                adaptiveIntensity = this.adaptiveIntensity
            };
        }
    }
}

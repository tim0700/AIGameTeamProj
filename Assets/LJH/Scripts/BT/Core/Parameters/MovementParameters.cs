using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 이동 노드용 파라미터 클래스
    /// 적에게 이동, 거리 유지, 아레나 복귀, 순찰 등 이동 관련 노드들이 사용
    /// </summary>
    [System.Serializable]
    public class MovementParameters : INodeParameters
    {
        [Header("이동 기본 설정")]
        public string parameterName = "MovementParameters";
        public string description = "이동 노드 파라미터";
        
        [Header("거리 설정")]
        [Range(0.1f, 10f)]
        public float stoppingDistance = 2f;         // 정지 거리
        
        [Range(0.1f, 15f)]
        public float maxRange = 8f;                 // 최대 이동 범위
        
        [Range(0.1f, 5f)]
        public float safeDistance = 3f;             // 안전 거리
        
        [Header("속도 설정")]
        [Range(0.1f, 2f)]
        public float moveSpeed = 1f;                // 이동 속도 배율
        
        [Range(0.1f, 5f)]
        public float rotationSpeed = 2f;            // 회전 속도
        
        [Range(0.1f, 3f)]
        public float acceleration = 1.5f;           // 가속도
        
        [Header("경로 계획")]
        public PathfindingType pathfindingType = PathfindingType.Direct;  // 경로 찾기 방식
        public bool enableObstacleAvoidance = true; // 장애물 회피
        public bool enableSmoothing = true;         // 경로 스무딩
        
        [Range(0f, 2f)]
        public float pathUpdateInterval = 0.2f;     // 경로 업데이트 간격
        
        [Header("회피 설정")]
        public bool allowEvasion = true;            // 이동 중 회피 허용
        
        [Range(0f, 1f)]
        public float evasionChance = 0.3f;          // 회피 확률
        
        [Range(0.1f, 5f)]
        public float evasionDistance = 2f;          // 회피 거리
        
        [Header("아레나 제약")]
        public bool respectArenaBounds = true;      // 아레나 경계 준수
        
        [Range(0f, 5f)]
        public float boundaryPadding = 1f;          // 경계 여유 공간
        
        public bool autoReturnToBounds = true;      // 경계 밖 자동 복귀
        
        [Header("우선순위 및 성능")]
        [Range(1, 10)]
        public int priority = 5;                    // 이동 우선순위
        
        [Range(0f, 1f)]
        public float movementCost = 0.2f;           // 상대적 이동 비용
        
        public bool enablePrediction = true;        // 목표 위치 예측
        
        [Range(0f, 2f)]
        public float predictionTime = 0.5f;         // 예측 시간
        
        [Header("적응형 설정")]
        public bool adaptiveSpeed = false;          // 적응적 속도 조정
        public bool adaptiveDistance = false;       // 적응적 거리 조정
        
        [Range(0f, 1f)]
        public float adaptationRate = 0.1f;         // 적응 학습률
        
        [Header("ML 최적화")]
        public bool isOptimizable = true;           // ML 최적화 대상 여부
        public bool trackEfficiency = true;        // 이동 효율성 추적
        public bool trackCollisions = true;         // 충돌 추적
        
        public string GetParameterName() => parameterName;
        public string GetDescription() => description;
        
        public bool IsValid()
        {
            return stoppingDistance > 0f && 
                   maxRange > stoppingDistance && 
                   safeDistance > 0f && 
                   moveSpeed > 0f && 
                   rotationSpeed > 0f && 
                   acceleration > 0f && 
                   pathUpdateInterval >= 0f && 
                   evasionChance >= 0f && evasionChance <= 1f && 
                   evasionDistance > 0f && 
                   boundaryPadding >= 0f && 
                   priority >= 1 && 
                   movementCost >= 0f && movementCost <= 1f && 
                   predictionTime >= 0f && 
                   adaptationRate >= 0f && adaptationRate <= 1f;
        }
        
        public void ResetToDefault()
        {
            stoppingDistance = 2f;
            maxRange = 8f;
            safeDistance = 3f;
            moveSpeed = 1f;
            rotationSpeed = 2f;
            acceleration = 1.5f;
            pathfindingType = PathfindingType.Direct;
            enableObstacleAvoidance = true;
            enableSmoothing = true;
            pathUpdateInterval = 0.2f;
            allowEvasion = true;
            evasionChance = 0.3f;
            evasionDistance = 2f;
            respectArenaBounds = true;
            boundaryPadding = 1f;
            autoReturnToBounds = true;
            priority = 5;
            movementCost = 0.2f;
            enablePrediction = true;
            predictionTime = 0.5f;
            adaptiveSpeed = false;
            adaptiveDistance = false;
            adaptationRate = 0.1f;
            isOptimizable = true;
            trackEfficiency = true;
            trackCollisions = true;
        }
        
        public float[] ToArray()
        {
            return new float[]
            {
                stoppingDistance,
                maxRange,
                safeDistance,
                moveSpeed,
                rotationSpeed,
                acceleration,
                (float)pathfindingType,
                enableObstacleAvoidance ? 1f : 0f,
                enableSmoothing ? 1f : 0f,
                pathUpdateInterval,
                allowEvasion ? 1f : 0f,
                evasionChance,
                evasionDistance,
                respectArenaBounds ? 1f : 0f,
                boundaryPadding,
                autoReturnToBounds ? 1f : 0f,
                priority,
                movementCost,
                enablePrediction ? 1f : 0f,
                predictionTime,
                adaptiveSpeed ? 1f : 0f,
                adaptiveDistance ? 1f : 0f,
                adaptationRate,
                trackEfficiency ? 1f : 0f,
                trackCollisions ? 1f : 0f
            };
        }
        
        public void FromArray(float[] values)
        {
            if (values == null || values.Length < GetParameterCount()) return;
            
            var constraints = GetConstraints();
            
            stoppingDistance = constraints.ClampValue(0, values[0]);
            maxRange = constraints.ClampValue(1, values[1]);
            safeDistance = constraints.ClampValue(2, values[2]);
            moveSpeed = constraints.ClampValue(3, values[3]);
            rotationSpeed = constraints.ClampValue(4, values[4]);
            acceleration = constraints.ClampValue(5, values[5]);
            pathfindingType = (PathfindingType)(int)constraints.ClampValue(6, values[6]);
            enableObstacleAvoidance = values[7] > 0.5f;
            enableSmoothing = values[8] > 0.5f;
            pathUpdateInterval = constraints.ClampValue(9, values[9]);
            allowEvasion = values[10] > 0.5f;
            evasionChance = constraints.ClampValue(11, values[11]);
            evasionDistance = constraints.ClampValue(12, values[12]);
            respectArenaBounds = values[13] > 0.5f;
            boundaryPadding = constraints.ClampValue(14, values[14]);
            autoReturnToBounds = values[15] > 0.5f;
            priority = (int)constraints.ClampValue(16, values[16]);
            movementCost = constraints.ClampValue(17, values[17]);
            enablePrediction = values[18] > 0.5f;
            predictionTime = constraints.ClampValue(19, values[19]);
            adaptiveSpeed = values[20] > 0.5f;
            adaptiveDistance = values[21] > 0.5f;
            adaptationRate = constraints.ClampValue(22, values[22]);
            trackEfficiency = values[23] > 0.5f;
            trackCollisions = values[24] > 0.5f;
        }
        
        public int GetParameterCount() => 25;
        
        public ParameterConstraints GetConstraints()
        {
            var constraints = ParameterConstraints.Create(GetParameterCount());
            
            constraints.minValues = new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f, 0.1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
            constraints.maxValues = new float[] { 10f, 15f, 5f, 2f, 5f, 3f, 2f, 1f, 1f, 2f, 1f, 1f, 5f, 1f, 5f, 1f, 10f, 1f, 1f, 2f, 1f, 1f, 1f, 1f, 1f };
            constraints.isInteger = new bool[] { false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false };
            constraints.paramNames = new string[]
            {
                "stoppingDistance", "maxRange", "safeDistance", "moveSpeed", "rotationSpeed",
                "acceleration", "pathfindingType", "enableObstacleAvoidance", "enableSmoothing", "pathUpdateInterval",
                "allowEvasion", "evasionChance", "evasionDistance", "respectArenaBounds", "boundaryPadding",
                "autoReturnToBounds", "priority", "movementCost", "enablePrediction", "predictionTime",
                "adaptiveSpeed", "adaptiveDistance", "adaptationRate", "trackEfficiency", "trackCollisions"
            };
            
            return constraints;
        }
        
        public INodeParameters Clone()
        {
            return new MovementParameters
            {
                parameterName = this.parameterName,
                description = this.description,
                stoppingDistance = this.stoppingDistance,
                maxRange = this.maxRange,
                safeDistance = this.safeDistance,
                moveSpeed = this.moveSpeed,
                rotationSpeed = this.rotationSpeed,
                acceleration = this.acceleration,
                pathfindingType = this.pathfindingType,
                enableObstacleAvoidance = this.enableObstacleAvoidance,
                enableSmoothing = this.enableSmoothing,
                pathUpdateInterval = this.pathUpdateInterval,
                allowEvasion = this.allowEvasion,
                evasionChance = this.evasionChance,
                evasionDistance = this.evasionDistance,
                respectArenaBounds = this.respectArenaBounds,
                boundaryPadding = this.boundaryPadding,
                autoReturnToBounds = this.autoReturnToBounds,
                priority = this.priority,
                movementCost = this.movementCost,
                enablePrediction = this.enablePrediction,
                predictionTime = this.predictionTime,
                adaptiveSpeed = this.adaptiveSpeed,
                adaptiveDistance = this.adaptiveDistance,
                adaptationRate = this.adaptationRate,
                isOptimizable = this.isOptimizable,
                trackEfficiency = this.trackEfficiency,
                trackCollisions = this.trackCollisions
            };
        }
    }
    
    /// <summary>
    /// 경로 찾기 방식 열거형
    /// </summary>
    public enum PathfindingType
    {
        Direct = 0,         // 직선 이동
        Smooth = 1,         // 부드러운 경로
        Tactical = 2        // 전술적 이동 (엄폐, 우회 등)
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT 노드의 메타데이터 및 실행 통계 정보
    /// ML 학습과 성능 분석을 위한 데이터 수집
    /// </summary>
    [System.Serializable]
    public struct NodeMetadata
    {
        [Header("노드 기본 정보")]
        public string nodeName;          // 노드 이름
        public string nodeType;          // "Action", "Condition", "Movement"
        public string description;       // 노드 설명
        
        [Header("실행 통계")]
        public int executionCount;       // 총 실행 횟수
        public int successCount;         // 성공 횟수
        public int failureCount;         // 실패 횟수
        public int runningCount;         // Running 상태 횟수
        
        [Header("성능 정보")]
        public float averageExecutionTime;   // 평균 실행 시간 (ms)
        public float lastExecutionTime;      // 마지막 실행 시간 (ms)
        public float totalExecutionTime;     // 총 실행 시간 (ms)
        
        [Header("ML 최적화")]
        public bool isOptimizable;       // ML 최적화 가능 여부
        public float[] parameters;       // 최적화 가능한 파라미터들
        public float successRate;        // 성공률 (0.0 ~ 1.0)
        
        /// <summary>
        /// 기본 메타데이터 생성
        /// </summary>
        public static NodeMetadata Create(string name, string type, string desc = "")
        {
            return new NodeMetadata
            {
                nodeName = name,
                nodeType = type,
                description = desc,
                executionCount = 0,
                successCount = 0,
                failureCount = 0,
                runningCount = 0,
                averageExecutionTime = 0f,
                lastExecutionTime = 0f,
                totalExecutionTime = 0f,
                isOptimizable = false,
                parameters = new float[0],
                successRate = 0f
            };
        }
        
        /// <summary>
        /// 실행 결과 업데이트
        /// </summary>
        public void UpdateStats(NodeState result, float executionTime)
        {
            executionCount++;
            lastExecutionTime = executionTime;
            totalExecutionTime += executionTime;
            
            switch (result)
            {
                case NodeState.Success:
                    successCount++;
                    break;
                case NodeState.Failure:
                    failureCount++;
                    break;
                case NodeState.Running:
                    runningCount++;
                    break;
            }
            
            // 평균 실행 시간 계산
            if (executionCount > 0)
            {
                averageExecutionTime = totalExecutionTime / executionCount;
            }
            
            // 성공률 계산 (Running 제외)
            int completedExecutions = successCount + failureCount;
            if (completedExecutions > 0)
            {
                successRate = (float)successCount / completedExecutions;
            }
        }
        
        /// <summary>
        /// 메타데이터 리셋
        /// </summary>
        public void Reset()
        {
            executionCount = 0;
            successCount = 0;
            failureCount = 0;
            runningCount = 0;
            averageExecutionTime = 0f;
            lastExecutionTime = 0f;
            totalExecutionTime = 0f;
            successRate = 0f;
        }
    }
}
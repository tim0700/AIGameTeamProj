using UnityEngine;
using System.Collections.Generic;

namespace LJH.BT
{
    /// <summary>
    /// 확장된 BT 노드 베이스 클래스
    /// 기존 BTNode와 호환성을 유지하면서 메타데이터, 파라미터, 성능 측정 등 고급 기능 제공
    /// </summary>
    public abstract class BTNodeBase : BTNode, IBTNode
    {
        [Header("노드 메타정보")]
        [SerializeField] protected string nodeName = "BTNode";
        [SerializeField] protected string nodeDescription = "";
        [SerializeField] protected bool isActive = true;
        
        [Header("메타데이터 추적")]
        [SerializeField] protected NodeMetadata metadata;
        [SerializeField] protected bool enableMetadataTracking = true;
        
        [Header("성능 최적화")]
        [SerializeField] protected bool enableCaching = false;
        [SerializeField] protected float cacheValidDuration = 0.1f; // 캐시 유효 시간 (초)
        
        // 내부 변수들
        protected INodeParameters parameters;
        protected System.Diagnostics.Stopwatch executionTimer;
        protected bool isInitialized = false;
        
        // 캐싱 시스템
        protected NodeState? cachedResult = null;
        protected float lastCacheTime = -1f;
        protected GameObservation lastObservation;
        
        // 디버깅 및 로깅
        protected bool enableDetailedLogging = false;
        
        public BTNodeBase()
        {
            executionTimer = new System.Diagnostics.Stopwatch();
            InitializeMetadata();
        }
        
        public BTNodeBase(string name, string description = "") : this()
        {
            nodeName = name;
            nodeDescription = description;
        }
        
        #region IBTNode 구현
        
        /// <summary>
        /// 메인 평가 메서드 (기존 BTNode 호환)
        /// 성능 측정, 캐싱, 메타데이터 업데이트 등 고급 기능 포함
        /// </summary>
        public override NodeState Evaluate(GameObservation observation)
        {
            // 캐시 확인
            if (enableCaching && IsCacheValid(observation))
            {
                return cachedResult.Value;
            }
            
            // 실행 시간 측정 시작
            if (enableMetadataTracking)
            {
                executionTimer.Restart();
            }
            
            // 실제 노드 실행
            NodeState result;
            try
            {
                result = EvaluateNode(observation);
                state = result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{nodeName}] 노드 실행 중 오류 발생: {ex.Message}");
                result = NodeState.Failure;
                state = result;
            }
            
            // 실행 시간 측정 종료 및 메타데이터 업데이트
            if (enableMetadataTracking)
            {
                executionTimer.Stop();
                float executionTime = (float)executionTimer.Elapsed.TotalMilliseconds;
                UpdateMetadata(result, executionTime);
            }
            
            // 캐시 업데이트
            if (enableCaching)
            {
                UpdateCache(observation, result);
            }
            
            // 디버그 로깅
            if (enableDetailedLogging)
            {
                LogExecution(observation, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// 하위 클래스에서 구현해야 하는 실제 노드 로직
        /// </summary>
        protected abstract NodeState EvaluateNode(GameObservation observation);
        
        public override void Initialize(AgentController controller)
        {
            base.Initialize(controller);
            isInitialized = true;
            
            // 파라미터 초기화
            InitializeParameters();
            
            // 커스텀 초기화
            OnInitialize();
            
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 노드가 초기화되었습니다.");
            }
        }
        
        public virtual string GetNodeName() => nodeName;
        
        public virtual NodeMetadata GetMetadata() => metadata;
        
        public virtual void UpdateMetadata(NodeState result, float executionTime)
        {
            if (!enableMetadataTracking) return;
            
            metadata.UpdateStats(result, executionTime);
        }
        
        public virtual void Reset()
        {
            state = NodeState.Running;
            cachedResult = null;
            lastCacheTime = -1f;
            
            // 커스텀 리셋 로직
            OnReset();
            
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 노드가 리셋되었습니다.");
            }
        }
        
        public virtual bool IsActive() => isActive && isInitialized;
        
        public virtual string GetDescription() => string.IsNullOrEmpty(nodeDescription) ? $"{nodeName} 노드" : nodeDescription;
        
        #endregion
        
        #region 파라미터 시스템
        
        /// <summary>
        /// 노드 파라미터 설정
        /// </summary>
        public virtual void SetParameters(INodeParameters newParameters)
        {
            if (newParameters != null && newParameters.IsValid())
            {
                parameters = newParameters.Clone();
                OnParametersChanged();
            }
            else
            {
                Debug.LogWarning($"[{nodeName}] 유효하지 않은 파라미터가 설정되려고 했습니다.");
            }
        }
        
        /// <summary>
        /// 현재 파라미터 반환
        /// </summary>
        public virtual INodeParameters GetParameters() => parameters?.Clone();
        
        /// <summary>
        /// 파라미터 초기화 (하위 클래스에서 오버라이드)
        /// </summary>
        protected virtual void InitializeParameters() { }
        
        /// <summary>
        /// 파라미터 변경 시 호출되는 콜백
        /// </summary>
        protected virtual void OnParametersChanged() { }
        
        #endregion
        
        #region 캐싱 시스템
        
        /// <summary>
        /// 캐시가 유효한지 확인
        /// </summary>
        protected virtual bool IsCacheValid(GameObservation observation)
        {
            if (!enableCaching || !cachedResult.HasValue) return false;
            
            float currentTime = Time.time;
            bool timeValid = (currentTime - lastCacheTime) < cacheValidDuration;
            bool observationSimilar = IsObservationSimilar(observation, lastObservation);
            
            return timeValid && observationSimilar;
        }
        
        /// <summary>
        /// 관찰 데이터가 유사한지 확인 (하위 클래스에서 오버라이드 가능)
        /// </summary>
        protected virtual bool IsObservationSimilar(GameObservation current, GameObservation cached)
        {
            // 기본 구현: 위치와 HP만 확인
            float positionDelta = Vector3.Distance(current.selfPosition, cached.selfPosition);
            float hpDelta = Mathf.Abs(current.selfHP - cached.selfHP);
            
            return positionDelta < 0.1f && hpDelta < 1f;
        }
        
        /// <summary>
        /// 캐시 업데이트
        /// </summary>
        protected virtual void UpdateCache(GameObservation observation, NodeState result)
        {
            cachedResult = result;
            lastCacheTime = Time.time;
            lastObservation = observation;
        }
        
        #endregion
        
        #region 디버깅 및 로깅
        
        /// <summary>
        /// 실행 로깅
        /// </summary>
        protected virtual void LogExecution(GameObservation observation, NodeState result)
        {
            string logMessage = $"[{nodeName}] 실행 완료 - 결과: {result}";
            
            if (enableMetadataTracking)
            {
                logMessage += $", 실행시간: {metadata.lastExecutionTime:F2}ms";
                logMessage += $", 성공률: {metadata.successRate:P1}";
            }
            
            Debug.Log(logMessage);
        }
        
        /// <summary>
        /// 상세 디버그 로깅 활성화/비활성화
        /// </summary>
        public virtual void SetDetailedLogging(bool enable)
        {
            enableDetailedLogging = enable;
        }
        
        #endregion
        
        #region 가상 메서드들 (하위 클래스에서 오버라이드 가능)
        
        /// <summary>
        /// 커스텀 초기화 로직
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// 커스텀 리셋 로직
        /// </summary>
        protected virtual void OnReset() { }
        
        /// <summary>
        /// 메타데이터 초기화
        /// </summary>
        protected virtual void InitializeMetadata()
        {
            metadata = NodeMetadata.Create(nodeName, GetNodeType(), nodeDescription);
        }
        
        /// <summary>
        /// 노드 타입 반환 (하위 클래스에서 구현)
        /// </summary>
        protected abstract string GetNodeType();
        
        #endregion
        
        #region Unity Inspector 지원
        
        [System.Serializable]
        public class DebugInfo
        {
            public bool showMetadata = false;
            public bool showParameters = false;
            public bool showCacheInfo = false;
        }
        
        [Header("디버그 정보")]
        [SerializeField] protected DebugInfo debugInfo = new DebugInfo();
        
        /// <summary>
        /// Inspector에서 메타데이터 정보 표시용
        /// </summary>
        [System.Serializable]
        public class InspectorMetadata
        {
            public int executionCount;
            public float successRate;
            public float averageExecutionTime;
            
            public void UpdateFrom(NodeMetadata metadata)
            {
                executionCount = metadata.executionCount;
                successRate = metadata.successRate;
                averageExecutionTime = metadata.averageExecutionTime;
            }
        }
        
        [SerializeField] protected InspectorMetadata inspectorMetadata = new InspectorMetadata();
        
        /// <summary>
        /// Inspector 정보 업데이트 (에디터에서만 호출)
        /// </summary>
        public virtual void UpdateInspectorInfo()
        {
            #if UNITY_EDITOR
            if (enableMetadataTracking)
            {
                inspectorMetadata.UpdateFrom(metadata);
            }
            #endif
        }
        
        #endregion
    }
}

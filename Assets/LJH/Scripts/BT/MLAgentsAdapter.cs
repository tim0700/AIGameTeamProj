using UnityEngine;
using LJH.BT;

namespace LJH.BT
{
    /// <summary>
    /// ML-Agents 환경에서 BT 에이전트가 올바른 GameObservation을 받도록 하는 어댑터
    /// BattleManager 대신 ML-Agents 환경 정보를 사용하여 GameObservation 생성
    /// </summary>
    public class MLAgentsAdapter : MonoBehaviour
    {
        [Header("아레나 설정 (ML-Agents 호환)")]
        public Vector3 arenaCenter = Vector3.zero;
        public float arenaRadius = 15f; // ML-Agents와 동일하게 설정
        
        [Header("BT 에이전트들")]
        public AgentController btAgent;
        public AgentController mlAgent; // 상대방 (ML-Agents)
        
        [Header("디버깅")]
        public bool enableDebugLogs = true;
        public bool showArenaGizmos = true;
        
        // 내부 변수들
        private bool isInitialized = false;
        private int updateFrameCounter = 0;
        private int debugLogInterval = 60; // 60프레임마다 디버그 로그
        
        void Start()
        {
            InitializeAdapter();
        }
        
        void Update()
        {
            if (isInitialized && btAgent != null && mlAgent != null)
            {
                UpdateBTAgent();
                
                // 디버깅 로그
                if (enableDebugLogs && updateFrameCounter % debugLogInterval == 0)
                {
                    LogAdapterStatus();
                }
                
                updateFrameCounter++;
            }
        }
        
        /// <summary>
        /// 어댑터 초기화
        /// </summary>
        private void InitializeAdapter()
        {
            // 아레나 설정 자동 감지 시도
            if (arenaCenter == Vector3.zero && arenaRadius == 15f)
            {
                DetectArenaSettings();
            }
            
            // BT 에이전트 자동 감지 시도
            if (btAgent == null)
            {
                DetectBTAgent();
            }
            
            // ML 에이전트 자동 감지 시도
            if (mlAgent == null)
            {
                DetectMLAgent();
            }
            
            isInitialized = (btAgent != null && mlAgent != null);
            
            if (isInitialized)
            {
                Debug.Log($"[MLAgentsAdapter] 초기화 완료 - 아레나: {arenaCenter}, 반지름: {arenaRadius}");
            }
            else
            {
                Debug.LogWarning("[MLAgentsAdapter] 초기화 실패 - 에이전트를 찾을 수 없습니다.");
            }
        }
        
        /// <summary>
        /// BT 에이전트 업데이트 (올바른 GameObservation 제공)
        /// </summary>
        private void UpdateBTAgent()
        {
            if (btAgent == null || mlAgent == null) return;
            
            try
            {
                // 올바른 GameObservation 생성
                GameObservation observation = CreateGameObservation();
                
                // BT 에이전트 업데이트 (BattleManager 우회)
                if (btAgent.agent != null)
                {
                    AgentAction action = btAgent.agent.DecideAction(observation);
                    ActionResult result = btAgent.ExecuteAction(action);
                    btAgent.agent.OnActionResult(result);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MLAgentsAdapter] BT 에이전트 업데이트 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ML-Agents 환경에 맞는 GameObservation 생성
        /// </summary>
        private GameObservation CreateGameObservation()
        {
            return new GameObservation
            {
                selfPosition = btAgent.transform.position,
                enemyPosition = mlAgent.transform.position,
                selfHP = btAgent.GetCurrentHP(),
                enemyHP = mlAgent.GetCurrentHP(),
                cooldowns = btAgent.GetCooldownState(),
                distanceToEnemy = Vector3.Distance(btAgent.transform.position, mlAgent.transform.position),
                currentState = btAgent.GetCurrentState(),
                arenaCenter = arenaCenter,         // ✅ ML-Agents와 일치하는 값
                arenaRadius = arenaRadius          // ✅ ML-Agents와 일치하는 값
            };
        }
        
        #region 자동 감지 메서드들
        
        /// <summary>
        /// 아레나 설정 자동 감지
        /// </summary>
        private void DetectArenaSettings()
        {
            // PlayerRLAgent에서 아레나 설정 찾기
            var rlAgent = FindFirstObjectByType<PlayerRLAgent>();
            if (rlAgent != null && rlAgent.arenaCenter != null)
            {
                arenaCenter = rlAgent.arenaCenter.position;
                arenaRadius = 15f; // PlayerRLAgent의 arenaHalf * 2
                Debug.Log($"[MLAgentsAdapter] 아레나 설정 자동 감지: {arenaCenter}, {arenaRadius}");
            }
        }
        
        /// <summary>
        /// BT 에이전트 자동 감지
        /// </summary>
        private void DetectBTAgent()
        {
            // AggressiveBTAgent 또는 DefensiveBTAgent 찾기
            var btAgents = FindObjectsByType<AgentController>(FindObjectsSortMode.None);
            foreach (var agent in btAgents)
            {
                var btAgentComponent = agent.GetComponent<BTAgentBase>();
                if (btAgentComponent != null)
                {
                    btAgent = agent;
                    Debug.Log($"[MLAgentsAdapter] BT 에이전트 자동 감지: {btAgent.name}");
                    break;
                }
            }
        }
        
        /// <summary>
        /// ML 에이전트 자동 감지
        /// </summary>
        private void DetectMLAgent()
        {
            // PlayerRL 컴포넌트를 가진 에이전트 찾기
            var mlAgents = FindObjectsByType<AgentController>(FindObjectsSortMode.None);
            foreach (var agent in mlAgents)
            {
                var playerRL = agent.GetComponent<PlayerRL>();
                if (playerRL != null)
                {
                    mlAgent = agent;
                    Debug.Log($"[MLAgentsAdapter] ML 에이전트 자동 감지: {mlAgent.name}");
                    break;
                }
            }
        }
        
        #endregion
        
        #region 디버깅 및 유틸리티
        
        /// <summary>
        /// 어댑터 상태 로깅
        /// </summary>
        private void LogAdapterStatus()
        {
            if (btAgent == null || mlAgent == null) return;
            
            float btDistanceFromCenter = Vector3.Distance(btAgent.transform.position, arenaCenter);
            float mlDistanceFromCenter = Vector3.Distance(mlAgent.transform.position, arenaCenter);
            
            Debug.Log($"[MLAgentsAdapter] 프레임: {updateFrameCounter} | " +
                     $"BT거리: {btDistanceFromCenter:F2}/{arenaRadius:F2} | " +
                     $"ML거리: {mlDistanceFromCenter:F2}/{arenaRadius:F2} | " +
                     $"BT-HP: {btAgent.GetCurrentHP():F0} | " +
                     $"ML-HP: {mlAgent.GetCurrentHP():F0}");
        }
        
        /// <summary>
        /// 아레나 설정 수동 변경
        /// </summary>
        public void SetArenaSettings(Vector3 center, float radius)
        {
            arenaCenter = center;
            arenaRadius = radius;
            
            Debug.Log($"[MLAgentsAdapter] 아레나 설정 변경: {arenaCenter}, {arenaRadius}");
        }
        
        /// <summary>
        /// 에이전트 수동 설정
        /// </summary>
        public void SetAgents(AgentController bt, AgentController ml)
        {
            btAgent = bt;
            mlAgent = ml;
            isInitialized = (btAgent != null && mlAgent != null);
            
            Debug.Log($"[MLAgentsAdapter] 에이전트 설정: BT={btAgent?.name}, ML={mlAgent?.name}");
        }
        
        /// <summary>
        /// 현재 GameObservation 상태 확인 (디버깅용)
        /// </summary>
        public GameObservation GetCurrentObservation()
        {
            if (btAgent == null || mlAgent == null)
            {
                return default(GameObservation);
            }
            
            return CreateGameObservation();
        }
        
        /// <summary>
        /// BT 에이전트가 아레나 경계에 있는지 확인
        /// </summary>
        public bool IsBTAgentNearBoundary(float threshold = 0.9f)
        {
            if (btAgent == null) return false;
            
            float distance = Vector3.Distance(btAgent.transform.position, arenaCenter);
            return distance > (arenaRadius * threshold);
        }
        
        #endregion
        
        #region Unity Editor 지원
        
        void OnDrawGizmos()
        {
            if (!showArenaGizmos) return;
            
            // 아레나 경계 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
            
            // 안전 구역 표시 (75%)
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(arenaCenter, arenaRadius * 0.75f);
            
            // 위험 구역 표시 (95%)
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(arenaCenter, arenaRadius * 0.95f);
            
            // 에이전트 위치 표시
            if (btAgent != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(btAgent.transform.position, 0.5f);
                
                // 중심까지의 선
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(btAgent.transform.position, arenaCenter);
            }
            
            if (mlAgent != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(mlAgent.transform.position, 0.5f);
            }
        }
        
        #endregion
    }
}

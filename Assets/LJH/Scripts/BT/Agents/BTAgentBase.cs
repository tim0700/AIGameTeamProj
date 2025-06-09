using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT 에이전트의 기본 클래스
    /// ActionTracker를 통합하여 행동 통계를 자동으로 수집
    /// </summary>
    public abstract class BTAgentBase : MonoBehaviour, IBattleAgent
    {
        [Header("BT 에이전트 설정")]
        public string agentName = "BT Agent";
        public string agentType = "BT";

        [Header("데이터 수집 설정")]
        [SerializeField] private bool enableActionTracking = true;
        [SerializeField] private bool showTrackingDebug = true; // 🔧 기본값 true로 변경

        protected BTNode rootNode;
        protected AgentController controller;
        
        // 🆕 행동 추적기
        private ActionTracker actionTracker;
        
        // Public getter for controller
        public AgentController Controller => controller;
        
        // 🆕 ActionTracker 접근자
        public ActionTracker GetActionTracker() => actionTracker;

        public virtual void Initialize(AgentController controller)
        {
            this.controller = controller;
            
            // 🆕 ActionTracker 초기화
            InitializeActionTracker();
            
            BuildBehaviorTree();
            
            if (rootNode != null)
            {
                rootNode.Initialize(controller);
            }
            
            // 🔧 초기화 확인 로그
            Debug.Log($"[{agentName}] BTAgentBase 초기화 완료 - ActionTracker: {(actionTracker != null ? "OK" : "NULL")}, Tracking: {enableActionTracking}");
        }

        /// <summary>
        /// 🆕 ActionTracker 초기화
        /// </summary>
        private void InitializeActionTracker()
        {
            if (!enableActionTracking)
            {
                if (showTrackingDebug)
                    Debug.Log($"[{agentName}] 행동 추적이 비활성화되어 있습니다.");
                return;
            }
            
            // 🔧 강제 초기화 (이미 있어도 새로 만듦)
            actionTracker = new ActionTracker();
            
            if (showTrackingDebug)
                Debug.Log($"[{agentName}] ActionTracker 초기화 완료 - 인스턴스: {actionTracker?.GetHashCode()}");
            
            // 🔧 초기화 후 테스트
            TestActionTracker();
        }
        
        /// <summary>
        /// 🔧 ActionTracker 테스트
        /// </summary>
        private void TestActionTracker()
        {
            if (actionTracker != null)
            {
                // 테스트 데이터 추가
                actionTracker.RecordAttack(true, 1.0f);
                var summary = actionTracker.GetSummary();
                
                if (showTrackingDebug)
                    Debug.Log($"[🧪 {agentName}] ActionTracker 테스트 성공:\n{summary}");
                
                // 테스트 데이터 리셋
                actionTracker.Reset();
            }
            else
            {
                Debug.LogError($"[⚠️ {agentName}] ActionTracker가 여전히 null입니다!");
            }
        }

        public virtual AgentAction DecideAction(GameObservation observation)
        {
            if (rootNode != null)
            {
                NodeState result = rootNode.Evaluate(observation);
                
                // BT 실행 결과에 따른 기본 처리
                switch (result)
                {
                    case NodeState.Success:
                        // BT가 성공적으로 실행됨
                        break;
                    case NodeState.Running:
                        // BT가 계속 실행 중
                        break;
                    case NodeState.Failure:
                        // BT 실행 실패, 기본 행동으로 대체
                        return AgentAction.Idle;
                }
            }

            // BT에서 실제 행동이 실행되므로 Idle 반환
            return AgentAction.Idle;
        }

        public virtual void OnActionResult(ActionResult result)
        {
            // BT 에이전트는 개별 노드에서 행동을 처리하므로 
            // 여기서는 결과를 로깅하거나 학습에 활용할 수 있음
            if (!result.success)
            {
                Debug.LogWarning($"{agentName} 행동 실패: {result.message}");
            }
        }

        public virtual void OnEpisodeEnd(EpisodeResult result)
        {
            if (showTrackingDebug && actionTracker != null)
            {
                Debug.Log($"[{agentName}] 에피소드 종료\n{actionTracker.GetSummary()}");
            }
            
            Debug.Log($"{agentName} 에피소드 종료 - 승리: {result.won}, 최종 HP: {result.finalHP}");
        }

        public string GetAgentName() => agentName;
        public string GetAgentType() => agentType;

        /// <summary>
        /// 🆕 행동 통계 요약 반환
        /// </summary>
        /// <returns>통계 요약 문자열</returns>
        public string GetActionSummary()
        {
            if (actionTracker == null)
                return "ActionTracker가 초기화되지 않았습니다.";
            
            return actionTracker.GetSummary();
        }

        /// <summary>
        /// 🆕 다른 에이전트와 통계 비교
        /// </summary>
        /// <param name="other">비교할 에이전트</param>
        /// <returns>비교 결과</returns>
        public string CompareWith(BTAgentBase other)
        {
            if (actionTracker == null || other?.actionTracker == null)
                return "비교할 수 없습니다. ActionTracker가 초기화되지 않았습니다.";
            
            return actionTracker.CompareTo(other.actionTracker);
        }

        /// <summary>
        /// 🆕 통계 리셋 (새 에피소드 시작 시)
        /// </summary>
        public virtual void ResetActionTracking()
        {
            if (actionTracker != null)
            {
                actionTracker.Reset();
                
                if (showTrackingDebug)
                    Debug.Log($"[{agentName}] ActionTracker 리셋 완료");
            }
        }

        /// <summary>
        /// 🆕 현재 성공률 반환 (UI 표시용)
        /// </summary>
        /// <returns>공격, 방어, 회피 성공률</returns>
        public (float attack, float defense, float dodge) GetSuccessRates()
        {
            if (actionTracker == null)
                return (0f, 0f, 0f);
            
            return (
                actionTracker.AttackSuccessRate,
                actionTracker.DefenseSuccessRate,
                actionTracker.DodgeSuccessRate
            );
        }

        /// <summary>
        /// 🆕 행동 카운트 반환 (UI 표시용)
        /// </summary>
        /// <returns>공격, 방어, 회피 시도/성공 카운트</returns>
        public ActionCounts GetActionCounts()
        {
            if (actionTracker == null)
                return new ActionCounts();
            
            return new ActionCounts
            {
                attackAttempts = actionTracker.AttackAttempts,
                attackSuccesses = actionTracker.AttackSuccesses,
                defenseAttempts = actionTracker.DefenseAttempts,
                defenseSuccesses = actionTracker.DefenseSuccesses,
                dodgeAttempts = actionTracker.DodgeAttempts,
                dodgeSuccesses = actionTracker.DodgeSuccesses,
                totalActions = actionTracker.TotalActions
            };
        }

        /// <summary>
        /// 🆕 설정 업데이트
        /// </summary>
        /// <param name="enableTracking">추적 활성화</param>
        /// <param name="showDebug">디버그 메시지 표시</param>
        public void UpdateTrackingSettings(bool? enableTracking = null, bool? showDebug = null)
        {
            if (enableTracking.HasValue)
            {
                enableActionTracking = enableTracking.Value;
                
                if (enableActionTracking && actionTracker == null)
                {
                    InitializeActionTracker();
                }
            }
            
            if (showDebug.HasValue)
            {
                showTrackingDebug = showDebug.Value;
            }
            
            if (showTrackingDebug)
            {
                Debug.Log($"[{agentName}] 추적 설정 업데이트: Tracking={enableActionTracking}, Debug={showTrackingDebug}");
            }
        }

        /// <summary>
        /// 하위 클래스에서 구현해야 하는 BT 구조 생성 메서드
        /// </summary>
        protected abstract void BuildBehaviorTree();

        #region Unity Inspector 디버그 정보

        [Header("실시간 통계 (읽기 전용)")]
        [SerializeField, ReadOnly] private int debugAttackAttempts;
        [SerializeField, ReadOnly] private int debugAttackSuccesses;
        [SerializeField, ReadOnly] private int debugDefenseAttempts;
        [SerializeField, ReadOnly] private int debugDefenseSuccesses;
        [SerializeField, ReadOnly] private int debugDodgeAttempts;
        [SerializeField, ReadOnly] private int debugDodgeSuccesses;
        [SerializeField, ReadOnly] private float debugAttackSuccessRate;
        [SerializeField, ReadOnly] private float debugDefenseSuccessRate;
        [SerializeField, ReadOnly] private float debugDodgeSuccessRate;

        private void Update()
        {
            // Inspector에서 실시간 통계 표시
            if (actionTracker != null)
            {
                debugAttackAttempts = actionTracker.AttackAttempts;
                debugAttackSuccesses = actionTracker.AttackSuccesses;
                debugDefenseAttempts = actionTracker.DefenseAttempts;
                debugDefenseSuccesses = actionTracker.DefenseSuccesses;
                debugDodgeAttempts = actionTracker.DodgeAttempts;
                debugDodgeSuccesses = actionTracker.DodgeSuccesses;
                debugAttackSuccessRate = actionTracker.AttackSuccessRate;
                debugDefenseSuccessRate = actionTracker.DefenseSuccessRate;
                debugDodgeSuccessRate = actionTracker.DodgeSuccessRate;
            }
        }

        #endregion
    }

    /// <summary>
    /// 🆕 행동 카운트 구조체
    /// </summary>
    [System.Serializable]
    public struct ActionCounts
    {
        public int attackAttempts;
        public int attackSuccesses;
        public int defenseAttempts;
        public int defenseSuccesses;
        public int dodgeAttempts;
        public int dodgeSuccesses;
        public int totalActions;

        public override string ToString()
        {
            return $"공격: {attackSuccesses}/{attackAttempts}, " +
                   $"방어: {defenseSuccesses}/{defenseAttempts}, " +
                   $"회피: {dodgeSuccesses}/{dodgeAttempts}, " +
                   $"총: {totalActions}";
        }
    }

    /// <summary>
    /// 🆕 ReadOnly 속성 (Inspector 표시용)
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}

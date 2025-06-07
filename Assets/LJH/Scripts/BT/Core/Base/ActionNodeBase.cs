using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 액션 노드 특화 베이스 클래스
    /// 공격, 방어, 회피 등 실제 게임 상태를 변경하는 행동을 수행하는 노드들의 기반
    /// </summary>
    public abstract class ActionNodeBase : BTNodeBase, IActionNode
    {
        [Header("액션 노드 설정")]
        [SerializeField] protected ActionType actionType = ActionType.Idle;
        [SerializeField] protected ActionParameters actionParameters;
        [SerializeField] protected bool allowInterruption = true;
        
        // 액션 실행 상태
        protected bool isExecuting = false;
        protected float actionStartTime = 0f;
        protected ActionResult lastActionResult;
        
        // 연계 액션 관리
        protected ActionType lastExecutedAction = ActionType.Idle;
        protected float lastActionTime = 0f;
        
        public ActionNodeBase() : base()
        {
            InitializeActionParameters();
        }
        
        public ActionNodeBase(ActionType type, string name, string description = "") : base(name, description)
        {
            actionType = type;
            InitializeActionParameters();
        }
        
        #region BTNodeBase 오버라이드
        
        protected override NodeState EvaluateNode(GameObservation observation)
        {
            // 1. 실행 가능성 체크
            if (!CanExecute(observation))
            {
                return NodeState.Failure;
            }
            
            // 2. 이미 실행 중인 경우 진행 상태 확인
            if (isExecuting)
            {
                return CheckExecutionProgress(observation);
            }
            
            // 3. 새로운 액션 실행
            ActionResult result = ExecuteAction(observation);
            lastActionResult = result;
            
            if (result.success)
            {
                OnActionStarted(observation);
                return NodeState.Running; // 대부분의 액션은 시간이 걸리므로 Running 반환
            }
            else
            {
                return NodeState.Failure;
            }
        }
        
        protected override string GetNodeType() => "Action";
        
        protected override void InitializeParameters()
        {
            if (actionParameters == null)
            {
                actionParameters = CreateDefaultParameters();
            }
            parameters = actionParameters;
        }
        
        protected override void OnParametersChanged()
        {
            if (parameters is ActionParameters newParams)
            {
                actionParameters = newParams;
                OnActionParametersChanged();
            }
        }
        
        protected override void OnReset()
        {
            isExecuting = false;
            actionStartTime = 0f;
            lastActionResult = default;
        }
        
        #endregion
        
        #region IActionNode 구현
        
        public virtual bool CanExecute(GameObservation observation)
        {
            // 기본 실행 조건들 확인
            if (!IsActive()) return false;
            
            // 쿨다운 체크
            if (!IsCooldownReady(observation))
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 쿨다운이 준비되지 않음");
                return false;
            }
            
            // 범위 체크
            if (!IsInRange(observation))
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 범위를 벗어남. 거리: {observation.distanceToEnemy:F2}, 요구 범위: {actionParameters.range:F2}");
                return false;
            }
            
            // HP 체크
            if (observation.selfHP < actionParameters.hpThreshold)
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] HP 부족. 현재: {observation.selfHP}, 요구: {actionParameters.hpThreshold}");
                return false;
            }
            
            // 커스텀 조건 체크
            return CheckCustomConditions(observation);
        }
        
        public virtual ActionResult ExecuteAction(GameObservation observation)
        {
            if (agentController == null)
            {
                return ActionResult.Failure(actionType, "AgentController가 null입니다.");
            }
            
            // 액션 실행 전 처리
            OnBeforeExecuteAction(observation);
            
            // AgentController를 통한 실제 액션 실행
            AgentAction action = CreateAgentAction(observation);
            ActionResult result = agentController.ExecuteAction(action);
            
            // 실행 결과 처리
            OnAfterExecuteAction(observation, result);
            
            // 연계 액션 정보 업데이트
            if (result.success)
            {
                lastExecutedAction = actionType;
                lastActionTime = Time.time;
                isExecuting = true;
                actionStartTime = Time.time;
            }
            
            return result;
        }
        
        public virtual ActionType GetActionType() => actionType;
        
        public virtual float GetExecutionCost() => actionParameters?.executionCost ?? 0.3f;
        
        public virtual float GetExpectedEffect() => actionParameters?.expectedEffect ?? 1f;
        
        public virtual float GetDuration() => actionParameters?.executionTime ?? 1f;
        
        public virtual bool CanInterrupt() => allowInterruption && actionParameters?.canInterrupt == true;
        
        public virtual bool InterruptAction()
        {
            if (!isExecuting || !CanInterrupt()) return false;
            
            isExecuting = false;
            OnActionInterrupted();
            
            if (enableDetailedLogging)
                Debug.Log($"[{nodeName}] 액션이 중단되었습니다.");
            
            return true;
        }
        
        public virtual bool CanChainWith(ActionType nextActionType)
        {
            if (!actionParameters.allowChaining) return false;
            
            // 기본 연계 규칙
            return IsValidActionChain(actionType, nextActionType);
        }
        
        public virtual string GetExecutionConditions(GameObservation observation)
        {
            var conditions = new System.Text.StringBuilder();
            
            if (!IsCooldownReady(observation))
                conditions.AppendLine($"- 쿨다운 미완료 ({GetCooldownRemaining(observation):F1}s 남음)");
            
            if (!IsInRange(observation))
                conditions.AppendLine($"- 범위 벗어남 (거리: {observation.distanceToEnemy:F1}, 요구: {actionParameters.range:F1})");
            
            if (observation.selfHP < actionParameters.hpThreshold)
                conditions.AppendLine($"- HP 부족 (현재: {observation.selfHP:F1}, 요구: {actionParameters.hpThreshold:F1})");
            
            string customConditions = GetCustomExecutionConditions(observation);
            if (!string.IsNullOrEmpty(customConditions))
                conditions.AppendLine(customConditions);
            
            return conditions.Length > 0 ? conditions.ToString() : "실행 가능";
        }
        
        public virtual void SetActionParameters(float[] parameters)
        {
            if (actionParameters != null)
            {
                actionParameters.FromArray(parameters);
                OnActionParametersChanged();
            }
        }
        
        public virtual float[] GetActionParameters()
        {
            return actionParameters?.ToArray() ?? new float[0];
        }
        
        #endregion
        
        #region 액션 실행 관리
        
        /// <summary>
        /// 실행 중인 액션의 진행 상태 확인
        /// </summary>
        protected virtual NodeState CheckExecutionProgress(GameObservation observation)
        {
            float elapsedTime = Time.time - actionStartTime;
            
            // 실행 시간 초과 확인
            if (elapsedTime >= GetDuration())
            {
                isExecuting = false;
                OnActionCompleted(observation);
                return NodeState.Success;
            }
            
            // 중단 조건 확인
            if (ShouldInterruptExecution(observation))
            {
                InterruptAction();
                return NodeState.Failure;
            }
            
            return NodeState.Running;
        }
        
        /// <summary>
        /// 액션 실행 중단 여부 결정
        /// </summary>
        protected virtual bool ShouldInterruptExecution(GameObservation observation)
        {
            // 기본 중단 조건들
            if (observation.selfHP <= 0) return true;
            if (!IsActive()) return true;
            
            // 커스텀 중단 조건
            return CheckCustomInterruptConditions(observation);
        }
        
        #endregion
        
        #region 조건 체크 메서드들
        
        /// <summary>
        /// 쿨다운 준비 상태 확인
        /// </summary>
        protected virtual bool IsCooldownReady(GameObservation observation)
        {
            switch (actionType)
            {
                case ActionType.Attack:
                    return observation.cooldowns.CanAttack;
                case ActionType.Defend:
                    return observation.cooldowns.CanDefend;
                case ActionType.Dodge:
                    return observation.cooldowns.CanDodge;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 남은 쿨다운 시간 반환
        /// </summary>
        protected virtual float GetCooldownRemaining(GameObservation observation)
        {
            switch (actionType)
            {
                case ActionType.Attack:
                    return observation.cooldowns.attackCooldown;
                case ActionType.Defend:
                    return observation.cooldowns.defendCooldown;
                case ActionType.Dodge:
                    return observation.cooldowns.dodgeCooldown;
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 실행 범위 확인
        /// </summary>
        protected virtual bool IsInRange(GameObservation observation)
        {
            return observation.distanceToEnemy <= actionParameters.range;
        }
        
        /// <summary>
        /// 연계 액션 유효성 확인
        /// </summary>
        protected virtual bool IsValidActionChain(ActionType current, ActionType next)
        {
            // 기본 연계 규칙 (확장 가능)
            switch (current)
            {
                case ActionType.Attack:
                    return next == ActionType.Dodge || next == ActionType.Defend;
                case ActionType.Defend:
                    return next == ActionType.Attack || next == ActionType.Dodge;
                case ActionType.Dodge:
                    return next == ActionType.Attack;
                default:
                    return true;
            }
        }
        
        #endregion
        
        #region 가상 메서드들 (하위 클래스에서 구현)
        
        /// <summary>
        /// 기본 액션 파라미터 생성
        /// </summary>
        protected virtual ActionParameters CreateDefaultParameters()
        {
            var defaultParams = new ActionParameters();
            defaultParams.parameterName = $"{nodeName}_Parameters";
            defaultParams.description = $"{nodeName} 액션 파라미터";
            return defaultParams;
        }
        
        /// <summary>
        /// AgentAction 생성 (하위 클래스에서 구현)
        /// </summary>
        protected abstract AgentAction CreateAgentAction(GameObservation observation);
        
        /// <summary>
        /// 커스텀 실행 조건 확인
        /// </summary>
        protected virtual bool CheckCustomConditions(GameObservation observation) => true;
        
        /// <summary>
        /// 커스텀 중단 조건 확인
        /// </summary>
        protected virtual bool CheckCustomInterruptConditions(GameObservation observation) => false;
        
        /// <summary>
        /// 커스텀 실행 조건 설명
        /// </summary>
        protected virtual string GetCustomExecutionConditions(GameObservation observation) => "";
        
        /// <summary>
        /// 액션 파라미터 변경 시 호출
        /// </summary>
        protected virtual void OnActionParametersChanged() { }
        
        /// <summary>
        /// 액션 실행 전 호출
        /// </summary>
        protected virtual void OnBeforeExecuteAction(GameObservation observation) { }
        
        /// <summary>
        /// 액션 실행 후 호출
        /// </summary>
        protected virtual void OnAfterExecuteAction(GameObservation observation, ActionResult result) { }
        
        /// <summary>
        /// 액션 시작 시 호출
        /// </summary>
        protected virtual void OnActionStarted(GameObservation observation) { }
        
        /// <summary>
        /// 액션 완료 시 호출
        /// </summary>
        protected virtual void OnActionCompleted(GameObservation observation) { }
        
        /// <summary>
        /// 액션 중단 시 호출
        /// </summary>
        protected virtual void OnActionInterrupted() { }
        
        /// <summary>
        /// 액션 파라미터 초기화
        /// </summary>
        protected virtual void InitializeActionParameters()
        {
            if (actionParameters == null)
            {
                actionParameters = CreateDefaultParameters();
            }
        }
        
        #endregion
    }
}

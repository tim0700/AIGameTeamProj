using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 특정 행동의 쿨타임을 확인하는 노드
    /// </summary>
    public class CheckCooldownNode : BTNode
    {
        private ActionType actionType;

        public CheckCooldownNode(ActionType actionType) 
            : base($"CheckCooldown Node ({actionType})")
        {
            this.actionType = actionType;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            bool canExecute = false;
            float remainingCooldown = 0f;

            switch (actionType)
            {
                case ActionType.Attack:
                    canExecute = observation.cooldowns.CanAttack;
                    remainingCooldown = observation.cooldowns.attackCooldown;
                    break;
                case ActionType.Defend:
                    canExecute = observation.cooldowns.CanDefend;
                    remainingCooldown = observation.cooldowns.defendCooldown;
                    break;
                case ActionType.Dodge:
                    canExecute = observation.cooldowns.CanDodge;
                    remainingCooldown = observation.cooldowns.dodgeCooldown;
                    break;
                default:
                    canExecute = true; // 쿨타임이 없는 행동
                    remainingCooldown = 0f;
                    break;
            }

            if (canExecute)
            {
                state = NodeState.Success;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] {actionType} 쿨다운 준비 완료");
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] {actionType} 쿨다운 중. 남은 시간: {remainingCooldown:F2}초");
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// 액션 타입 변경
        /// </summary>
        public void SetActionType(ActionType newActionType)
        {
            actionType = newActionType;
            SetNodeName($"CheckCooldown Node ({actionType})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 액션 타입 변경: {actionType}");
        }
        
        /// <summary>
        /// 현재 액션 타입 반환
        /// </summary>
        public ActionType GetActionType() => actionType;
        
        /// <summary>
        /// 쿨다운 상태 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool IsCooldownReady(GameObservation observation)
        {
            if (!CheckBasicConditions()) return false;
            
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
        public float GetRemainingCooldown(GameObservation observation)
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
        /// 쿨다운 준비 완료까지의 비율 (0.0 ~ 1.0)
        /// </summary>
        public float GetCooldownProgress(GameObservation observation, float maxCooldownTime = 2f)
        {
            float remaining = GetRemainingCooldown(observation);
            if (remaining <= 0f) return 1f;
            
            float elapsed = maxCooldownTime - remaining;
            return Mathf.Clamp01(elapsed / maxCooldownTime);
        }
        
        /// <summary>
        /// 곧 준비될 예정인지 확인 (0.5초 이내)
        /// </summary>
        public bool IsAlmostReady(GameObservation observation, float threshold = 0.5f)
        {
            float remaining = GetRemainingCooldown(observation);
            return remaining <= threshold;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 액션: {actionType}";
        }
        
        #endregion
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 특정 행동의 쿨다운 상태를 확인하는 조건 노드
    /// 각 액션의 사용 가능 여부를 검사하여 행동 실행 전 사전 검증
    /// 
    /// 사용 사례:
    /// - 공격 시퀀스에서 공격 쿨다운 확인
    /// - 방어 전 방어 쿨다운 준비 상태 검사
    /// - 회피 전 회피 쿨다운 사용 가능 여부 확인
    /// - 복합 행동에서 특정 단계의 사전 조건 검사
    /// 
    /// 전략적 가치:
    /// - 자원 낭비 방지 (비효율적인 시도 차단)
    /// - 타이밍 최적화 (쿨다운 완료 시점에 맞는 실행)
    /// - 예측 가능성 (상대방이 쿨다운 예측 가능)
    /// - 코스트 효율성 (연산 비용 극소)
    /// 
    /// 성능 특성:
    /// - 극초 고속 실행 (단순 불리언 체크)
    /// - 메모리 비용 제로
    /// - 예측 가능한 결과
    /// </summary>
    public class CheckCooldownNode : BTNode
    {
        /// <summary>
        /// 검사할 행동 타입
        /// Attack, Defend, Dodge 등의 쿨다운 상태를 확인
        /// </summary>
        private ActionType actionType;

        /// <summary>
        /// 쿨다운 체크 노드 생성자
        /// </summary>
        /// <param name="actionType">검사할 행동 타입</param>
        public CheckCooldownNode(ActionType actionType)
        {
            this.actionType = actionType;
        }

        /// <summary>
        /// 쿨다운 상태 검사 실행
        /// 특정 행동의 사용 가능 여부를 즉시 판단
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>쿨다운 완료시 Success, 아직 대기 중이면 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // 행동 타입에 따른 쿨다운 상태 검사
            switch (actionType)
            {
                case ActionType.Attack:
                    state = observation.cooldowns.CanAttack ? NodeState.Success : NodeState.Failure;
                    LogCooldownStatus("공격", observation.cooldowns.CanAttack, observation.cooldowns.attackCooldown);
                    break;
                    
                case ActionType.Defend:
                    state = observation.cooldowns.CanDefend ? NodeState.Success : NodeState.Failure;
                    LogCooldownStatus("방어", observation.cooldowns.CanDefend, observation.cooldowns.defendCooldown);
                    break;
                    
                case ActionType.Dodge:
                    state = observation.cooldowns.CanDodge ? NodeState.Success : NodeState.Failure;
                    LogCooldownStatus("회피", observation.cooldowns.CanDodge, observation.cooldowns.dodgeCooldown);
                    break;
                    
                default:
                    // 쿨다운이 없는 행동 (이동 등)
                    state = NodeState.Success;
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log($"[쿨다운 체크] {actionType}: 쿨다운 없는 행동 - 항상 사용 가능");
                    }
                    break;
            }

            return state;
        }
        
        /// <summary>
        /// 쿨다운 상태 로깅 (디버깅용)
        /// </summary>
        /// <param name="actionName">행동 이름</param>
        /// <param name="canUse">사용 가능 여부</param>
        /// <param name="remainingTime">남은 쿨다운 시간</param>
        private void LogCooldownStatus(string actionName, bool canUse, float remainingTime)
        {
            // Debug 모드에서만 로깅 (성능 최적화)
            if (Debug.isDebugBuild)
            {
                if (canUse)
                {
                    Debug.Log($"[쿨다운 체크] {actionName}: 사용 가능 ✓");
                }
                else
                {
                    Debug.Log($"[쿨다운 체크] {actionName}: 대기 중 - 남은 시간: {remainingTime:F1}초");
                }
            }
        }
        
        /// <summary>
        /// 현재 검사 중인 행동 타입 반환
        /// </summary>
        /// <returns>설정된 행동 타입</returns>
        public ActionType GetActionType()
        {
            return actionType;
        }
        
        /// <summary>
        /// 검사 중인 행동 타입 변경 (런타임 조정용)
        /// </summary>
        /// <param name="newActionType">새로운 행동 타입</param>
        public void SetActionType(ActionType newActionType)
        {
            actionType = newActionType;
        }
        
        /// <summary>
        /// 쿨다운 상태 미리 확인 (실제 실행 없이)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>쿨다운 완료 여부</returns>
        public bool PreviewCooldownReady(GameObservation observation)
        {
            return actionType switch
            {
                ActionType.Attack => observation.cooldowns.CanAttack,
                ActionType.Defend => observation.cooldowns.CanDefend,
                ActionType.Dodge => observation.cooldowns.CanDodge,
                _ => true // 쿨다운이 없는 행동
            };
        }
        
        /// <summary>
        /// 남은 쿨다운 시간 반환
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>남은 쿨다운 시간 (초), 준비되면 0</returns>
        public float GetRemainingCooldown(GameObservation observation)
        {
            return actionType switch
            {
                ActionType.Attack => observation.cooldowns.attackCooldown,
                ActionType.Defend => observation.cooldowns.defendCooldown,
                ActionType.Dodge => observation.cooldowns.dodgeCooldown,
                _ => 0f // 쿨다운이 없는 행동
            };
        }
        
        /// <summary>
        /// 쿨다운 준비도 계산 (0.0 ~ 1.0)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <param name="maxCooldown">최대 쿨다운 시간</param>
        /// <returns>준비도 (1.0 = 완전 준비, 0.0 = 시작 직후)</returns>
        public float GetCooldownProgress(GameObservation observation, float maxCooldown)
        {
            float remaining = GetRemainingCooldown(observation);
            if (maxCooldown <= 0f) return 1f;
            return Mathf.Clamp01(1f - (remaining / maxCooldown));
        }
        
        /// <summary>
        /// 노드 설정 정보 반환 (디버깅용)
        /// </summary>
        /// <returns>노드 설정 설명</returns>
        public string GetDescription()
        {
            return $"{actionType} 쿨다운 체크 노드";
        }
    }
}

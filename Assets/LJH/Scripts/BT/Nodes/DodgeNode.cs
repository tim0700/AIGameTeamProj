using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 회피 행동을 수행하는 액션 노드
    /// 적의 공격을 피하기 위해 빠르게 회피하는 핵심 생존 액션
    /// 
    /// 실행 조건:
    /// - 회피 쿨다운이 완료되어야 함
    /// - AgentController가 유효해야 함
    /// 
    /// 사용 사례:
    /// - 적 공격 직전 긴급 회피
    /// - 저체력 상태에서 생존을 위한 회피
    /// - 불리한 위치에서 안전한 곳으로 이동
    /// - 공격 후 반격을 피하기 위한 회피
    /// 
    /// 특징:
    /// - 즉시 실행형 노드 (빠른 반응)
    /// - 쿨다운 기반 사용 제한
    /// - 높은 생존 가치
    /// - 위치 재조정 효과
    /// 
    /// 전략적 가치:
    /// - 생존율 증가
    /// - 전투 지속 시간 연장
    /// - 상황 역전 기회 확보
    /// </summary>
    public class DodgeNode : BTNode
    {
        /// <summary>
        /// 회피 로직 실행
        /// 쿨다운 확인 후 즉시 회피 액션 수행
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>회피 성공시 Success, 쿨다운 중이거나 실패시 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // === 1단계: 회피 쿨다운 확인 ===
            if (!observation.cooldowns.CanDodge)
            {
                state = NodeState.Failure;
                return state;
            }

            // === 2단계: 회피 액션 실행 ===
            if (agentController != null)
            {
                // AgentController를 통한 회피 액션 생성 및 실행
                AgentAction dodgeAction = AgentAction.Dodge;
                ActionResult result = agentController.ExecuteAction(dodgeAction);
                
                // 회피 결과 처리
                if (result.success)
                {
                    state = NodeState.Success;
                    Debug.Log($"{agentController.GetAgentName()} 회피 성공! 안전한 위치로 이동");
                }
                else
                {
                    state = NodeState.Failure;
                    Debug.Log($"{agentController.GetAgentName()} 회피 실패: {result.message}");
                }
            }
            else
            {
                // AgentController가 null인 경우 (초기화 오류)
                state = NodeState.Failure;
                Debug.LogError("DodgeNode: AgentController가 null입니다. Initialize()가 호출되었는지 확인하세요.");
            }

            return state;
        }
        
        /// <summary>
        /// 회피 가능 여부 확인 (실제 실행 없이 조건만 검사)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>회피 실행 가능 여부</returns>
        public bool CanExecuteDodge(GameObservation observation)
        {
            return observation.cooldowns.CanDodge && agentController != null;
        }
        
        /// <summary>
        /// 회피의 전략적 가치 평가
        /// 현재 상황에서 회피가 얼마나 유용한지 평가
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>회피 가치 점수 (0.0 ~ 1.0)</returns>
        public float EvaluateDodgeValue(GameObservation observation)
        {
            float value = 0f;
            
            // 체력이 낮을수록 회피 가치 증가
            float healthRatio = observation.selfHP / 100f;
            value += (1f - healthRatio) * 0.4f;
            
            // 적과 가까울수록 회피 가치 증가 (위험도)
            float dangerRatio = Mathf.Clamp01(3f / Mathf.Max(observation.distanceToEnemy, 0.1f));
            value += dangerRatio * 0.3f;
            
            // 쿨다운 사용 가능하면 추가 가치
            if (observation.cooldowns.CanDodge)
                value += 0.3f;
                
            return Mathf.Clamp01(value);
        }
    }
}

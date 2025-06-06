using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 회피 행동을 수행하는 노드
    /// </summary>
    public class DodgeNode : BTNode
    {
        private Vector3 dodgeDirection = Vector3.zero;
        private bool hasCalculatedDirection = false;

        public DodgeNode() : base("Dodge Node")
        {
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 회피 쿨타임 확인
            if (!observation.cooldowns.CanDodge)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 회피 쿨다운 중. 남은 시간: {observation.cooldowns.dodgeCooldown:F2}초");
                
                state = NodeState.Failure;
                return state;
            }

            // 적이 너무 멀리 있으면 회피할 필요 없음
            if (observation.distanceToEnemy > 5f)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 너무 멀리 있어 회피 불필요. 거리: {observation.distanceToEnemy:F2}");
                
                state = NodeState.Failure;
                return state;
            }

            // 스마트 회피 방향 계산 (한번만)
            if (!hasCalculatedDirection)
            {
                dodgeDirection = CalculateSmartDodgeDirection(observation);
                hasCalculatedDirection = true;
            }

            // 회피 실행
            AgentAction dodgeAction = AgentAction.Dodge;
            ActionResult result = agentController.ExecuteAction(dodgeAction);
            
            if (result.success)
            {
                state = NodeState.Success;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 회피 성공! 방향: {dodgeDirection}");
                
                // 회피 후 방향 리셋
                hasCalculatedDirection = false;
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 회피 실패: {result.message}");
                
                // 실패시에도 방향 리셋
                hasCalculatedDirection = false;
            }

            return state;
        }
        
        #region 회피 로직
        
        /// <summary>
        /// 스마트한 회피 방향 계산
        /// </summary>
        private Vector3 CalculateSmartDodgeDirection(GameObservation observation)
        {
            Vector3 toEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
            Vector3 toCenter = (observation.arenaCenter - observation.selfPosition).normalized;
            
            // 적과 수직인 방향들 (좌우)
            Vector3 leftDirection = Vector3.Cross(Vector3.up, toEnemy).normalized;
            Vector3 rightDirection = -leftDirection;
            
            // 아레나 중심과의 거리 고려
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float arenaUsageRatio = distanceFromCenter / observation.arenaRadius;
            
            Vector3 dodgeDirection;
            
            if (arenaUsageRatio > 0.7f) // 아레나 외곽 근처
            {
                // 중심 방향으로 회피
                float leftTowardCenter = Vector3.Dot(leftDirection, toCenter);
                float rightTowardCenter = Vector3.Dot(rightDirection, toCenter);
                
                dodgeDirection = leftTowardCenter > rightTowardCenter ? leftDirection : rightDirection;
                
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 외곽 회피 - 중심 방향으로");
            }
            else // 아레나 내부
            {
                // 랜덤하게 좌우 선택 (예측 불가능)
                dodgeDirection = Random.Range(0f, 1f) > 0.5f ? leftDirection : rightDirection;
                
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 내부 회피 - 랜덤 방향");
            }
            
            // 후진 성분도 약간 추가 (적에게서 멀어지기)
            dodgeDirection = (dodgeDirection * 0.8f + (-toEnemy) * 0.2f).normalized;
            
            return dodgeDirection;
        }
        
        #endregion
        
        #region 추가 기능들
        
        /// <summary>
        /// 회피 가능 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CanDodge(GameObservation observation)
        {
            return CheckBasicConditions() && 
                   observation.cooldowns.CanDodge && 
                   observation.distanceToEnemy <= 5f;
        }
        
        /// <summary>
        /// 남은 회피 쿨다운 시간 반환
        /// </summary>
        public float GetDodgeCooldown(GameObservation observation)
        {
            return observation.cooldowns.dodgeCooldown;
        }
        
        /// <summary>
        /// 회피가 곧 준비될 예정인지 확인
        /// </summary>
        public bool IsDodgeAlmostReady(GameObservation observation, float threshold = 0.5f)
        {
            return GetDodgeCooldown(observation) <= threshold;
        }
        
        /// <summary>
        /// 회피가 필요한 상황인지 판단
        /// </summary>
        public bool ShouldDodge(GameObservation observation)
        {
            // 적이 가까이 있고, 자신의 HP가 위험하거나 적의 공격이 예상될 때
            bool enemyClose = observation.distanceToEnemy <= 3f;
            bool lowHP = observation.selfHP <= 30f;
            bool enemyCanAttack = observation.cooldowns.CanAttack; // 적도 공격 가능한 상태
            
            return enemyClose && (lowHP || enemyCanAttack);
        }
        
        /// <summary>
        /// 현재 계산된 회피 방향 반환
        /// </summary>
        public Vector3 GetDodgeDirection() => dodgeDirection;
        
        /// <summary>
        /// 회피 방향 리셋
        /// </summary>
        public void ResetDodgeDirection()
        {
            hasCalculatedDirection = false;
            dodgeDirection = Vector3.zero;
        }
        
        /// <summary>
        /// 노드 리셋
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            ResetDodgeDirection();
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 방향계산: {hasCalculatedDirection}";
        }
        
        #endregion
    }
}

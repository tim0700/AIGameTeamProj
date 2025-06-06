using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격 행동을 수행하는 노드
    /// </summary>
    public class AttackNode : BTNode
    {
        private float attackRange;

        public AttackNode(float range = 2f) : base("Attack Node")
        {
            this.attackRange = range;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 공격 범위 확인
            if (observation.distanceToEnemy > attackRange)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 공격 범위를 벗어남. 거리: {observation.distanceToEnemy:F2}, 요구 범위: {attackRange:F2}");
                
                state = NodeState.Failure;
                return state;
            }

            // 쿨타임 확인
            if (!observation.cooldowns.CanAttack)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 공격 쿨다운 중. 남은 시간: {observation.cooldowns.attackCooldown:F2}초");
                
                state = NodeState.Failure;
                return state;
            }

            // 🎯 공격 전 적을 향해 즉시 회전
            if (observation.distanceToEnemy <= attackRange)
            {
                Vector3 directionToEnemy = (observation.enemyPosition - observation.selfPosition).normalized;
                if (directionToEnemy.magnitude > 0.1f)
                {
                    // Y축 제거 (평면 회전)
                    directionToEnemy.y = 0;
                    directionToEnemy = directionToEnemy.normalized;
                    
                    // 즉시 회전 (비동기 아님)
                    Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                    agentController.transform.rotation = targetRotation;
                    
                    if (enableLogging)
                        Debug.Log($"[{nodeName}] 공격 전 적 방향 회전 완료");
                }
            }

            // 공격 실행 (AgentController를 통해)
            AgentAction attackAction = AgentAction.Attack;
            ActionResult result = agentController.ExecuteAction(attackAction);
            
            if (result.success)
            {
                state = NodeState.Success;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 공격 성공!");
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 공격 실패: {result.message}");
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// 공격 범위 설정
        /// </summary>
        public void SetAttackRange(float range)
        {
            attackRange = Mathf.Max(0.1f, range);
            if (enableLogging)
                Debug.Log($"[{nodeName}] 공격 범위 변경: {attackRange:F2}");
        }
        
        /// <summary>
        /// 현재 공격 범위 반환
        /// </summary>
        public float GetAttackRange() => attackRange;
        
        /// <summary>
        /// 공격 가능 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CanAttack(GameObservation observation)
        {
            return CheckBasicConditions() && 
                   observation.distanceToEnemy <= attackRange && 
                   observation.cooldowns.CanAttack;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 공격범위: {attackRange:F2}";
        }
        
        #endregion
    }
}

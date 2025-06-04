using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 개선된 공격 노드 (ActionNodeBase 기반)
    /// 기존 AttackNode와 호환성을 유지하면서 고급 기능 추가
    /// </summary>
    public class AttackNodeV2 : ActionNodeBase
    {
        [Header("공격 설정")]
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private bool enableComboAttack = false;
        
        // 기존 생성자와 호환성 유지
        public AttackNodeV2(float range = 2f) : base(ActionType.Attack, "AttackNode", "적을 공격하는 노드")
        {
            // 기존 파라미터를 새로운 시스템으로 변환
            if (actionParameters == null)
            {
                actionParameters = new ActionParameters();
            }
            actionParameters.range = range;
            actionParameters.optimalRange = range * 0.75f;
        }
        
        public AttackNodeV2() : this(2f) { }
        
        #region ActionNodeBase 구현
        
        protected override AgentAction CreateAgentAction(GameObservation observation)
        {
            // 강화된 공격 액션 생성
            AgentAction action = AgentAction.Attack;
            
            // 데미지 배율 적용
            action.intensity = damageMultiplier * actionParameters.intensity;
            
            return action;
        }
        
        protected override bool CheckCustomConditions(GameObservation observation)
        {
            // 추가 공격 조건들
            
            // 적이 너무 가까우면 공격하지 않음 (최소 거리 확인)
            if (observation.distanceToEnemy < 0.5f)
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 적이 너무 가까움. 거리: {observation.distanceToEnemy:F2}");
                return false;
            }
            
            // 적이 죽었으면 공격하지 않음
            if (observation.enemyHP <= 0)
            {
                if (enableDetailedLogging)
                    Debug.Log($"[{nodeName}] 적이 이미 사망함");
                return false;
            }
            
            return true;
        }
        
        protected override string GetCustomExecutionConditions(GameObservation observation)
        {
            var conditions = new System.Text.StringBuilder();
            
            if (observation.distanceToEnemy < 0.5f)
                conditions.AppendLine("- 적이 너무 가까움 (최소 거리 미달)");
            
            if (observation.enemyHP <= 0)
                conditions.AppendLine("- 적이 이미 사망");
            
            return conditions.ToString();
        }
        
        protected override void OnActionStarted(GameObservation observation)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 공격 시작! 거리: {observation.distanceToEnemy:F2}, 적 HP: {observation.enemyHP:F1}");
            }
        }
        
        protected override void OnActionCompleted(GameObservation observation)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 공격 완료! 예상 데미지: {GetExpectedDamage()}");
            }
            
            // 콤보 공격 처리
            if (enableComboAttack && CanStartCombo(observation))
            {
                RequestComboAttack(observation);
            }
        }
        
        protected override void OnActionParametersChanged()
        {
            // 데미지 배율 재계산
            RecalculateDamageMultiplier();
        }
        
        #endregion
        
        #region 공격 특화 기능들
        
        /// <summary>
        /// 예상 데미지 계산
        /// </summary>
        public float GetExpectedDamage()
        {
            return actionParameters.expectedEffect * damageMultiplier;
        }
        
        /// <summary>
        /// 콤보 공격 가능 여부 확인
        /// </summary>
        private bool CanStartCombo(GameObservation observation)
        {
            return enableComboAttack && 
                   observation.cooldowns.attackCooldown < 0.5f && 
                   observation.distanceToEnemy <= actionParameters.optimalRange;
        }
        
        /// <summary>
        /// 콤보 공격 요청
        /// </summary>
        private void RequestComboAttack(GameObservation observation)
        {
            // 콤보 공격 로직 (실제로는 BT 구조에서 처리)
            if (enableDetailedLogging)
            {
                Debug.Log($"[{nodeName}] 콤보 공격 요청!");
            }
        }
        
        /// <summary>
        /// 데미지 배율 재계산
        /// </summary>
        private void RecalculateDamageMultiplier()
        {
            // 파라미터 기반 데미지 배율 조정
            damageMultiplier = actionParameters.intensity;
        }
        
        /// <summary>
        /// 공격 타입별 효과 적용
        /// </summary>
        public void SetAttackVariation(AttackVariation variation)
        {
            switch (variation)
            {
                case AttackVariation.Quick:
                    actionParameters.executionTime = 0.5f;
                    actionParameters.expectedEffect = 0.7f;
                    break;
                case AttackVariation.Heavy:
                    actionParameters.executionTime = 1.5f;
                    actionParameters.expectedEffect = 1.5f;
                    break;
                case AttackVariation.Combo:
                    enableComboAttack = true;
                    actionParameters.allowChaining = true;
                    break;
            }
        }
        
        #endregion
        
        #region 기존 API 호환성
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public void SetAttackRange(float range)
        {
            actionParameters.range = range;
            actionParameters.optimalRange = range * 0.75f;
        }
        
        /// <summary>
        /// 기존 코드와의 호환성을 위한 메서드
        /// </summary>
        public float GetAttackRange()
        {
            return actionParameters.range;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 공격 변형 타입
    /// </summary>
    public enum AttackVariation
    {
        Normal,     // 기본 공격
        Quick,      // 빠른 공격
        Heavy,      // 강한 공격
        Combo       // 콤보 공격
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 방어 행동을 수행하는 노드
    /// </summary>
    public class DefendNode : BTNode
    {
        public DefendNode() : base("Defend Node")
        {
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 방어 쿨타임 확인
            if (!observation.cooldowns.CanDefend)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 방어 쿨다운 중. 남은 시간: {observation.cooldowns.defendCooldown:F2}초");
                
                state = NodeState.Failure;
                return state;
            }

            // 방어 실행
            AgentAction defendAction = AgentAction.Defend;
            ActionResult result = agentController.ExecuteAction(defendAction);
            
            if (result.success)
            {
                state = NodeState.Success;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 방어 성공!");
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] 방어 실패: {result.message}");
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// 방어 가능 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool CanDefend(GameObservation observation)
        {
            return CheckBasicConditions() && observation.cooldowns.CanDefend;
        }
        
        /// <summary>
        /// 남은 방어 쿨다운 시간 반환
        /// </summary>
        public float GetDefendCooldown(GameObservation observation)
        {
            return observation.cooldowns.defendCooldown;
        }
        
        /// <summary>
        /// 방어가 곧 준비될 예정인지 확인
        /// </summary>
        public bool IsDefendAlmostReady(GameObservation observation, float threshold = 0.5f)
        {
            return GetDefendCooldown(observation) <= threshold;
        }
        
        #endregion
    }
}

using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 방어 행동을 수행하는 노드
    /// ActionTracker와 연동하여 방어 시도 및 성공률을 추적
    /// </summary>
    public class DefendNode : BTNode
    {
        public override NodeState Evaluate(GameObservation observation)
        {
            float startTime = Time.realtimeSinceStartup * 1000f; // 밀리초로 변환
            
            // 방어 쿨타임 확인
            if (!observation.cooldowns.CanDefend)
            {
                state = NodeState.Failure;
                // 쿨타임으로 인한 실패는 시도로 카운트하지 않음
                return state;
            }

            // 방어 실행
            if (agentController != null)
            {
                AgentAction defendAction = AgentAction.Defend;
                ActionResult result = agentController.ExecuteAction(defendAction);
                
                if (result.success)
                {
                    state = NodeState.Success;
                    RecordDefenseResult(true, startTime);
                    Debug.Log($"{agentController.GetAgentName()} 방어 성공!");
                }
                else
                {
                    state = NodeState.Failure;
                    RecordDefenseResult(false, startTime);
                    Debug.Log($"{agentController.GetAgentName()} 방어 실패: {result.message}");
                }
            }
            else
            {
                state = NodeState.Failure;
                RecordDefenseResult(false, startTime);
            }

            return state;
        }
        
        /// <summary>
        /// 방어 결과를 ActionTracker에 기록
        /// </summary>
        /// <param name="success">방어 성공 여부</param>
        /// <param name="startTime">시작 시간</param>
        private void RecordDefenseResult(bool success, float startTime)
        {
            if (agentController == null) 
            {
                Debug.LogWarning("[DefendNode] agentController가 null입니다.");
                return;
            }
            
            float executionTime = (Time.realtimeSinceStartup * 1000f) - startTime;
            
            // 🔧 BTAgentBase를 통해 ActionTracker 접근
            if (agentController.TryGetComponent<BTAgentBase>(out var btAgent))
            {
                var actionTracker = btAgent.GetActionTracker();
                if (actionTracker != null)
                {
                    actionTracker.RecordDefense(success, executionTime);
                    Debug.Log($"[📊 DefendNode] {agentController.GetAgentName()} 방어 기록: {success} (ExecutionTime: {executionTime:F2}ms)");
                }
                else
                {
                    Debug.LogWarning($"[⚠️ DefendNode] {agentController.GetAgentName()}의 ActionTracker가 null입니다!");
                }
            }
            else
            {
                Debug.LogWarning($"[⚠️ DefendNode] {agentController.GetAgentName()}에서 BTAgentBase 컬포넌트를 찾을 수 없습니다!");
            }
        }
    }
}

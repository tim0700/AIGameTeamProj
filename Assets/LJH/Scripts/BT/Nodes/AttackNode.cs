using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격 행동을 수행하는 노드
    /// ActionTracker와 연동하여 공격 시도 및 성공률을 추적
    /// </summary>
    public class AttackNode : BTNode
    {
        private float attackRange;

        public AttackNode(float range = 2f)
        {
            this.attackRange = range;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            float startTime = Time.realtimeSinceStartup * 1000f; // 밀리초로 변환
            
            // 공격 범위 확인
            if (observation.distanceToEnemy > attackRange)
            {
                state = NodeState.Failure;
                RecordAttackResult(false, startTime);
                return state;
            }

            // 쿨타임 확인
            if (!observation.cooldowns.CanAttack)
            {
                state = NodeState.Failure;
                // 쿨타임으로 인한 실패는 시도로 카운트하지 않음
                return state;
            }

            // 🏆 강화된 공격 전 적 방향 회전 로직
            if (agentController != null && observation.distanceToEnemy <= attackRange)
            {
                bool rotationSuccess = EnsureRotationToEnemy(observation);
                if (!rotationSuccess)
                {
                    Debug.LogWarning($"[AttackNode] {agentController.GetAgentName()} 적 방향 회전 실패!");
                    state = NodeState.Failure;
                    RecordAttackResult(false, startTime);
                    return state;
                }
            }

            // 공격 실행 (AgentController를 통해)
            if (agentController != null)
            {
                AgentAction attackAction = AgentAction.Attack;
                ActionResult result = agentController.ExecuteAction(attackAction);

                if (result.success)
                {
                    state = NodeState.Success;
                    RecordAttackResult(true, startTime);
                    Debug.Log($"{agentController.GetAgentName()} 공격 성공!");
                }
                else
                {
                    state = NodeState.Failure;
                    RecordAttackResult(false, startTime);
                    Debug.Log($"{agentController.GetAgentName()} 공격 실패: {result.message}");
                }
            }
            else
            {
                state = NodeState.Failure;
                RecordAttackResult(false, startTime);
            }

            return state;
        }
        
        /// <summary>
        /// 공격 결과를 ActionTracker에 기록
        /// </summary>
        /// <param name="success">공격 성공 여부</param>
        /// <param name="startTime">시작 시간</param>
        private void RecordAttackResult(bool success, float startTime)
        {
            if (agentController == null) 
            {
                Debug.LogWarning("[AttackNode] agentController가 null입니다.");
                return;
            }
            
            float executionTime = (Time.realtimeSinceStartup * 1000f) - startTime;
            
            // 🔧 BTAgentBase를 통해 ActionTracker 접근
            if (agentController.TryGetComponent<BTAgentBase>(out var btAgent))
            {
                var actionTracker = btAgent.GetActionTracker();
                if (actionTracker != null)
                {
                    actionTracker.RecordAttack(success, executionTime);
                    Debug.Log($"[📊 AttackNode] {agentController.GetAgentName()} 공격 기록: {success} (ExecutionTime: {executionTime:F2}ms)");
                }
                else
                {
                    Debug.LogWarning($"[⚠️ AttackNode] {agentController.GetAgentName()}의 ActionTracker가 null입니다!");
                }
            }
            else
            {
                Debug.LogWarning($"[⚠️ AttackNode] {agentController.GetAgentName()}에서 BTAgentBase 컬포넌트를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 🏆 적을 향해 확실하게 회전하는 강화된 메서드
        /// </summary>
        /// <param name="observation">게임 관찰 데이터</param>
        /// <returns>회전 성공 여부</returns>
        private bool EnsureRotationToEnemy(GameObservation observation)
        {
            if (agentController == null)
            {
                Debug.LogWarning("[AttackNode] agentController가 null입니다.");
                return false;
            }
            
            // 현재 위치와 적 위치 가져오기
            Vector3 selfPos = agentController.transform.position;
            Vector3 enemyPos = observation.enemyPosition;
            
            // 적과의 거리 재확인
            float distance = Vector3.Distance(selfPos, enemyPos);
            if (distance > attackRange)
            {
                Debug.LogWarning($"[AttackNode] {agentController.GetAgentName()} 적과의 거리가 공격 범위를 초과: {distance:F2} > {attackRange}");
                return false;
            }
            
            // 적 방향 벡터 계산
            Vector3 directionToEnemy = (enemyPos - selfPos);
            
            // Y축 제거 (평면 회전만)
            directionToEnemy.y = 0;
            
            // 방향 벡터 정규화
            if (directionToEnemy.magnitude < 0.01f)
            {
                Debug.LogWarning($"[AttackNode] {agentController.GetAgentName()} 적과의 거리가 너무 가깜워 회전 방향을 결정할 수 없습니다.");
                return false;
            }
            
            directionToEnemy = directionToEnemy.normalized;
            
            // 타겟 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
            Quaternion originalRotation = agentController.transform.rotation;
            
            // 회전 각도 계산
            float rotationAngle = Quaternion.Angle(originalRotation, targetRotation);
            
            // 즉시 회전 실행
            agentController.transform.rotation = targetRotation;
            
            // 회전 검증
            float finalAngle = Quaternion.Angle(agentController.transform.rotation, targetRotation);
            bool rotationSuccess = finalAngle < 1.0f; // 1도 이내 오차
            
            // 디버그 로그
            if (rotationSuccess)
            {
                Debug.Log($"[🎯 AttackNode] {agentController.GetAgentName()} 적 방향 회전 성공! 각도: {rotationAngle:F1}° → 오차: {finalAngle:F2}°");
            }
            else
            {
                Debug.LogWarning($"[⚠️ AttackNode] {agentController.GetAgentName()} 회전 실패! 최종 오차: {finalAngle:F2}°");
            }
            
            // 추가 검증: 적을 제대로 바라보고 있는지 확인
            Vector3 currentForward = agentController.transform.forward;
            currentForward.y = 0;
            currentForward = currentForward.normalized;
            
            float dotProduct = Vector3.Dot(currentForward, directionToEnemy);
            bool facingEnemy = dotProduct > 0.9f; // 코사인 0.9 이상 (약 25도 이내)
            
            if (!facingEnemy)
            {
                Debug.LogWarning($"[⚠️ AttackNode] {agentController.GetAgentName()} 적을 제대로 바라보지 않음! Dot: {dotProduct:F3}");
                return false;
            }
            
            return rotationSuccess && facingEnemy;
        }
    }
}

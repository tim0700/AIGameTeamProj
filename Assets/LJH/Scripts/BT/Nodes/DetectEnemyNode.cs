using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적의 거리를 감지하는 노드
    /// </summary>
    public class DetectEnemyNode : BTNode
    {
        private float detectionRange;

        public DetectEnemyNode(float range) 
            : base($"DetectEnemy Node (Range: {range:F1})")
        {
            this.detectionRange = range;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 적이 죽었으면 감지 실패
            if (observation.enemyHP <= 0)
            {
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 사망하여 감지 실패");
                
                state = NodeState.Failure;
                return state;
            }

            if (observation.distanceToEnemy <= detectionRange)
            {
                state = NodeState.Success;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적 감지됨! 거리: {observation.distanceToEnemy:F2}");
            }
            else
            {
                state = NodeState.Failure;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] 적이 감지 범위를 벗어남. 거리: {observation.distanceToEnemy:F2}, 감지 범위: {detectionRange:F2}");
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// 감지 범위 설정
        /// </summary>
        public void SetDetectionRange(float range)
        {
            detectionRange = Mathf.Max(0.1f, range);
            SetNodeName($"DetectEnemy Node (Range: {detectionRange:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 감지 범위 변경: {detectionRange:F2}");
        }
        
        /// <summary>
        /// 현재 감지 범위 반환
        /// </summary>
        public float GetDetectionRange() => detectionRange;
        
        /// <summary>
        /// 적 감지 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool IsEnemyDetected(GameObservation observation)
        {
            return CheckBasicConditions() && 
                   observation.enemyHP > 0 && 
                   observation.distanceToEnemy <= detectionRange;
        }
        
        /// <summary>
        /// 적이 감지 범위에 진입하고 있는지 확인
        /// </summary>
        public bool IsEnemyApproaching(GameObservation observation, float threshold = 1.2f)
        {
            return observation.distanceToEnemy <= (detectionRange * threshold) && 
                   observation.distanceToEnemy > detectionRange;
        }
        
        /// <summary>
        /// 감지 범위 내 비율 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetDetectionRatio(GameObservation observation)
        {
            if (observation.distanceToEnemy > detectionRange) return 0f;
            
            return 1f - (observation.distanceToEnemy / detectionRange);
        }
        
        /// <summary>
        /// 적과의 방향 벡터 반환
        /// </summary>
        public Vector3 GetDirectionToEnemy(GameObservation observation)
        {
            return (observation.enemyPosition - observation.selfPosition).normalized;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 감지범위: {detectionRange:F2}";
        }
        
        #endregion
    }
}

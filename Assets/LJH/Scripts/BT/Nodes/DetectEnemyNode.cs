using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 적 탐지 조건 노드
    /// 지정된 범위 내에 적이 있는지 감지하는 기본 조건 노드
    /// 
    /// 동작 원리:
    /// - 현재 위치에서 적까지의 직선 거리 계산
    /// - 계산된 거리를 탐지 범위와 비교
    /// - 범위 내에 있으면 Success, 밖에 있으면 Failure
    /// 
    /// 전략적 활용:
    /// - 공격 전 적 범위 내 진입 확인
    /// - 방어/회피 행동의 전제 조건
    /// - 수색 및 추격 전략의 시작점
    /// - 어그로 및 비어그로 전환점
    /// 
    /// 성능 특성:
    /// - 매우 빠른 연산 (단순 거리 비교)
    /// - 커스터마이징 가능한 범위 설정
    /// - 디버깅 정보 풍부
    /// </summary>
    public class DetectEnemyNode : BTNode
    {
        /// <summary>
        /// 적 탐지 가능 최대 범위 (단위: Unity 유닛)
        /// 이 거리 이내에 적이 있어야 탐지 성공
        /// </summary>
        private float detectionRange;

        /// <summary>
        /// 적 탐지 노드 생성자
        /// </summary>
        /// <param name="range">탐지 범위 (단위: Unity 유닛)</param>
        public DetectEnemyNode(float range)
        {
            this.detectionRange = Mathf.Max(0.1f, range); // 최소 범위 보장
        }

        /// <summary>
        /// 적 탐지 로직 실행
        /// 거리 계산 → 범위 비교 → 결과 반환의 단순한 흐름
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>적 탐지 성공시 Success, 범위 밖이면 Failure</returns>
        public override NodeState Evaluate(GameObservation observation)
        {
            // === 탐지 로직: 거리 기반 단순 비교 ===
            if (observation.distanceToEnemy <= detectionRange)
            {
                // 탐지 성공
                state = NodeState.Success;
                
                // 상세 탐지 정보 로깅
                Debug.Log($"[적 탐지] 성공! 거리: {observation.distanceToEnemy:F2} ≤ 범위: {detectionRange:F2}");
                
                // 디버깅용 추가 정보
                LogDetailedDetectionInfo(observation);
            }
            else
            {
                // 탐지 실패 (범위 밖)
                state = NodeState.Failure;
                
                // 디버깅 모드에서만 실패 로깅 (스팦 방지)
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"[적 탐지] 실패. 거리: {observation.distanceToEnemy:F2} > 범위: {detectionRange:F2}");
                }
            }

            return state;
        }
        
        /// <summary>
        /// 상세 탐지 정보 로깅 (디버깅용)
        /// 적의 위치, 이동 예측, 위험도 등 분석 정보 제공
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        private void LogDetailedDetectionInfo(GameObservation observation)
        {
            // 거리 기반 위험도 계산
            float dangerLevel = Mathf.Clamp01((detectionRange - observation.distanceToEnemy) / detectionRange);
            string dangerDesc = GetDangerDescription(dangerLevel);
            
            // 상대 위치 정보
            Vector3 relativePosition = observation.enemyPosition - observation.selfPosition;
            
            Debug.Log($"[탐지 상세] 위험도: {dangerDesc} ({dangerLevel:P0}), 상대위치: {relativePosition}");
        }
        
        /// <summary>
        /// 위험도 설명 문자열 생성
        /// </summary>
        /// <param name="dangerLevel">위험도 (0.0 ~ 1.0)</param>
        /// <returns>위험도 설명</returns>
        private string GetDangerDescription(float dangerLevel)
        {
            if (dangerLevel >= 0.8f) return "매우 위험";
            if (dangerLevel >= 0.6f) return "위험";
            if (dangerLevel >= 0.4f) return "주의";
            if (dangerLevel >= 0.2f) return "경계";
            return "안전";
        }
        
        /// <summary>
        /// 탐지 범위 설정 (런타임 조정용)
        /// </summary>
        /// <param name="range">새로운 탐지 범위</param>
        public void SetDetectionRange(float range)
        {
            detectionRange = Mathf.Max(0.1f, range); // 최소 범위 보장
        }
        
        /// <summary>
        /// 현재 탐지 범위 반환
        /// </summary>
        /// <returns>설정된 탐지 범위</returns>
        public float GetDetectionRange()
        {
            return detectionRange;
        }
        
        /// <summary>
        /// 탐지 가능 여부 미리 확인 (실제 실행 없이)
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>탐지 가능 여부</returns>
        public bool CanDetectEnemy(GameObservation observation)
        {
            return observation.distanceToEnemy <= detectionRange;
        }
        
        /// <summary>
        /// 탐지 비율 계산 (0.0 ~ 1.0)
        /// 탐지 범위 대비 실제 거리 비율
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>탐지 비율 (1.0 = 완전 근접, 0.0 = 범위 밖)</returns>
        public float GetDetectionRatio(GameObservation observation)
        {
            if (observation.distanceToEnemy > detectionRange) return 0f;
            return 1f - (observation.distanceToEnemy / detectionRange);
        }
        
        /// <summary>
        /// 전략적 탐지 가치 평가
        /// 현재 상황에서 적 탐지의 전략적 중요도 계산
        /// </summary>
        /// <param name="observation">현재 게임 상황</param>
        /// <returns>전략적 가치 점수 (0.0 ~ 1.0)</returns>
        public float EvaluateStrategicValue(GameObservation observation)
        {
            float value = 0f;
            
            // 기본 탐지 성공 가치
            if (CanDetectEnemy(observation))
            {
                value += 0.5f;
                
                // 거리가 가까울수록 추가 가치
                value += GetDetectionRatio(observation) * 0.3f;
                
                // 자신의 체력이 낮을수록 적 탐지의 중요성 증가
                float healthRatio = observation.selfHP / 100f;
                value += (1f - healthRatio) * 0.2f;
            }
            
            return Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// 노드 설정 정보 반환 (디버깅용)
        /// </summary>
        /// <returns>노드 설정 설명</returns>
        public string GetDescription()
        {
            return $"적 탐지 범위: {detectionRange:F1} 유닛";
        }
    }
}

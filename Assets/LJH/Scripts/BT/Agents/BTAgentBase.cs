using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// BT(Behavior Tree) 에이전트의 기본 추상 클래스
    /// 모든 BT 기반 AI 에이전트가 상속받아 구현하는 핵심 베이스 클래스
    /// 
    /// 설계 철학:
    /// - IBattleAgent 인터페이스 구현으로 다른 AI 시스템과 호환성 보장
    /// - 템플릿 메서드 패턴으로 BT 구조 생성을 하위 클래스에 위임
    /// - AgentController와의 분리된 아키텍처로 유연성 확보
    /// - 에피소드 기반 학습 시스템 지원
    /// 
    /// 생명주기:
    /// 1. Initialize() - 컨트롤러 연결 및 BT 구조 생성
    /// 2. DecideAction() - 매 프레임 BT 실행 및 행동 결정
    /// 3. OnActionResult() - 행동 결과 피드백 처리
    /// 4. OnEpisodeEnd() - 에피소드 종료 시 학습 데이터 정리
    /// 
    /// 확장 포인트:
    /// - BuildBehaviorTree(): 에이전트별 고유 BT 구조 정의
    /// - OnActionResult(): 결과 기반 학습 및 적응 로직
    /// - OnEpisodeEnd(): 에피소드 단위 성과 분석 및 개선
    /// </summary>
    public abstract class BTAgentBase : MonoBehaviour, IBattleAgent
    {
        [Header("BT 에이전트 기본 설정")]
        [Tooltip("에이전트의 표시 이름 (디버깅 및 UI용)")]
        public string agentName = "BT Agent";
        
        [Tooltip("에이전트 타입 식별자 (통계 및 분류용)")]
        public string agentType = "BT";

        [Header("디버깅 설정")]
        [Tooltip("상세 실행 로그 활성화 여부")]
        public bool enableDetailedLogging = false;
        
        [Tooltip("BT 실행 통계 추적 여부")]
        public bool enablePerformanceTracking = true;

        /// <summary>
        /// BT의 루트 노드
        /// 모든 행동 결정의 시작점이 되는 최상위 노드
        /// </summary>
        protected BTNode rootNode;
        
        /// <summary>
        /// 실제 게임 세계와 상호작용하는 컨트롤러
        /// BT 노드들이 게임 액션을 실행할 때 사용
        /// </summary>
        protected AgentController controller;
        
        /// <summary>
        /// 외부에서 컨트롤러에 접근할 수 있는 읽기 전용 프로퍼티
        /// 디버깅 및 외부 시스템 연동에 활용
        /// </summary>
        public AgentController Controller => controller;
        
        // 성능 추적 변수들
        private int totalEvaluations = 0;
        private int successfulEvaluations = 0;
        private float totalEvaluationTime = 0f;
        private System.Diagnostics.Stopwatch evaluationTimer;

        /// <summary>
        /// BT 에이전트 초기화
        /// 컨트롤러 설정, BT 구조 생성, 노드 초기화를 순차적으로 수행
        /// </summary>
        /// <param name="controller">게임 세계와 상호작용할 컨트롤러</param>
        public virtual void Initialize(AgentController controller)
        {
            // 컨트롤러 연결
            this.controller = controller;
            
            // 성능 추적 초기화
            if (enablePerformanceTracking)
            {
                evaluationTimer = new System.Diagnostics.Stopwatch();
            }
            
            // 하위 클래스에서 정의한 BT 구조 생성
            try
            {
                BuildBehaviorTree();
                
                if (enableDetailedLogging)
                    Debug.Log($"[{agentName}] BT 구조 생성 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{agentName}] BT 구조 생성 실패: {ex.Message}");
                return;
            }
            
            // 루트 노드 및 전체 BT 초기화
            if (rootNode != null)
            {
                rootNode.Initialize(controller);
                
                if (enableDetailedLogging)
                    Debug.Log($"[{agentName}] BT 노드 초기화 완료");
            }
            else
            {
                Debug.LogError($"[{agentName}] 루트 노드가 null입니다. BuildBehaviorTree()에서 rootNode를 설정해주세요.");
            }
        }

        /// <summary>
        /// 매 프레임 행동 결정 로직
        /// BT를 실행하여 현재 상황에 맞는 최적 행동 결정
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>실행할 게임 액션</returns>
        public virtual AgentAction DecideAction(GameObservation observation)
        {
            // 루트 노드가 없으면 기본 행동
            if (rootNode == null)
            {
                if (enableDetailedLogging)
                    Debug.LogWarning($"[{agentName}] 루트 노드가 null이므로 Idle 반환");
                return AgentAction.Idle;
            }

            // 성능 추적 시작
            if (enablePerformanceTracking)
            {
                evaluationTimer.Restart();
            }

            NodeState result = NodeState.Failure;
            try
            {
                // BT 실행
                result = rootNode.Evaluate(observation);
                totalEvaluations++;
                
                if (result == NodeState.Success)
                    successfulEvaluations++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{agentName}] BT 실행 중 오류 발생: {ex.Message}");
                return AgentAction.Idle;
            }
            finally
            {
                // 성능 추적 종료
                if (enablePerformanceTracking)
                {
                    evaluationTimer.Stop();
                    totalEvaluationTime += (float)evaluationTimer.Elapsed.TotalMilliseconds;
                }
            }
            
            // BT 실행 결과에 따른 처리
            if (enableDetailedLogging)
            {
                LogBTEvaluationResult(result, observation);
            }
            
            // BT에서 실제 행동이 노드 내부에서 실행되므로 Idle 반환
            // (BT 패턴의 특성상 DecideAction은 실제 액션 실행이 아닌 의사결정 과정)
            return AgentAction.Idle;
        }
        
        /// <summary>
        /// BT 평가 결과 로깅 (디버깅용)
        /// </summary>
        /// <param name="result">BT 실행 결과</param>
        /// <param name="observation">게임 상황</param>
        private void LogBTEvaluationResult(NodeState result, GameObservation observation)
        {
            string resultDesc = result switch
            {
                NodeState.Success => "성공 - 목표 달성",
                NodeState.Running => "실행중 - 계속 진행",
                NodeState.Failure => "실패 - 조건 미충족",
                _ => "알 수 없는 상태"
            };
            
            Debug.Log($"[{agentName}] BT 실행 결과: {resultDesc} (HP: {observation.selfHP:F1}, 거리: {observation.distanceToEnemy:F2})");
        }

        /// <summary>
        /// 행동 실행 결과 피드백 처리
        /// BT 노드들이 실행한 액션의 결과를 받아 학습이나 적응에 활용
        /// </summary>
        /// <param name="result">행동 실행 결과</param>
        public virtual void OnActionResult(ActionResult result)
        {
            // 기본 구현: 실패한 행동에 대해서만 경고 로그
            if (!result.success)
            {
                Debug.LogWarning($"[{agentName}] 행동 실패 - 타입: {result.actionType}, 메시지: {result.message}");
            }
            else if (enableDetailedLogging)
            {
                Debug.Log($"[{agentName}] 행동 성공 - 타입: {result.actionType}");
            }
            
            // 하위 클래스에서 오버라이드하여 추가 처리 가능
            // 예: 성공률 추적, 적응적 파라미터 조정, 학습 데이터 수집 등
        }

        /// <summary>
        /// 에피소드 종료 시 호출되는 콜백
        /// 한 라운드의 전투가 끝났을 때 성과 분석 및 학습 데이터 정리
        /// </summary>
        /// <param name="result">에피소드 결과</param>
        public virtual void OnEpisodeEnd(EpisodeResult result)
        {
            // 에피소드 결과 로깅
            string resultMsg = result.won ? "승리" : "패배";
            Debug.Log($"[{agentName}] 에피소드 종료 - {resultMsg}, 최종 HP: {result.finalHP:F1}");
            
            // 성능 통계 로깅
            if (enablePerformanceTracking && totalEvaluations > 0)
            {
                float successRate = (float)successfulEvaluations / totalEvaluations * 100f;
                float avgEvaluationTime = totalEvaluationTime / totalEvaluations;
                
                Debug.Log($"[{agentName}] 성능 통계 - 성공률: {successRate:F1}%, 평균 평가시간: {avgEvaluationTime:F2}ms, 총 평가: {totalEvaluations}회");
                
                // 통계 리셋 (다음 에피소드 준비)
                ResetPerformanceStats();
            }
        }
        
        /// <summary>
        /// 성능 통계 리셋
        /// </summary>
        private void ResetPerformanceStats()
        {
            totalEvaluations = 0;
            successfulEvaluations = 0;
            totalEvaluationTime = 0f;
        }

        /// <summary>
        /// 에이전트 이름 반환 (IBattleAgent 인터페이스 구현)
        /// </summary>
        /// <returns>에이전트 표시 이름</returns>
        public string GetAgentName() => agentName;
        
        /// <summary>
        /// 에이전트 타입 반환 (IBattleAgent 인터페이스 구현)
        /// </summary>
        /// <returns>에이전트 타입 식별자</returns>
        public string GetAgentType() => agentType;

        /// <summary>
        /// 하위 클래스에서 구현해야 하는 BT 구조 생성 메서드
        /// 각 에이전트의 고유한 행동 패턴과 전략을 정의
        /// 
        /// 구현 가이드라인:
        /// 1. rootNode에 BT의 최상위 노드 할당
        /// 2. 노드들을 계층적으로 구성하여 복잡한 행동 패턴 구현
        /// 3. 에이전트의 특성에 맞는 우선순위와 조건 설정
        /// 4. 예외 상황과 fallback 행동 고려
        /// </summary>
        protected abstract void BuildBehaviorTree();
        
        /// <summary>
        /// 현재 BT 실행 통계 반환 (디버깅 및 모니터링용)
        /// </summary>
        /// <returns>BT 성능 통계 정보</returns>
        public BTPerformanceStats GetPerformanceStats()
        {
            return new BTPerformanceStats
            {
                totalEvaluations = this.totalEvaluations,
                successfulEvaluations = this.successfulEvaluations,
                successRate = totalEvaluations > 0 ? (float)successfulEvaluations / totalEvaluations : 0f,
                averageEvaluationTime = totalEvaluations > 0 ? totalEvaluationTime / totalEvaluations : 0f
            };
        }
    }
    
    /// <summary>
    /// BT 성능 통계 구조체
    /// </summary>
    [System.Serializable]
    public struct BTPerformanceStats
    {
        public int totalEvaluations;        // 총 평가 횟수
        public int successfulEvaluations;   // 성공한 평가 횟수
        public float successRate;           // 성공률 (0.0 ~ 1.0)
        public float averageEvaluationTime; // 평균 평가 시간 (ms)
        
        public override string ToString()
        {
            return $"평가: {totalEvaluations}회, 성공률: {successRate:P1}, 평균시간: {averageEvaluationTime:F2}ms";
        }
    }
}

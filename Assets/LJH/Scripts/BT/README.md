# Behavior Tree (BT) 아키텍처 문서

## 개요

이 프로젝트의 BT 시스템은 계층적 구조로 설계된 고급 Behavior Tree 아키텍처입니다. 전통적인 BT 패턴을 기반으로 하되, ML 최적화, 성능 분석, 파라미터 조정 등의 고급 기능을 추가로 제공합니다.

## 아키텍처 구조

### 1. Core System (핵심 시스템)
- **BTNode.cs**: 모든 BT 노드의 기본 클래스
- **SelectorNode.cs**: OR 논리의 컴포지트 노드 (첫 번째 성공까지 시도)
- **SequenceNode.cs**: AND 논리의 컴포지트 노드 (모든 자식 성공 필요)
- **RandomSelectorNode.cs**: 랜덤 순서 실행 셀렉터 (예측 불가능성 제공)

### 2. Base Classes (특화 베이스 클래스)
- **BTNodeBase.cs**: 확장된 기능의 노드 베이스 (메타데이터, 캐싱, 성능 분석)
- **ActionNodeBase.cs**: 액션 노드 특화 베이스 (공격, 방어, 회피 등)
- **ConditionNodeBase.cs**: 조건 노드 특화 베이스 (HP 체크, 쿨다운 체크 등)
- **MovementNodeBase.cs**: 이동 노드 특화 베이스 (경로 계획, 장애물 회피)

### 3. Interfaces (인터페이스 시스템)
- **IBTNode.cs**: 기본 노드 인터페이스
- **IActionNode.cs**: 액션 노드 인터페이스
- **IConditionNode.cs**: 조건 노드 인터페이스
- **IMovementNode.cs**: 이동 노드 인터페이스
- **NodeMetadata.cs**: 노드 메타데이터 구조체

### 4. Parameters (파라미터 시스템)
ML 최적화를 위한 런타임 파라미터 조정 시스템:
- **INodeParameters.cs**: 파라미터 인터페이스
- **ActionParameters.cs**: 액션 파라미터 (범위, 강도, 쿨다운 등)
- **ConditionParameters.cs**: 조건 파라미터 (임계값, 비교 방식 등)
- **MovementParameters.cs**: 이동 파라미터 (속도, 경로 찾기 방식 등)

### 5. Concrete Nodes (실제 노드 구현체)

#### 액션 노드들
- **AttackNode.cs**: 공격 행동 (자동 조준, 범위 검사)
- **DefendNode.cs**: 방어 행동 (쿨다운 관리)
- **DodgeNode.cs**: 회피 행동 (즉시 실행)

#### 조건 노드들
- **CheckHPNode.cs**: HP 상태 검사 (자신/적, 임계값 설정)
- **CheckCooldownNode.cs**: 쿨다운 상태 검사
- **DetectEnemyNode.cs**: 적 탐지 (거리 기반)
- **CheckArenaBoundaryNode.cs**: 아레나 경계 검사 (3단계 경고)
- **CriticalBoundaryCheckNode.cs**: 극한 상황 경계 검사

#### 이동 노드들
- **MoveToEnemyNode.cs**: 적에게 이동 (4방향 최적화)
- **MaintainDistanceNode.cs**: 거리 유지 (스마트 측면 이동)
- **ReturnToArenaNodeBT.cs**: 아레나 복귀 (안전 지역으로)
- **SafePatrolNode.cs**: 안전 순찰 (아레나 경계 고려)

### 6. Agents (BT 에이전트들)
- **BTAgentBase.cs**: BT 에이전트 기본 클래스
- **AggressiveBTAgent.cs**: 공격형 에이전트 (체력 기반 적응 전략)
- **DefensiveBTAgent.cs**: 방어형 에이전트 (균형 잡힌 전략)
- **RandomAggressiveBTAgent.cs**: 랜덤 공격형 (예측 불가능성)

### 7. Example Nodes (고급 예제 노드들)
- **AttackNodeV2_Example.cs**: 개선된 공격 노드 (콤보, 적응형 데미지)
- **CheckHPNodeV2_Example.cs**: 개선된 HP 체크 (응급 모드, 트렌드 분석)
- **MoveToEnemyNodeV2_Example.cs**: 개선된 적 추적 (예측 이동, 플랭킹)

## 주요 특징

### 1. 계층적 아키텍처
- 기본 BTNode에서 시작하여 특화된 베이스 클래스로 확장
- 각 노드 타입별 최적화된 기능 제공
- 코드 재사용성과 유지보수성 극대화

### 2. ML 최적화 지원
- 런타임 파라미터 조정 시스템
- 노드 성능 메트릭 수집
- 적응적 임계값 조정
- 성공률 기반 최적화

### 3. 고급 이동 시스템
- 아레나 경계 인식
- 장애물 회피
- 예측 기반 이동
- 전술적 포지셔닝

### 4. 안전 시스템
- 다단계 경계 검사
- 자동 복귀 메커니즘
- 안전 순찰 패턴
- 극한 상황 대응

### 5. 전략적 다양성
- 체력 기반 전략 전환
- 랜덤 행동 패턴
- 우선순위 기반 행동 선택
- 적응적 반응 시스템

## 사용법

### 기본 BT 노드 생성
```csharp
// 간단한 공격 노드
var attackNode = new AttackNode(2f); // 2유닛 범위

// 체력 체크 노드
var lowHPCheck = new CheckHPNode(30f, true, false); // 자신 HP 30 이하

// 시퀀스 노드 (공격 체인)
var attackSequence = new SequenceNode(new List<BTNode> {
    new DetectEnemyNode(3f),
    new CheckCooldownNode(ActionType.Attack),
    new MoveToEnemyNode(2f),
    new AttackNode(2f)
});
```

### 에이전트 생성
```csharp
// 공격형 에이전트
var aggressiveAgent = GetComponent<AggressiveBTAgent>();
aggressiveAgent.attackRange = 2.5f;
aggressiveAgent.hpThreshold = 40f;

// 에이전트 초기화
aggressiveAgent.Initialize(agentController);
```

### 고급 노드 사용
```csharp
// 파라미터 기반 노드
var advancedAttack = new AttackNodeV2(2f);
advancedAttack.SetAttackVariation(AttackVariation.Combo);

// 예측 이동 노드
var smartMove = new MoveToEnemyNodeV2(2f);
smartMove.SetPredictiveMovement(true);
smartMove.SetFlankingMovement(true);
```

## 성능 고려사항

### 1. 노드 실행 순서
- 빠른 조건 검사를 먼저 배치
- 비용이 높은 액션을 나중에 배치
- 실패 확률이 높은 조건을 앞쪽에 배치

### 2. 캐싱 활용
- 동일한 프레임 내 중복 계산 방지
- 조건 노드의 결과 캐싱
- 경로 계산 결과 재사용

### 3. 메모리 관리
- 노드 풀링 고려
- 대용량 데이터 구조 주의
- 메타데이터 수집 최적화

## 확장 가이드

### 새로운 노드 타입 추가
1. 적절한 베이스 클래스 선택 (ActionNodeBase, ConditionNodeBase, MovementNodeBase)
2. 필요한 인터페이스 구현
3. 파라미터 클래스 정의
4. 메타데이터 설정

### 새로운 에이전트 타입 추가
1. BTAgentBase 상속
2. BuildBehaviorTree() 메서드 구현
3. 필요한 파라미터 노출
4. 전략별 노드 조합

## 디버깅 및 모니터링

### 로깅 시스템
- 노드별 상세 실행 로그
- 성능 메트릭 추적
- 실행 시간 측정
- 성공/실패율 모니터링

### 시각적 디버깅
- Scene View에서 경계선 표시
- 이동 경로 시각화
- 타겟 위치 표시
- 상태 정보 오버레이

## 향후 개발 방향

1. **더 많은 노드 타입 추가**: 치료, 버프, 특수 능력 등
2. **고급 AI 패턴**: 협력 행동, 팀 전략, 적응 학습
3. **성능 최적화**: 멀티스레딩, Job System 활용
4. **시각적 에디터**: 그래픽 BT 편집기 개발
5. **ML 통합**: 강화학습과의 하이브리드 시스템

이 BT 시스템은 게임 AI의 복잡성과 성능 요구사항을 균형있게 충족하도록 설계되었으며, 확장 가능하고 유지보수가 용이한 구조를 제공합니다.

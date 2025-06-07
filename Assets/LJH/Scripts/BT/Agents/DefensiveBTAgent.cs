using System.Collections.Generic;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 방어형 BT 에이전트 - 체력 기반 적응적 방어 전략
    /// </summary>
    public class DefensiveBTAgent : BTAgentBase
    {
        [Header("방어형 설정")]
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public float preferredDistance = 4f;
        public float hpThreshold = 40f; // 체력 기반 패턴 변경 기준

        /// <summary>
        /// 방어형 BT 구조 생성
        /// 체력 기반 적응적 방어 전략을 사용하는 안정적 AI
        /// 
        /// 전략 개요:
        /// 1. 안전 시스템: 아레나 경계 안전 (최우선)
        /// 2. 고체력 패턴 (40% 초과): 균형 잡힌 랜덤 전투
        /// 3. 저체력 패턴 (40% 이하): 방어 우선 생존 전략
        /// 4. 기본 순찰: 신중한 안전 순찰
        /// 
        /// 특징:
        /// - 체력 40% 기준으로 전략 전환
        /// - 고체력시 균형 잡힌 여러 옵션 랜덤 선택
        /// - 저체력시 방어/회피 우선 순서
        /// - 전반적으로 안전성을 중시하는 전략
        /// 
        /// 대전 상성:
        /// - vs 공격형: 거리 유지 및 비타이밍 전략
        /// - vs 방어형: 지구전 및 안정성 대결
        /// - vs 랜덤형: 예측 가능한 패턴으로 카운터
        /// </summary>
        protected override void BuildBehaviorTree()
        {
            agentName = "방어형 BT 에이전트";
            agentType = "BT-Defensive";

            // === 공통 시퀀스들 정의 ===
            
            // 1. 안전 시스템 (극한 상황에서만) - 아레나 경계
            var criticalBoundaryCheck = new CriticalBoundaryCheckNode(0.95f); // 95% 이상에서만 작동
            var returnToArena = new ReturnToArenaNodeBT(0.7f, 1.2f);
            var safetySequence = new SequenceNode(new List<BTNode> 
            { 
                criticalBoundaryCheck, 
                returnToArena 
            });

            // 2. 공격 시퀀스 (공통)
            var attackSequence = new SequenceNode(new List<BTNode> 
            { 
                new DetectEnemyNode(attackRange + 1f),
                new CheckCooldownNode(ActionType.Attack),
                new MoveToEnemyNode(attackRange),
                new AttackNode(attackRange)
            });

            // 3. 방어 시퀀스 (공통)
            var defenseSequence = new SequenceNode(new List<BTNode> 
            { 
                new DetectEnemyNode(attackRange + 1f),
                new CheckCooldownNode(ActionType.Defend),
                new DefendNode()
            });

            // 4. 회피 시퀀스 (공통)
            var dodgeSequence = new SequenceNode(new List<BTNode> 
            { 
                new DetectEnemyNode(attackRange),
                new CheckCooldownNode(ActionType.Dodge),
                new DodgeNode()
            });

            // 5. 거리 유지 시퀀스
            var maintainDistanceSequence = new SequenceNode(new List<BTNode>
            {
                new DetectEnemyNode(preferredDistance),
                new MaintainDistanceNode(preferredDistance, 1f)
            });

            // 6. 안전 순찰
            var safePatrol = new SafePatrolNode(2f, 0.7f); // 방어형은 더 신중하게

            // === 체력 기반 패턴 ===

            // 고체력 패턴 (HP > 40%): 균형 잡힌 랜덤 전투
            var highHPPattern = new SequenceNode(new List<BTNode>
            {
                new CheckHPNode(hpThreshold, true, true), // HP 40% 초과 확인 (inverted=true)
                new RandomSelectorNode(new List<BTNode>
                {
                    attackSequence,           // 공격 (33%)
                    defenseSequence,          // 방어 (33%)
                    maintainDistanceSequence  // 거리 유지 (33%)
                })
            });

            // 저체력 패턴 (HP ≤ 40%): 방어 우선 생존 전략
            var lowHPPattern = new SequenceNode(new List<BTNode>
            {
                new CheckHPNode(hpThreshold, true, false), // HP 40% 이하 확인 (inverted=false)
                new SelectorNode(new List<BTNode>
                {
                    defenseSequence,    // 방어 최우선
                    dodgeSequence,      // 회피 우선  
                    attackSequence     // 공격 기회
                })
            });

            // === 최종 BT 구조 ===
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                safetySequence,    // 아레나 안전 (최우선)
                highHPPattern,     // 고체력: 균형 랜덤 전투
                lowHPPattern,      // 저체력: 방어 우선 생존
                safePatrol        // 🆕 기본 순찰 (fallback - 모든 조건 실패시)
            });

            Debug.Log($"{agentName} BT 구조 생성 완료 - 체력 기반 패턴 ({hpThreshold}% 기준) + 기본 순찰");
        }

        public override void OnActionResult(ActionResult result)
        {
            base.OnActionResult(result);
            
            switch (result.actionType)
            {
                case ActionType.Defend:
                    if (result.success)
                        Debug.Log($"{agentName} 방어 성공!");
                    break;
                case ActionType.Dodge:
                    if (result.success)
                        Debug.Log($"{agentName} 회피 성공!");
                    break;
                case ActionType.Attack:
                    if (result.success)
                        Debug.Log($"{agentName} 공격 성공! 데미지: {result.damage}");
                    break;
            }
        }
    }
}

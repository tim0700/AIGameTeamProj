using System.Collections.Generic;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격형 BT 에이전트 - 체력 기반 적응적 공격 전략
    /// </summary>
    public class AggressiveBTAgent : BTAgentBase
    {
        [Header("공격형 설정")]
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public float hpThreshold = 40f; // 체력 기반 패턴 변경 기준

        protected override void BuildBehaviorTree()
        {
            agentName = "공격형 BT 에이전트";
            agentType = "BT-Aggressive";

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

            // 5. 접근 시퀀스 (공격 불가시)
            var approachSequence = new SequenceNode(new List<BTNode> 
            { 
                new DetectEnemyNode(detectionRange),
                new MoveToEnemyNode(attackRange - 0.5f)
            });

            // 6. 안전 순찰
            var safePatrol = new SafePatrolNode(3f, 0.75f);

            // === 체력 기반 패턴 ===

            // 고체력 패턴 (HP > 40%): 공격 우선
            var highHPPattern = new SequenceNode(new List<BTNode>
            {
                new CheckHPNode(hpThreshold, true, true), // HP 40% 초과 확인 (inverted=true)
                new SelectorNode(new List<BTNode>
                {
                    attackSequence,     // 공격 우선
                    approachSequence   // 접근 (공격 불가시)
                })
            });

            // 저체력 패턴 (HP ≤ 40%): 랜덤 생존 전략
            var lowHPPattern = new SequenceNode(new List<BTNode>
            {
                new CheckHPNode(hpThreshold, true, false), // HP 40% 이하 확인 (inverted=false)
                new RandomSelectorNode(new List<BTNode>
                {
                    defenseSequence,    // 방어 (33%)
                    dodgeSequence,      // 회피 (33%)
                    attackSequence      // 공격 (33%)
                })
            });

            // === 최종 BT 구조 ===
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                safetySequence,    // 아레나 안전 (최우선)
                highHPPattern,     // 고체력: 공격 우선
                lowHPPattern,      // 저체력: 랜덤 생존 전략
                safePatrol        // 🆕 기본 순찰 (fallback - 모든 조건 실패시)
            });

            Debug.Log($"{agentName} BT 구조 생성 완료 - 체력 기반 패턴 ({hpThreshold}% 기준) + 기본 순찰");
        }

        public override void OnActionResult(ActionResult result)
        {
            base.OnActionResult(result);
            
            if (result.success && result.actionType == ActionType.Attack)
            {
                Debug.Log($"{agentName} 공격 성공! 데미지: {result.damage}");
            }
        }
    }
}

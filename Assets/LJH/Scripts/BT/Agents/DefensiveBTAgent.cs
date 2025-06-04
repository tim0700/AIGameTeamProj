using System.Collections.Generic;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 수비형 BT 에이전트 - 방어 위주의 신중한 전략
    /// </summary>
    public class DefensiveBTAgent : BTAgentBase
    {
        [Header("수비형 설정")]
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public float preferredDistance = 4f;
        public float counterAttackHPThreshold = 70f;

        protected override void BuildBehaviorTree()
        {
            agentName = "수비형 BT 에이전트";
            agentType = "BT-Defensive";

            // 1. 안전 시스템 (최우선) - 아레나 경계 감지 및 복귀
            var boundaryCheck = new CheckArenaBoundaryNode(0.8f, 0.9f, 0.95f); // 80%, 90%, 95% 경계
            var returnToArena = new ReturnToArenaNodeBT(0.7f, 1.2f); // 70% 안전 지역, 1.2배 속도
            var safetySequence = new SequenceNode(new List<BTNode> 
            { 
                boundaryCheck, 
                returnToArena 
            });

            // 2. 적이 가까이 있을 때 방어 행동
            var enemyClose = new DetectEnemyNode(attackRange + 1f);
            var checkDefendCooldown = new CheckCooldownNode(ActionType.Defend);
            var defend = new DefendNode();
            
            var defendSequence = new SequenceNode(new List<BTNode> 
            { 
                enemyClose, 
                checkDefendCooldown, 
                defend 
            });

            // 3. 적이 너무 가까우면 회피
            var enemyTooClose = new DetectEnemyNode(attackRange);
            var checkDodgeCooldown = new CheckCooldownNode(ActionType.Dodge);
            var dodge = new DodgeNode();
            
            var dodgeSequence = new SequenceNode(new List<BTNode> 
            { 
                enemyTooClose, 
                checkDodgeCooldown, 
                dodge 
            });

            // 4. 반격 기회 포착 (상대 HP가 낮거나 자신의 HP가 높을 때)
            var checkSelfHP = new CheckHPNode(counterAttackHPThreshold, true); // 자신 HP 70% 이상
            var checkAttackCooldown = new CheckCooldownNode(ActionType.Attack);
            var moveToAttack = new MoveToEnemyNode(attackRange);
            var attack = new AttackNode(attackRange);
            
            // CheckHPNode는 threshold 이하일 때 Success를 반환하므로, 
            // 반전 로직이 필요. 여기서는 단순화하여 적 HP가 낮을 때만 공격
            var enemyLowHP = new CheckHPNode(40f, false); // 적 HP 40% 이하
            
            var counterAttackSequence = new SequenceNode(new List<BTNode> 
            { 
                enemyLowHP,
                checkAttackCooldown, 
                moveToAttack, 
                attack 
            });

            // 5. 거리 유지 (수비형의 핵심 전략)
            var maintainDistance = new MaintainDistanceNode(preferredDistance, 1f);

            // 6. 안전 순찰 (아레나 경계 고려)
            var patrol = new SafePatrolNode(2f, 0.7f); // 순찰 반경 2f, 아레나의 70% 내에서만 (수비형은 더 신중)

            // 최종 트리 구성: 안전 > 회피 > 방어 > 반격 > 거리유지 > 순찰
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                safetySequence,       // 🆕 최우선 안전 시스템
                dodgeSequence,        // 기존 회피
                defendSequence,       // 기존 방어
                counterAttackSequence, // 기존 반격
                maintainDistance,     // 기존 거리 유지
                patrol                // 기존 순찰
            });

            Debug.Log($"{agentName} BT 구조 생성 완료 (안전 시스템 포함)");
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
                        Debug.Log($"{agentName} 반격 성공! 데미지: {result.damage}");
                    break;
            }
        }
    }
}

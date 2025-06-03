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

            // 1. 적이 가까이 있을 때 방어 행동
            var enemyClose = new DetectEnemyNode(attackRange + 1f);
            var checkDefendCooldown = new CheckCooldownNode(ActionType.Defend);
            var defend = new DefendNode();
            
            var defendSequence = new SequenceNode(new List<BTNode> 
            { 
                enemyClose, 
                checkDefendCooldown, 
                defend 
            });

            // 2. 적이 너무 가까우면 회피
            var enemyTooClose = new DetectEnemyNode(attackRange);
            var checkDodgeCooldown = new CheckCooldownNode(ActionType.Dodge);
            var dodge = new DodgeNode();
            
            var dodgeSequence = new SequenceNode(new List<BTNode> 
            { 
                enemyTooClose, 
                checkDodgeCooldown, 
                dodge 
            });

            // 3. 반격 기회 포착 (상대 HP가 낮거나 자신의 HP가 높을 때)
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

            // 4. 거리 유지 (수비형의 핵심 전략)
            var maintainDistance = new MaintainDistanceNode(preferredDistance, 1f);

            // 5. 기본 순찰
            var patrol = new PatrolNode(2f);

            // 최종 트리 구성: 회피 > 방어 > 반격 > 거리유지 > 순찰
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                dodgeSequence,
                defendSequence, 
                counterAttackSequence,
                maintainDistance, 
                patrol 
            });

            Debug.Log($"{agentName} BT 구조 생성 완료");
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

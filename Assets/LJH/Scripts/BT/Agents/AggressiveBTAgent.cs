using System.Collections.Generic;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 공격형 BT 에이전트 - 적극적인 공격 전략
    /// </summary>
    public class AggressiveBTAgent : BTAgentBase
    {
        [Header("공격형 설정")]
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public float emergencyHPThreshold = 30f;

        protected override void BuildBehaviorTree()
        {
            agentName = "공격형 BT 에이전트";
            agentType = "BT-Aggressive";

            // 1. 응급 상황 처리 (HP 30% 이하일 때 회피)
            var emergencyHPCheck = new CheckHPNode(emergencyHPThreshold, true); // 자신의 HP 확인
            var emergencyDodge = new DodgeNode();
            var emergencySequence = new SequenceNode(new List<BTNode> 
            { 
                emergencyHPCheck, 
                emergencyDodge 
            });

            // 2. 공격 시퀀스
            var detectEnemy = new DetectEnemyNode(detectionRange);
            var checkAttackCooldown = new CheckCooldownNode(ActionType.Attack);
            var moveToEnemy = new MoveToEnemyNode(attackRange);
            var attack = new AttackNode(attackRange);

            var attackSequence = new SequenceNode(new List<BTNode> 
            { 
                detectEnemy, 
                checkAttackCooldown, 
                moveToEnemy, 
                attack 
            });

            // 3. 이동 및 접근 (공격이 불가능할 때)
            var moveToEnemyAlternative = new SequenceNode(new List<BTNode> 
            { 
                detectEnemy, 
                new MoveToEnemyNode(attackRange - 0.5f) 
            });

            // 4. 기본 순찰
            var patrol = new PatrolNode(3f);

            // 최종 트리 구성: 응급상황 > 공격 > 접근 > 순찰
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                emergencySequence,
                attackSequence, 
                moveToEnemyAlternative, 
                patrol 
            });

            Debug.Log($"{agentName} BT 구조 생성 완료");
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

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

            // 1. 안전 시스템 (최우선) - 아레나 경계 감지 및 복귀
            var boundaryCheck = new CheckArenaBoundaryNode(0.8f, 0.9f, 0.95f); // 80%, 90%, 95% 경계
            var returnToArena = new ReturnToArenaNodeBT(0.7f, 1.2f); // 70% 안전 지역, 1.2배 속도
            var safetySequence = new SequenceNode(new List<BTNode> 
            { 
                boundaryCheck, 
                returnToArena 
            });

            // 2. 응급 상황 처리 (HP 30% 이하일 때 회피)
            var emergencyHPCheck = new CheckHPNode(emergencyHPThreshold, true); // 자신의 HP 확인
            var emergencyDodge = new DodgeNode();
            var emergencySequence = new SequenceNode(new List<BTNode> 
            { 
                emergencyHPCheck, 
                emergencyDodge 
            });

            // 3. 공격 시퀀스
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

            // 4. 이동 및 접근 (공격이 불가능할 때)
            var moveToEnemyAlternative = new SequenceNode(new List<BTNode> 
            { 
                detectEnemy, 
                new MoveToEnemyNode(attackRange - 0.5f) 
            });

            // 5. 안전 순찰 (아레나 경계 고려)
            var patrol = new SafePatrolNode(3f, 0.75f); // 순찰 반경 3f, 아레나의 75% 내에서만

            // 최종 트리 구성: 안전 > 응급상황 > 공격 > 접근 > 순찰
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                safetySequence,       // 🆕 최우선 안전 시스템
                emergencySequence,    // 기존 응급 처리
                attackSequence,       // 기존 공격
                moveToEnemyAlternative, // 기존 접근
                patrol                // 기존 순찰
            });

            Debug.Log($"{agentName} BT 구조 생성 완료 (안전 시스템 포함)");
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

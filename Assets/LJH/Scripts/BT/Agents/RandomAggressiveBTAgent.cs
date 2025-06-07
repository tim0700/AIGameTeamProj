using System.Collections.Generic;
using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// RandomSelectorNode가 적용된 공격형 BT 에이전트 (실험 버전)
    /// 기존 AggressiveBTAgent에 랜덤성 추가
    /// </summary>
    public class RandomAggressiveBTAgent : BTAgentBase
    {
        [Header("공격형 설정")]
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public float emergencyHPThreshold = 30f;

        [Header("🎲 랜덤 설정")]
        public bool enableRandomAttackPattern = true;
        public bool enableDebugLog = false;

        protected override void BuildBehaviorTree()
        {
            agentName = "🎲 랜덤 공격형 BT 에이전트";
            agentType = "BT-Random-Aggressive";

            // 기존 안전 시스템 (변경 없음)
            var boundaryCheck = new CheckArenaBoundaryNode(0.8f, 0.9f, 0.95f);
            var returnToArena = new ReturnToArenaNodeBT(0.7f, 1.2f);
            var safetySequence = new SequenceNode(new List<BTNode> 
            { 
                boundaryCheck, 
                returnToArena 
            });

            // 기존 응급 상황 (변경 없음)
            var emergencyHPCheck = new CheckHPNode(emergencyHPThreshold, true);
            var emergencyDodge = new DodgeNode();
            var emergencySequence = new SequenceNode(new List<BTNode> 
            { 
                emergencyHPCheck, 
                emergencyDodge 
            });

            // 🆕 랜덤 공격 패턴들 생성
            BTNode attackStrategy;
            if (enableRandomAttackPattern)
            {
                // 서로 다른 공격 전략들
                var directAttackSequence = new SequenceNode(new List<BTNode> 
                { 
                    new DetectEnemyNode(detectionRange),
                    new CheckCooldownNode(ActionType.Attack),
                    new MoveToEnemyNode(attackRange - 0.2f), // 가까이 접근
                    new AttackNode(attackRange)
                });

                var cautiousAttackSequence = new SequenceNode(new List<BTNode> 
                { 
                    new DetectEnemyNode(detectionRange),
                    new CheckCooldownNode(ActionType.Attack),
                    new MoveToEnemyNode(attackRange), // 일반 거리
                    new AttackNode(attackRange)
                });

                var hitAndRunSequence = new SequenceNode(new List<BTNode> 
                { 
                    new DetectEnemyNode(detectionRange),
                    new CheckCooldownNode(ActionType.Attack),
                    new MoveToEnemyNode(attackRange),
                    new AttackNode(attackRange),
                    new MaintainDistanceNode(4f, 1f) // 공격 후 거리 벌리기
                });

                // 🎲 RandomSelectorNode로 공격 패턴 랜덤화
                var attackPatterns = new List<BTNode> 
                { 
                    directAttackSequence,    // 직접 공격
                    cautiousAttackSequence,  // 신중한 공격
                    hitAndRunSequence        // 공격 후 후퇴
                };

                attackStrategy = new RandomSelectorNode(attackPatterns)
                {
                    enableDebugLog = this.enableDebugLog
                };

                if (enableDebugLog)
                {
                    Debug.Log($"{agentName}: 랜덤 공격 패턴 활성화 ({attackPatterns.Count}가지 패턴)");
                }
            }
            else
            {
                // 기존 방식 (랜덤성 없음)
                attackStrategy = new SequenceNode(new List<BTNode> 
                { 
                    new DetectEnemyNode(detectionRange),
                    new CheckCooldownNode(ActionType.Attack),
                    new MoveToEnemyNode(attackRange),
                    new AttackNode(attackRange)
                });
            }

            // 기존 이동 및 순찰 (변경 없음)
            var moveToEnemyAlternative = new SequenceNode(new List<BTNode> 
            { 
                new DetectEnemyNode(detectionRange),
                new MoveToEnemyNode(attackRange - 0.5f) 
            });

            var patrol = new SafePatrolNode(3f, 0.75f);

            // 최종 트리 구성
            rootNode = new SelectorNode(new List<BTNode> 
            { 
                safetySequence,         // 안전 시스템 (최우선)
                emergencySequence,      // 응급 상황
                attackStrategy,         // 🆕 랜덤/일반 공격 전략
                moveToEnemyAlternative, // 대체 이동
                patrol                  // 순찰
            });

            if (enableDebugLog)
            {
                Debug.Log($"{agentName} BT 구조 생성 완료 (랜덤 패턴: {enableRandomAttackPattern})");
            }
        }

        public override void OnActionResult(ActionResult result)
        {
            base.OnActionResult(result);
            
            if (result.success && result.actionType == ActionType.Attack && enableDebugLog)
            {
                Debug.Log($"🎯 {agentName} 공격 성공! 데미지: {result.damage}");
            }
        }

        /// <summary>
        /// 런타임에 랜덤 패턴 활성화/비활성화
        /// </summary>
        public void ToggleRandomPattern()
        {
            enableRandomAttackPattern = !enableRandomAttackPattern;
            BuildBehaviorTree(); // 트리 재구성
            
            if (controller != null && rootNode != null)
            {
                rootNode.Initialize(controller);
            }
            
            Debug.Log($"{agentName} 랜덤 패턴 {(enableRandomAttackPattern ? "활성화" : "비활성화")}");
        }
    }
}

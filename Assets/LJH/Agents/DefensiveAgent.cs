using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class DefensiveAgent : BTAgent
    {
        [Header("Defensive Agent Settings")]
        public float optimalDistance = 3f;
        public float maxDistance = 5f;
        
        protected override void SetupTree()
        {
            // Create condition nodes
            var enemyInDetectionRange = new EnemyInRangeCondition(gameObject, detectionRange);
            var enemyInAttackRange = new EnemyInRangeCondition(gameObject, attackRange);
            var enemyAttacking = new EnemyStateCondition(gameObject, EnemyStateCondition.EnemyState.Attacking);
            var enemyVulnerable = new EnemyStateCondition(gameObject, EnemyStateCondition.EnemyState.Vulnerable);
            var defenseReady = new CooldownReadyCondition(gameObject, CooldownReadyCondition.CooldownType.Defense);
            var evasionReady = new CooldownReadyCondition(gameObject, CooldownReadyCondition.CooldownType.Evasion);
            var attackReady = new CooldownReadyCondition(gameObject, CooldownReadyCondition.CooldownType.Attack);
            var enemyTooClose = new EnemyInRangeCondition(gameObject, optimalDistance * 0.5f);
            var enemyTooFar = new Inverter(new EnemyInRangeCondition(gameObject, maxDistance));
            var enemyInOptimalRange = new Sequence(new List<Node>
            {
                new EnemyInRangeCondition(gameObject, optimalDistance),
                new Inverter(enemyTooClose)
            });

            // Create action nodes
            var defenseAction = new DefenseAction(gameObject);
            var evasionAction = new EvasionAction(gameObject);
            var counterAttackAction = new AttackAction(gameObject);
            var moveAwayAction = new MoveAwayFromEnemyAction(gameObject, moveSpeed);
            var moveTowardEnemyAction = new MoveTowardEnemyAction(gameObject, moveSpeed);
            var sideStepAction = new SideStepAction(gameObject, moveSpeed);
            var patrolAction = new PatrolAction(gameObject, moveSpeed);

            // Build the behavior tree according to the defensive strategy

            // Threat Response - Block Sequence
            Node blockSequence = new Sequence(new List<Node>
            {
                defenseReady,
                defenseAction
            });

            // Threat Response - Evasion Sequence
            Node evasionSequence = new Sequence(new List<Node>
            {
                evasionReady,
                evasionAction
            });

            // Defense Selector (prioritizes block, then evasion)
            Node defenseSelector = new Selector(new List<Node>
            {
                blockSequence,
                evasionSequence
            });

            // Enemy Attack Incoming Sequence
            Node enemyAttackIncomingSequence = new Sequence(new List<Node>
            {
                enemyInAttackRange,
                enemyAttacking,
                defenseSelector
            });

            // Counter Attack Sequence
            Node counterAttackSequence = new Sequence(new List<Node>
            {
                enemyVulnerable,
                enemyInAttackRange,
                attackReady,
                counterAttackAction
            });

            // Position Management - Too Close Sequence
            Node tooCloseSequence = new Sequence(new List<Node>
            {
                enemyTooClose,
                moveAwayAction
            });

            // Position Management - Too Far Sequence
            Node tooFarSequence = new Sequence(new List<Node>
            {
                enemyTooFar,
                moveTowardEnemyAction
            });

            // Position Management - Maintain Distance Sequence
            Node maintainDistanceSequence = new Sequence(new List<Node>
            {
                enemyInOptimalRange,
                sideStepAction
            });

            // Distance Management Selector
            Node distanceManagementSelector = new Selector(new List<Node>
            {
                tooCloseSequence,
                tooFarSequence,
                maintainDistanceSequence
            });

            // Enemy Detected Behavior
            Node enemyDetectedBehavior = new Sequence(new List<Node>
            {
                enemyInDetectionRange,
                new Selector(new List<Node>
                {
                    enemyAttackIncomingSequence,  // Priority 1: Defend against attacks
                    counterAttackSequence,         // Priority 2: Counter when enemy vulnerable
                    distanceManagementSelector     // Priority 3: Maintain optimal distance
                })
            });

            // Root Selector
            root = new Selector(new List<Node>
            {
                enemyDetectedBehavior,  // Priority 1: Combat behavior
                patrolAction           // Priority 2: Idle patrol
            });
        }

        protected override void Start()
        {
            base.Start();
            gameObject.name = "DefensiveAgent";
        }
    }
}

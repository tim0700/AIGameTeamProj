using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class OffensiveAgent : BTAgent
    {
        protected override void SetupTree()
        {
            // Create condition nodes
            var lowHealthCondition = new HealthBelowThresholdCondition(gameObject, 0.3f);
            var enemyInDetectionRange = new EnemyInRangeCondition(gameObject, detectionRange);
            var enemyInAttackRange = new EnemyInRangeCondition(gameObject, attackRange);
            var enemyOutOfAttackRange = new Inverter(new EnemyInRangeCondition(gameObject, attackRange));
            var attackReady = new CooldownReadyCondition(gameObject, CooldownReadyCondition.CooldownType.Attack);
            var enemyTooClose = new EnemyInRangeCondition(gameObject, 1.5f);

            // Create action nodes
            var moveAwayAction = new MoveAwayFromEnemyAction(gameObject, moveSpeed);
            var attackAction = new AttackAction(gameObject);
            var moveTowardEnemyAction = new MoveTowardEnemyAction(gameObject, moveSpeed);
            var sideStepAction = new SideStepAction(gameObject, moveSpeed);
            var patrolAction = new PatrolAction(gameObject, moveSpeed);

            // Build the behavior tree according to the offensive strategy

            // Emergency Behavior - Low Health Escape
            Node lowHealthSequence = new Sequence(new List<Node>
            {
                lowHealthCondition,
                moveAwayAction
            });

            // Combat Behavior - Attack Sequence
            Node attackSequence = new Sequence(new List<Node>
            {
                enemyInAttackRange,
                attackReady,
                attackAction
            });

            // Combat Behavior - Approach Sequence
            Node approachSequence = new Sequence(new List<Node>
            {
                enemyOutOfAttackRange,
                moveTowardEnemyAction
            });

            // Combat Behavior - Circle Strafe Sequence
            Node circleStrafeSequence = new Sequence(new List<Node>
            {
                enemyTooClose,
                sideStepAction
            });

            // Combat Selector (prioritizes attack, then approach, then strafe)
            Node combatSelector = new Selector(new List<Node>
            {
                attackSequence,
                approachSequence,
                circleStrafeSequence
            });

            // Enemy Detected Sequence
            Node enemyDetectedSequence = new Sequence(new List<Node>
            {
                enemyInDetectionRange,
                combatSelector
            });

            // Root Selector
            root = new Selector(new List<Node>
            {
                lowHealthSequence,      // Priority 1: Emergency
                enemyDetectedSequence,  // Priority 2: Combat
                patrolAction           // Priority 3: Idle patrol
            });
        }

        protected override void Start()
        {
            base.Start();
            gameObject.name = "OffensiveAgent";
        }
    }
}

using UnityEngine;
using BehaviorTree.Nodes;

namespace BehaviorTree.Strategies
{
    public class AttackingAgentBT : BehaviorTree
    {
        protected override void ConstructTree()
        {
            // 공격형 에이전트의 전략:
            // 1. 적을 찾아서 공격적으로 접근
            // 2. 공격 범위에 들어오면 지속적으로 공격
            // 3. 체력이 낮으면 회피하며 거리 조절
            // 4. 적이 없으면 순찰

            Selector rootSelector = new Selector(agent);
            
            // 1. 체력이 낮을 때의 생존 전략
            Sequence lowHealthSequence = new Sequence(agent);
            lowHealthSequence.AddChild(new CheckLowHealth(agent, 0.15f)); // 15% 이하 (더 공격적으로)
            lowHealthSequence.AddChild(new CheckEnemyInRange(agent));
            
            Selector lowHealthActions = new Selector(agent);
            
            // 회피 가능하면 회피
            Sequence dodgeSequence = new Sequence(agent);
            dodgeSequence.AddChild(new CheckCanDodge(agent));
            dodgeSequence.AddChild(new DodgeAction(agent));
            lowHealthActions.AddChild(dodgeSequence);
            
            // 거리 유지
            lowHealthActions.AddChild(new KeepDistance(agent, 5f));
            
            lowHealthSequence.AddChild(lowHealthActions);
            rootSelector.AddChild(lowHealthSequence);

            // 2. 적을 발견했을 때의 공격 전략
            Sequence combatSequence = new Sequence(agent);
            combatSequence.AddChild(new CheckEnemyInRange(agent));
            
            Selector combatActions = new Selector(agent);
            
            // 2-1. 공격 범위 내에 있으면 공격
            Sequence attackSequence = new Sequence(agent);
            attackSequence.AddChild(new CheckEnemyInAttackRange(agent));
            attackSequence.AddChild(new CheckCanAttack(agent));
            attackSequence.AddChild(new AttackTarget(agent));
            combatActions.AddChild(attackSequence);
            
            // 2-2. 공격 범위 밖이면 적극적으로 접근
            combatActions.AddChild(new MoveToTarget(agent, 1.8f)); // 더 가까이 접근
            
            combatSequence.AddChild(combatActions);
            rootSelector.AddChild(combatSequence);

            // 3. 적이 없으면 순찰
            rootSelector.AddChild(new Patrol(agent, 8f));

            SetRootNode(rootSelector);
        }
    }
}

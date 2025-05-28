using UnityEngine;
using BehaviorTree.Nodes;

namespace BehaviorTree.Strategies
{
    public class DefensiveAgentBT : BehaviorTree
    {
        protected override void ConstructTree()
        {
            // 수비형 에이전트의 전략:
            // 1. 적을 발견하면 거리를 유지하며 방어적으로 대응
            // 2. 적이 접근하면 방어하거나 회피
            // 3. 반격 기회를 노려 공격
            // 4. 체력 관리 우선

            Selector rootSelector = new Selector(agent);
            
            // 1. 적을 발견했을 때의 방어 전략
            Sequence combatSequence = new Sequence(agent);
            combatSequence.AddChild(new CheckEnemyInRange(agent));
            
            Selector combatActions = new Selector(agent);
            
            // 1-1. 체력이 낮으면 생존 우선
            Sequence lowHealthSequence = new Sequence(agent);
            lowHealthSequence.AddChild(new CheckLowHealth(agent, 0.4f)); // 40% 이하
            
            Selector survivalActions = new Selector(agent);
            
            // 회피 가능하면 회피
            Sequence dodgeSequence = new Sequence(agent);
            dodgeSequence.AddChild(new CheckCanDodge(agent));
            dodgeSequence.AddChild(new DodgeAction(agent));
            survivalActions.AddChild(dodgeSequence);
            
            // 거리 유지
            survivalActions.AddChild(new KeepDistance(agent, 6f));
            
            lowHealthSequence.AddChild(survivalActions);
            combatActions.AddChild(lowHealthSequence);
            
            // 1-2. 적이 공격 범위에 있을 때 방어적 대응
            Sequence closeRangeSequence = new Sequence(agent);
            closeRangeSequence.AddChild(new CheckEnemyInAttackRange(agent));
            
            Selector closeRangeActions = new Selector(agent);
            
            // 우선 반격 기회를 노림 (50% 확률로 공격 시도)
            Sequence aggressiveCounterSequence = new Sequence(agent);
            aggressiveCounterSequence.AddChild(new RandomChance(0.5f)); // 50% 확률
            aggressiveCounterSequence.AddChild(new CheckCanAttack(agent));
            aggressiveCounterSequence.AddChild(new AttackTarget(agent));
            closeRangeActions.AddChild(aggressiveCounterSequence);
            
            // 방어 가능하면 방어 (30% 확률로만)
            Sequence defendSequence = new Sequence(agent);
            defendSequence.AddChild(new RandomChance(0.3f)); // 30% 확률
            defendSequence.AddChild(new CheckCanDefend(agent));
            defendSequence.AddChild(new DefendAction(agent));
            closeRangeActions.AddChild(defendSequence);
            
            // 회피 가능하면 회피
            Sequence dodgeSequence2 = new Sequence(agent);
            dodgeSequence2.AddChild(new CheckCanDodge(agent));
            dodgeSequence2.AddChild(new DodgeAction(agent));
            closeRangeActions.AddChild(dodgeSequence2);
            
            // 나머지 경우 반격
            Sequence counterAttackSequence = new Sequence(agent);
            counterAttackSequence.AddChild(new CheckCanAttack(agent));
            counterAttackSequence.AddChild(new AttackTarget(agent));
            closeRangeActions.AddChild(counterAttackSequence);
            
            closeRangeSequence.AddChild(closeRangeActions);
            combatActions.AddChild(closeRangeSequence);
            
            // 1-3. 중거리에서 거리 유지하며 반격 기회 노리기
            Sequence midRangeSequence = new Sequence(agent);
            
            Selector midRangeActions = new Selector(agent);
            
            // 공격 가능하면 접근해서 공격 (30% 확률)
            Sequence approachAttackSequence = new Sequence(agent);
            approachAttackSequence.AddChild(new RandomChance(0.3f));
            approachAttackSequence.AddChild(new CheckCanAttack(agent));
            approachAttackSequence.AddChild(new MoveToTarget(agent, 1.5f));
            midRangeActions.AddChild(approachAttackSequence);
            
            // 그렇지 않으면 거리 유지
            midRangeActions.AddChild(new KeepDistance(agent, 3.5f));
            
            midRangeSequence.AddChild(midRangeActions);
            combatActions.AddChild(midRangeSequence);
            
            combatSequence.AddChild(combatActions);
            rootSelector.AddChild(combatSequence);

            // 2. 적이 없으면 순찰 (보수적으로)
            rootSelector.AddChild(new Patrol(agent, 5f));

            SetRootNode(rootSelector);
        }
    }
}

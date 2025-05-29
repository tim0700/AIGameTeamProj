using UnityEngine;

namespace BehaviorTree
{
    public class CooldownReadyCondition : ConditionNode
    {
        public enum CooldownType
        {
            Attack,
            Defense,
            Evasion
        }

        private CooldownType cooldownType;
        private BTAgent btAgent;

        public CooldownReadyCondition(GameObject agent, CooldownType cooldownType) : base(agent)
        {
            this.cooldownType = cooldownType;
            this.btAgent = agent.GetComponent<BTAgent>();
        }

        public override NodeState Evaluate()
        {
            bool isReady = false;

            switch (cooldownType)
            {
                case CooldownType.Attack:
                    isReady = btAgent.IsAttackReady();
                    break;
                case CooldownType.Defense:
                    isReady = btAgent.IsDefenseReady();
                    break;
                case CooldownType.Evasion:
                    isReady = btAgent.IsEvasionReady();
                    break;
            }

            state = isReady ? NodeState.SUCCESS : NodeState.FAILURE;
            return state;
        }
    }
}

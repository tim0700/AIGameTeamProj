using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 특정 행동의 쿨타임을 확인하는 노드
    /// </summary>
    public class CheckCooldownNode : BTNode
    {
        private ActionType actionType;

        public CheckCooldownNode(ActionType actionType)
        {
            this.actionType = actionType;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            switch (actionType)
            {
                case ActionType.Attack:
                    state = observation.cooldowns.CanAttack ? NodeState.Success : NodeState.Failure;
                    break;
                case ActionType.Defend:
                    state = observation.cooldowns.CanDefend ? NodeState.Success : NodeState.Failure;
                    break;
                case ActionType.Dodge:
                    state = observation.cooldowns.CanDodge ? NodeState.Success : NodeState.Failure;
                    break;
                default:
                    state = NodeState.Success; // 쿨타임이 없는 행동
                    break;
            }

            return state;
        }
    }
}

using UnityEngine;

public class AttackNode : BTNode
{
    private Transform agent;
    private Transform enemy;
    private float attackRange;
    private float attackCooldown;
    private float lastAttackTime;

    public AttackNode(Transform agent, Transform enemy, float range, float cooldown)
    {
        this.agent = agent;
        this.enemy = enemy;
        this.attackRange = range;
        this.attackCooldown = cooldown;
        this.lastAttackTime = -cooldown; // 시작부터 바로 공격 가능
    }

    public override NodeState Evaluate()
    {
        float distance = Vector3.Distance(agent.position, enemy.position);

        if (distance > attackRange)
        {
            state = NodeState.Failure;
            return state;
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // 실제 공격 로직
            Debug.Log("공격!");
            lastAttackTime = Time.time;

            // 여기서 적 체력 감소, 애니메이션 재생 등 가능
            state = NodeState.Success;
            return state;
        }

        // 쿨다운 중
        state = NodeState.Running;
        return state;
    }
}

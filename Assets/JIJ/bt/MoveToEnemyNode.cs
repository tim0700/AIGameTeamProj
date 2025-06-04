using UnityEngine;

public class MoveToEnemyNode : BTNode
{
    private Transform agent;
    private Transform enemy;
    private float stoppingDistance;
    private float moveSpeed;

    public MoveToEnemyNode(Transform agent, Transform enemy, float stoppingDistance, float moveSpeed)
    {
        this.agent = agent;
        this.enemy = enemy;
        this.stoppingDistance = stoppingDistance;
        this.moveSpeed = moveSpeed;
    }

    public override NodeState Evaluate()
    {
        float distance = Vector3.Distance(agent.position, enemy.position);

        if (distance <= stoppingDistance)
        {
            // 도착함
            state = NodeState.Success;
            return state;
        }

        // 적 방향으로 이동
        Vector3 direction = (enemy.position - agent.position).normalized;
        agent.position += direction * moveSpeed * Time.deltaTime;

        state = NodeState.Running;
        return state;
    }
}


using UnityEngine;

public class DetectEnemyNode : BTNode
{
    private Transform agent;
    private Transform enemy;
    private float detectionRange;

    public DetectEnemyNode(Transform agent, Transform enemy, float range)
    {
        this.agent = agent;
        this.enemy = enemy;
        this.detectionRange = range;
    }

    public override NodeState Evaluate()
    {
        float distance = Vector3.Distance(agent.position, enemy.position);

        if (distance <= detectionRange)
        {
            Debug.Log("적이 범위 안에 있음!");
            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}


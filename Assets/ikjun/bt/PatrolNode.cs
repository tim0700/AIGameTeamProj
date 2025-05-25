using UnityEngine;

public class PatrolNode : BTNode
{
    private Transform agent;
    private float rotationSpeed = 30f;

    public PatrolNode(Transform agent)
    {
        this.agent = agent;
    }

    public override NodeState Evaluate()
    {
        agent.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        Debug.Log("순찰 중... (회전)");

        state = NodeState.Running;
        return state;
    }
}

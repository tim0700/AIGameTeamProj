using UnityEngine;

public class DefensiveTestAgent : TestAgent
{
    void Start()
    {
        aggressionLevel = 0.3f; // ������
        actionDelay = 1.2f;     // ������ �ൿ
        agentName = "������ AI";
    }

    public override string GetAgentName() => "������ AI";

    public override string GetAgentType() => "Defensive AI";
}
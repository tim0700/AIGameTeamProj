using UnityEngine;

public class AggressiveTestAgent : TestAgent
{
    void Start()
    {
        aggressionLevel = 0.9f; // �ſ� ������
        actionDelay = 0.5f;     // ���� �ൿ
        agentName = "������ AI";
    }

    public override string GetAgentName() => "������ AI";

    public override string GetAgentType() => "Aggressive AI";
}
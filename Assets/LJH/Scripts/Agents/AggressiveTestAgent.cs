using UnityEngine;

public class AggressiveTestAgent : TestAgent
{
    void Start()
    {
        aggressionLevel = 0.9f; // 매우 공격적
        actionDelay = 0.5f;     // 빠른 행동
        agentName = "공격형 AI";
    }

    public override string GetAgentName() => "공격형 AI";

    public override string GetAgentType() => "Aggressive AI";
}
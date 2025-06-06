using UnityEngine;

public class DefensiveTestAgent : TestAgent
{
    void Start()
    {
        aggressionLevel = 0.3f; // 수비적
        actionDelay = 1.2f;     // 신중한 행동
        agentName = "수비형 AI";
    }

    public override string GetAgentName() => "수비형 AI";

    public override string GetAgentType() => "Defensive AI";
}
using UnityEngine;

/// <summary>
/// 방어적인 테스트 에이전트 - TestAgent를 상속받아 방어성을 높임
/// </summary>
public class DefensiveTestAgent : TestAgent
{
    void Start()
    {
        aggressionLevel = 0.3f; // 방어적
        actionDelay = 1.2f;     // 신중한 행동
        agentName = "방어형 AI";
    }

    public override string GetAgentName() => "방어형 AI";

    public override string GetAgentType() => "Defensive AI";
}

/// <summary>
/// 에이전트가 수행할 수 있는 행동 타입 정의
/// </summary>
public enum ActionType
{
    Idle,        // 대기 상태
    MoveForward, // 앞으로 이동
    MoveBack,    // 뒤로 이동
    MoveLeft,    // 왼쪽 이동
    MoveRight,   // 오른쪽 이동
    Attack,      // 공격
    Defend,      // 방어
    Dodge        // 회피
}

/// <summary>
/// 에이전트의 현재 상태를 나타내는 enum
/// </summary>
public enum AgentState
{
    Idle,      // 대기 상태
    Moving,    // 이동 중
    Attacking, // 공격 중
    Defending, // 방어 중
    Dodging,   // 회피 중
    Dead       // 사망
}
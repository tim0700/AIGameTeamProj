using UnityEngine;

/// <summary>
/// 게임 상황을 관찰할 수 있는 데이터 구조체
/// 에이전트가 의사 결정을 내리기 위해 필요한 모든 정보
/// </summary>
[System.Serializable]
public struct GameObservation
{
    public Vector3 selfPosition;
    public Vector3 enemyPosition;
    public float selfHP;
    public float enemyHP;
    public CooldownState cooldowns;
    public float distanceToEnemy;
    public AgentState currentState;
    public Vector3 arenaCenter;
    public float arenaRadius;
}

/// <summary>
/// 에이전트가 수행할 행동을 정의하는 구조체
/// </summary>
[System.Serializable]
public struct AgentAction
{
    public ActionType type;
    public Vector3 direction;
    public float intensity;

    public static AgentAction Idle => new AgentAction { type = ActionType.Idle };
    public static AgentAction Attack => new AgentAction { type = ActionType.Attack };
    public static AgentAction Defend => new AgentAction { type = ActionType.Defend };
    public static AgentAction Dodge => new AgentAction { type = ActionType.Dodge };

    public static AgentAction Move(Vector3 direction)
    {
        return new AgentAction { type = GetMoveType(direction), direction = direction };
    }

    private static ActionType GetMoveType(Vector3 dir)
    {
        if (dir.z > 0.5f) return ActionType.MoveForward;
        if (dir.z < -0.5f) return ActionType.MoveBack;
        if (dir.x > 0.5f) return ActionType.MoveRight;
        if (dir.x < -0.5f) return ActionType.MoveLeft;
        return ActionType.Idle;
    }
}

/// <summary>
/// 에이전트의 각 행동에 대한 쿨다운 상태를 관리
/// </summary>
[System.Serializable]
public struct CooldownState
{
    public float attackCooldown;
    public float defendCooldown;
    public float dodgeCooldown;
    public float attackMaxTime;
    public float defendMaxTime;
    public float dodgeMaxTime;

    public bool CanAttack => attackCooldown <= 0f;
    public bool CanDefend => defendCooldown <= 0f;
    public bool CanDodge => dodgeCooldown <= 0f;
}

/// <summary>
/// 행동 실행 결과를 나타내는 구조체
/// </summary>
[System.Serializable]
public struct ActionResult
{
    public bool success;
    public float damage;
    public ActionType actionType;
    public string message;
    public Vector3 resultPosition;
    internal GameObject target;
    // public GameObject target;
    public static ActionResult Success(ActionType type, float damage = 0f)
    {
        return new ActionResult
        {
            success = true,
            actionType = type,
            damage = damage,
            message = $"{type} 성공"
        };
    }

    public static ActionResult Failure(ActionType type, string reason)
    {
        return new ActionResult
        {
            success = false,
            actionType = type,
            message = reason
        };
    }
}

/// <summary>
/// 한 에피소드(배틀) 종료 시 결과를 나타내는 구조체
/// </summary>
[System.Serializable]
public struct EpisodeResult
{
    public bool won;
    public float finalHP;
    public float battleDuration;
    public int totalActions;
    public int successfulActions;
    public string agentName;
    public string enemyName;
}
using UnityEngine;

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

[System.Serializable]
public struct ActionResult
{
    public bool success;
    public float damage;
    public ActionType actionType;
    public string message;
    public Vector3 resultPosition;

    public static ActionResult Success(ActionType type, float damage = 0f)
    {
        return new ActionResult
        {
            success = true,
            actionType = type,
            damage = damage,
            message = $"{type} ¼º°ø"
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
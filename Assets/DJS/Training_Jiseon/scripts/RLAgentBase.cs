using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// AgentController를 내부에서 호출해 RL 학습용 인터페이스를 제공하는 베이스 클래스
/// </summary>
[RequireComponent(typeof(AgentController))]
public abstract class RLAgentBase : Agent
{
    protected AgentController ctrl;      // 이미 완성된 전투 로직
    [Header("보상 가중치")]
    public float winReward = 2.0f;
    public float losePenalty = -2.0f;
    public float timePenalty = -0.001f;  // 매 step 마다 부과

    // 내부 기록용
    protected float prevEnemyHP;
    protected float prevSelfHP;

    bool isReady = false;

    /* 외부에서 true/false 모두 넘길 수 있게 */
    public void SetReady(bool value = true) => isReady = value;


    public override void Initialize()
    {
        ctrl = GetComponent<AgentController>();
    }

    public override void OnEpisodeBegin()
    {
        if (!isReady) return;
        // RLEnvironmentManager가 위치 재배치·HP초기화 호출
        ctrl.ResetAgent();
        prevEnemyHP = ctrl.enemy.GetCurrentHP();
        prevSelfHP = ctrl.GetCurrentHP();
    }

    /* ----------------- 관측 공간 ----------------- */
    public override void CollectObservations(VectorSensor sensor)
    {
        if (!isReady)
        {               // 준비 전엔 모두 0 채우기
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f); sensor.AddObservation(0f);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            return;
        }

        // ① 나와 적의 위치(로컬), ② 체력·쿨타임, ③ 경계까지 거리
        Vector3 localPos = transform.localPosition / 15f;        // 아레나 반지름 15
        Vector3 enemyLocalPos = ctrl.enemy.transform.localPosition / 15f;

        sensor.AddObservation(localPos);                // (3)
        sensor.AddObservation(enemyLocalPos);           // (3)

        sensor.AddObservation(ctrl.GetCurrentHP() / ctrl.GetMaxHP());
        sensor.AddObservation(ctrl.enemy.GetCurrentHP() / ctrl.enemy.GetMaxHP());

        var cd = ctrl.GetCooldownState();
        var ecd = ctrl.enemy.GetCooldownState();

        sensor.AddObservation(cd.attackCooldown / cd.attackMaxTime);
        sensor.AddObservation(cd.defendCooldown / cd.defendMaxTime);
        sensor.AddObservation(cd.dodgeCooldown / cd.dodgeMaxTime);

        sensor.AddObservation(ecd.attackCooldown / ecd.attackMaxTime);
        sensor.AddObservation(ecd.defendCooldown / ecd.defendMaxTime);
        sensor.AddObservation(ecd.dodgeCooldown / ecd.dodgeMaxTime);
    }

    /* ----------------- 행동 공간 ----------------- */
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: discrete 0~6  → Idle, F, B, L, R, Attack, Defend, Dodge
        int act = actions.DiscreteActions[0];

        AgentAction a = new AgentAction();
        a.type = act switch
        {
            0 => ActionType.Idle,
            1 => ActionType.MoveForward,
            2 => ActionType.MoveBack,
            3 => ActionType.MoveLeft,
            4 => ActionType.MoveRight,
            5 => ActionType.Attack,
            6 => ActionType.Defend,
            7 => ActionType.Dodge,
            _ => ActionType.Idle
        };


        var result = ctrl.ExecuteAction(a);

        /* -------- 공통 보상 처리 -------- */
        AddReward(timePenalty);                  // 시간 페널티

        if (result.success && result.actionType == ActionType.Attack)
            OnAttackSuccess();

        //if (!result.success && result.cooldownBlocked)AddReward(-0.01f);                   // 쿨다운 중 잘못된 시도 소 벌점

        // 승패 판정은 EnvironmentManager에서 EndEpisode 호출
    }

    protected virtual void OnAttackSuccess() { /* 개별 에이전트가 구현 */ }

    /* ----------------- 휴리스틱 ----------------- */
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 간단 키보드 수동 테스트용(WASD+JKL), 없어도 됨
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;
        if (Input.GetKey(KeyCode.W)) discrete[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discrete[0] = 2;
        else if (Input.GetKey(KeyCode.A)) discrete[0] = 3;
        else if (Input.GetKey(KeyCode.D)) discrete[0] = 4;
        else if (Input.GetKey(KeyCode.J)) discrete[0] = 5;
        else if (Input.GetKey(KeyCode.K)) discrete[0] = 6;
        else if (Input.GetKey(KeyCode.L)) discrete[0] = 7;
    }

    /* ----------------- 외부에서 호출 ----------------- */
    public void Win() { AddReward(winReward); EndEpisode(); }
    public void Lose() { AddReward(losePenalty); EndEpisode(); }
}

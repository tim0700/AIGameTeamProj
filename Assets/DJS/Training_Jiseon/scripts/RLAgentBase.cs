using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// AgentController�� ���ο��� ȣ���� RL �н��� �������̽��� �����ϴ� ���̽� Ŭ����
/// </summary>
[RequireComponent(typeof(AgentController))]
public abstract class RLAgentBase : Agent
{
    protected AgentController ctrl;      // �̹� �ϼ��� ���� ����
    [Header("���� ����ġ")]
    public float winReward = 2.0f;
    public float losePenalty = -2.0f;
    public float timePenalty = -0.001f;  // �� step ���� �ΰ�

    // ���� ��Ͽ�
    protected float previousEnemyHP;
    protected float prevSelfHP;

    bool isReady = false;

    /* �ܺο��� true/false ��� �ѱ� �� �ְ� */
    public void SetReady(bool value = true) => isReady = value;

    void Update()
    {
        // 정상 속도로 강제 설정 (배속 방지)
        Time.timeScale = 1.0f;
    }

    public override void Initialize()
    {
        ctrl = GetComponent<AgentController>();
    }

    public override void OnEpisodeBegin()
    {
        if (!isReady) return;
        // RLEnvironmentManager�� ��ġ ���ġ��HP�ʱ�ȭ ȣ��
        ctrl.ResetAgent();
        previousEnemyHP = ctrl.enemy.GetCurrentHP();
        prevSelfHP = ctrl.GetCurrentHP();
    }

    /* ----------------- ���� ���� ----------------- */
    public override void CollectObservations(VectorSensor sensor)
    {
        if (!isReady)
        {               // �غ� ���� ��� 0 ä���
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f); sensor.AddObservation(0f);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            return;
        }

        // �� ���� ���� ��ġ(����), �� ü�¡���Ÿ��, �� ������ �Ÿ�
        Vector3 localPos = transform.localPosition / 15f;        // �Ʒ��� ������ 15
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

    /* ----------------- �ൿ ���� ----------------- */
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: discrete 0~6  �� Idle, F, B, L, R, Attack, Defend, Dodge
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

        /* -------- ���� ���� ó�� -------- */
        AddReward(timePenalty);                  // �ð� ���Ƽ

        if (result.success && result.actionType == ActionType.Attack)
            OnAttackSuccess();

        //if (!result.success && result.cooldownBlocked)AddReward(-0.01f);                   // ��ٿ� �� �߸��� �õ� �� ����

        // ���� ������ EnvironmentManager���� EndEpisode ȣ��
    }

    protected virtual void OnAttackSuccess() { /* ���� ������Ʈ�� ���� */ }

    /* ----------------- �޸���ƽ ----------------- */
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // ���� Ű���� ���� �׽�Ʈ��(WASD+JKL), ��� ��
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

    /* ----------------- �ܺο��� ȣ�� ----------------- */
    public virtual void Win() { AddReward(winReward); EndEpisode(); }
    public virtual void Lose() { AddReward(losePenalty); EndEpisode(); }

    public virtual void Draw() { EndEpisode(); }   // �߸�(0��)���� ����

    public new virtual void EndEpisode()
    {
        base.EndEpisode();
    }
}

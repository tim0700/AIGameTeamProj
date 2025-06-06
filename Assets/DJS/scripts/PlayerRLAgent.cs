using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerRLAgent : Agent
{
    [Header("참조")]
    public PlayerRL self;
    public PlayerRL opponent;
    public Rigidbody rb;
    public Transform arenaCenter;

    public bool debugConsole = true;   // 인스펙터에서 켜기
    public int debugEvery = 1;      // 1 스텝마다 출력

    // 파라미터
    readonly float arenaHalf = 15f;
    readonly float maxSpeed = 6f;

    public override void Initialize()
    {
        if (rb == null) rb = self.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // 위치 초기화
        self.ResetStatus();
        opponent.ResetStatus();

        self.transform.localPosition = new Vector3(-4, 0, 0);
        opponent.transform.localPosition = new Vector3(4, 0, 0);

        self.transform.rotation = Quaternion.identity;
        opponent.transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 본인 위치·속도
        sensor.AddObservation(self.transform.localPosition.x / arenaHalf);
        sensor.AddObservation(self.transform.localPosition.z / arenaHalf);
        sensor.AddObservation(rb.velocity.x / maxSpeed);
        sensor.AddObservation(rb.velocity.z / maxSpeed);

        // 본인 체력·쿨타임
        sensor.AddObservation(self.CurHP / self.maxHP);
        sensor.AddObservation(self.AttackCD / self.attackCooldownTime);
        sensor.AddObservation(self.GuardCD / self.guardCooldownTime);
        sensor.AddObservation(self.DodgeCD / self.dodgeCooldownTime);

        // 상대 위치·체력
        Vector3 rel = opponent.transform.localPosition - self.transform.localPosition;
        sensor.AddObservation(rel.x / arenaHalf);
        sensor.AddObservation(rel.z / arenaHalf);
        sensor.AddObservation(opponent.CurHP / opponent.maxHP);

        // 경계까지 거리
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.x)) / arenaHalf);
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.z)) / arenaHalf);

        // if (debugConsole) Debug.Log($"[OBS] Size={sensor.ObservationSize()}  Step={StepCount}");
    }

    // Branch 0: 0정지 1앞 2뒤 3좌 4우
    // Branch 1: 0없음 1공격 2방어 3회피
    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0];
        int skill = actions.DiscreteActions[1];

        Vector3 dir = Vector3.zero;
        if (move == 1) dir = self.transform.forward;
        if (move == 2) dir = -self.transform.forward;
        if (move == 3) dir = -self.transform.right;
        if (move == 4) dir = self.transform.right;
        rb.AddForce(dir * 20f, ForceMode.Force);

        bool actionDone = false;
        if (skill == 1) { if (self.CanAttack()) { self.RL_Attack(); actionDone = true; } }
        if (skill == 2) { if (self.CanGuard()) { self.RL_Guard(); actionDone = true; } }
        if (skill == 3) { if (self.CanDodge()) { self.RL_Dodge(); actionDone = true; } }

        // 쿨타임 중 시도하면 작은 패널티
        if (!actionDone && skill != 0) AddReward(-0.05f);

        if (Unity.MLAgents.Academy.Instance.IsCommunicatorOn)
        {
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Agent/HP", self.CurHP / self.maxHP, StatAggregationMethod.MostRecent);
            sr.Add("Agent/AtkCD", self.AttackCD, StatAggregationMethod.MostRecent);
            sr.Add("Agent/CumReward", GetCumulativeReward(), StatAggregationMethod.MostRecent);
        }

        //if (debugConsole && StepCount % debugEvery == 0) Debug.Log($"[ACT] Step={StepCount}  Move={actions.DiscreteActions[0]}  Skill={actions.DiscreteActions[1]}  Rew={GetCumulativeReward():F3}");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0;
        if (Input.GetKey(KeyCode.W)) d[0] = 1;
        if (Input.GetKey(KeyCode.S)) d[0] = 2;
        if (Input.GetKey(KeyCode.A)) d[0] = 3;
        if (Input.GetKey(KeyCode.D)) d[0] = 4;

        d[1] = 0;
        if (Input.GetKey(KeyCode.J)) d[1] = 1;
        if (Input.GetKey(KeyCode.K)) d[1] = 2;
        if (Input.GetKey(KeyCode.L)) d[1] = 3;
    }

    // 체력·승패 보상은 PlayerRL.TakeDamage() 안에서 self.Agent.AddReward 호출로 처리해도 된다
}

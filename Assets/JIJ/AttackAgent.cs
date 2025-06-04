using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class IKJ_AttackAgent : Agent
{
    public Transform enemy;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    private float lastAttackTime; //마지막 공격시간을 저장하여 쿨타임 체크에 사용
    public float attackCooldown = 2f;//쿨타임

    public override void OnEpisodeBegin()
    {
        // 초기화: 위치 리셋
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        //x,z축은 -4~4 사이 무작위 위치, y축은 땅에 띄우기 위해 0.5로 고정. 캐릭터 몸의 중심이 0.5에 있으므로 -> 학습이 특정 위치 패턴에만 익숙해지는걸 막으려고
        enemy.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        lastAttackTime = -attackCooldown; // 처음부터 바로 공격가능하게하려고
        //53번 if (dist < attackRange && Time.time - lastAttackTime >= attackCooldown)코드에서 게임시작시 바로 공격할수있도록 위와같이 설정함.
    }

    public override void CollectObservations(VectorSensor sensor)
    //에이전트가 학습시 보는 정보를 등록
    {
        // 자기 위치, 적 위치, 적까지 거리
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(enemy.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, enemy.localPosition));
        //적과의 거리 측정

        // 공격 쿨다운 남은 시간
        float cooldownLeft = Mathf.Max(0f, attackCooldown - (Time.time - lastAttackTime));
        sensor.AddObservation(cooldownLeft / attackCooldown); // 정규화(0~1로 정규화된 수치)
    }

    public override void OnActionReceived(ActionBuffers actions)
    // 학습 목적 : 적을 공격범위 안에서 공격 쿨다운을 지켜 성공적으로 공격하는것
    {
        int action = actions.DiscreteActions[0]; //행동을 정수형으로 받아옴.

        switch (action)
        {
            case 0: // 대기
                AddReward(-0.01f); // 시간 낭비 패널티를 줘서 무한대기 방지
                break;

            case 1: // 이동
                Vector3 dir = (enemy.position - transform.position).normalized;
                //.normalized를 통해 길이를 1로 맞춰 방향 정보만 남김.
                transform.position += dir * moveSpeed * Time.deltaTime;
                AddReward(-0.005f); // 조금의 탐색 패널티를줘서 무한이동 방지
                break;

            case 2: // 공격 시도
                float dist = Vector3.Distance(transform.position, enemy.position);
                if (dist < attackRange && Time.time - lastAttackTime >= attackCooldown)
                //Time.time은 게임 시작이후 흐른시간을 의미함.
                {
                    Debug.Log("공격 성공!");
                    AddReward(+1f); // 공격 성공시 보상
                    lastAttackTime = Time.time;
                }
                else
                {
                    AddReward(-0.5f); // 쿨다운 중 공격 시도 → 패널티
                }
                break;
        }

        // 실패 조건 예시: 너무 멀면 종료
        if (Vector3.Distance(transform.position, enemy.position) > 10f)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 수동 테스트용: 키보드 입력
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.Alpha1)) discreteActionsOut[0] = 1; // 이동
        else if (Input.GetKey(KeyCode.Alpha2)) discreteActionsOut[0] = 2; // 공격
        else discreteActionsOut[0] = 0; // 대기
    }
}


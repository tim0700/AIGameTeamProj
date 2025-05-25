using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class IKJ_AttackAgent : Agent
{
    public Transform enemy;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    private float lastAttackTime;
    public float attackCooldown = 2f;

    public override void OnEpisodeBegin()
    {
        // 초기화: 위치 리셋
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        enemy.localPosition = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-4f, 4f));
        lastAttackTime = -attackCooldown;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 자기 위치, 적 위치, 적까지 거리
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(enemy.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, enemy.localPosition));

        // 공격 쿨다운 남은 시간
        float cooldownLeft = Mathf.Max(0f, attackCooldown - (Time.time - lastAttackTime));
        sensor.AddObservation(cooldownLeft / attackCooldown); // 정규화
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 0: // 대기
                AddReward(-0.01f); // 시간 낭비 패널티
                break;

            case 1: // 이동
                Vector3 dir = (enemy.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                AddReward(-0.005f); // 조금의 탐색 패널티
                break;

            case 2: // 공격 시도
                float dist = Vector3.Distance(transform.position, enemy.position);
                if (dist < attackRange && Time.time - lastAttackTime >= attackCooldown)
                {
                    Debug.Log("공격 성공!");
                    AddReward(+1f);
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


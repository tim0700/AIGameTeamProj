using UnityEngine;

public class CombatCameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    public Transform agentA;
    public Transform agentB;
    public float height = 12f;
    public float distance = 8f;
    public float smoothSpeed = 2f;

    [Header("카메라 제한")]
    public float minDistance = 5f;
    public float maxDistance = 20f;

    private Vector3 targetPosition;

    void LateUpdate()
    {
        if (agentA == null || agentB == null) return;

        UpdateCameraPosition();

        // 부드러운 카메라 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // 카메라가 중점을 바라보도록
        Vector3 centerPoint = (agentA.position + agentB.position) / 2f;
        transform.LookAt(centerPoint);
    }

    private void UpdateCameraPosition()
    {
        // 두 에이전트 사이의 중점 계산
        Vector3 centerPoint = (agentA.position + agentB.position) / 2f;

        // 에이전트 간 거리에 따라 카메라 거리 조정
        float agentDistance = Vector3.Distance(agentA.position, agentB.position);
        float adjustedDistance = Mathf.Clamp(distance + agentDistance * 0.5f, minDistance, maxDistance);

        // 카메라 목표 위치 계산
        targetPosition = centerPoint + Vector3.up * height + Vector3.back * adjustedDistance;
    }

    public void SetTargets(Transform newAgentA, Transform newAgentB)
    {
        agentA = newAgentA;
        agentB = newAgentB;
    }
}
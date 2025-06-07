using UnityEngine;

public class CombatCameraController : MonoBehaviour
{
    [Header("ī�޶� ����")]
    public Transform agentA;
    public Transform agentB;
    public float height = 12f;
    public float distance = 8f;
    public float smoothSpeed = 2f;

    [Header("ī�޶� ����")]
    public float minDistance = 5f;
    public float maxDistance = 20f;

    private Vector3 targetPosition;

    void LateUpdate()
    {
        if (agentA == null || agentB == null) return;

        UpdateCameraPosition();

        // �ε巯�� ī�޶� �̵�
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // ī�޶� ������ �ٶ󺸵���
        Vector3 centerPoint = (agentA.position + agentB.position) / 2f;
        transform.LookAt(centerPoint);
    }

    private void UpdateCameraPosition()
    {
        // �� ������Ʈ ������ ���� ���
        Vector3 centerPoint = (agentA.position + agentB.position) / 2f;

        // ������Ʈ �� �Ÿ��� ���� ī�޶� �Ÿ� ����
        float agentDistance = Vector3.Distance(agentA.position, agentB.position);
        float adjustedDistance = Mathf.Clamp(distance + agentDistance * 0.5f, minDistance, maxDistance);

        // ī�޶� ��ǥ ��ġ ���
        targetPosition = centerPoint + Vector3.up * height + Vector3.back * adjustedDistance;
    }

    public void SetTargets(Transform newAgentA, Transform newAgentB)
    {
        agentA = newAgentA;
        agentB = newAgentB;
    }
}
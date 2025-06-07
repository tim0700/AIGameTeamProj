using UnityEngine;

public interface IBattleAgent
{
    /// <summary>
    /// ���� ��Ȳ�� �����ϰ� ���� �ൿ�� ����
    /// </summary>
    AgentAction DecideAction(GameObservation observation);

    /// <summary>
    /// �ൿ ����� �޾Ƽ� �н�/����
    /// </summary>
    void OnActionResult(ActionResult result);

    /// <summary>
    /// ���Ǽҵ�(����) ���� �� ȣ��
    /// </summary>
    void OnEpisodeEnd(EpisodeResult result);

    /// <summary>
    /// ������Ʈ �ʱ�ȭ (��Ʈ�ѷ� ����)
    /// </summary>
    void Initialize(AgentController controller);

    /// <summary>
    /// ������Ʈ �̸�
    /// </summary>
    string GetAgentName();

    /// <summary>
    /// ������Ʈ Ÿ�� (BT, RL ��)
    /// </summary>
    string GetAgentType();
}
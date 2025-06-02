using UnityEngine;

public interface IBattleAgent
{
    /// <summary>
    /// 현재 상황을 관찰하고 다음 행동을 결정
    /// </summary>
    AgentAction DecideAction(GameObservation observation);

    /// <summary>
    /// 행동 결과를 받아서 학습/조정
    /// </summary>
    void OnActionResult(ActionResult result);

    /// <summary>
    /// 에피소드(전투) 종료 시 호출
    /// </summary>
    void OnEpisodeEnd(EpisodeResult result);

    /// <summary>
    /// 에이전트 초기화 (컨트롤러 연결)
    /// </summary>
    void Initialize(AgentController controller);

    /// <summary>
    /// 에이전트 이름
    /// </summary>
    string GetAgentName();

    /// <summary>
    /// 에이전트 타입 (BT, RL 등)
    /// </summary>
    string GetAgentType();
}
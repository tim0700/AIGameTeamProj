using UnityEngine;
using System;

namespace LJH.BT
{
    /// <summary>
    /// 에이전트 시뮬레이션 결과를 CSV 저장용으로 구조화한 레코드
    /// 각 에피소드에서 각 에이전트당 하나의 레코드가 생성됨
    /// </summary>
    [System.Serializable]
    public struct AgentSimulationRecord
    {
        [Header("기본 정보")]
        public string timestamp;           // 시간 (YYYY-MM-DD HH:MM:SS)
        public string episodeId;          // 에피소드 ID
        public string agentName;          // 에이전트 이름
        public string agentType;          // 에이전트 타입 (Aggressive, Defensive 등)
        
        [Header("전투 결과")]
        public float winResult;           // 승리: 1, 패배: 0, 무승부: 0.5
        public float finalHP;             // 최종 체력
        public float initialHP;           // 초기 체력
        public float battleDuration;      // 전투 지속 시간 (초)
        
        [Header("행동 통계")]
        public int totalActions;          // 총 행동 수
        public float averageExecutionTime; // 평균 실행 시간 (밀리초)
        public string enemyAgentName;     // 상대 에이전트 이름
        
        [Header("공격 통계")]
        public int attackAttempts;        // 공격 시도 횟수
        public int attackSuccesses;       // 공격 성공 횟수
        
        [Header("방어 통계")]
        public int defenseAttempts;       // 방어 시도 횟수
        public int defenseSuccesses;      // 방어 성공 횟수
        
        [Header("회피 통계")]
        public int dodgeAttempts;         // 회피 시도 횟수
        public int dodgeSuccesses;        // 회피 성공 횟수
        
        [Header("🆕 누적 통계")]
        public int cumulativeRounds;      // 누적 라운드 수
        public int cumulativeWins;        // 누적 승리 횟수
        public float winPercentage;       // 승리 비율 (%)
        
        /// <summary>
        /// CSV 헤더 문자열 반환
        /// </summary>
        public static string GetCSVHeader()
        {
            return "Timestamp,EpisodeId,AgentName,AgentType,WinResult,FinalHP,InitialHP," +
                   "BattleDuration,TotalActions,AverageExecutionTime,EnemyAgentName," +
                   "AttackAttempts,AttackSuccesses,DefenseAttempts,DefenseSuccesses," +
                   "DodgeAttempts,DodgeSuccesses,AttackSuccessRate,DefenseSuccessRate,DodgeSuccessRate," +
                   "CumulativeRounds,CumulativeWins,WinPercentage";
        }
        
        /// <summary>
        /// CSV 데이터 문자열 반환
        /// </summary>
        public string ToCSVString()
        {
            // 성공률 계산
            float attackSuccessRate = attackAttempts > 0 ? (float)attackSuccesses / attackAttempts : 0f;
            float defenseSuccessRate = defenseAttempts > 0 ? (float)defenseSuccesses / defenseAttempts : 0f;
            float dodgeSuccessRate = dodgeAttempts > 0 ? (float)dodgeSuccesses / dodgeAttempts : 0f;
            
            return $"{timestamp},{episodeId},{agentName},{agentType},{winResult:F1}," +
                   $"{finalHP:F1},{initialHP:F1},{battleDuration:F2},{totalActions}," +
                   $"{averageExecutionTime:F2},{enemyAgentName},{attackAttempts},{attackSuccesses}," +
                   $"{defenseAttempts},{defenseSuccesses},{dodgeAttempts},{dodgeSuccesses}," +
                   $"{attackSuccessRate:F3},{defenseSuccessRate:F3},{dodgeSuccessRate:F3}," +
                   $"{cumulativeRounds},{cumulativeWins},{winPercentage:F1}";
        }
        
        /// <summary>
        /// ActionTracker와 기본 정보로부터 레코드 생성
        /// </summary>
        /// <param name="episodeId">에피소드 ID</param>
        /// <param name="agentName">에이전트 이름</param>
        /// <param name="agentType">에이전트 타입</param>
        /// <param name="winResult">승부 결과</param>
        /// <param name="finalHP">최종 체력</param>
        /// <param name="initialHP">초기 체력</param>
        /// <param name="battleDuration">전투 지속 시간</param>
        /// <param name="enemyAgentName">상대 에이전트 이름</param>
        /// <param name="actionTracker">행동 추적기</param>
        /// <param name="cumulativeRounds">누적 라운드 수</param>
        /// <param name="cumulativeWins">누적 승리 횟수</param>
        /// <returns>생성된 레코드</returns>
        public static AgentSimulationRecord Create(
            string episodeId,
            string agentName,
            string agentType,
            float winResult,
            float finalHP,
            float initialHP,
            float battleDuration,
            string enemyAgentName,
            ActionTracker actionTracker,
            int cumulativeRounds = 0,
            int cumulativeWins = 0)
        {
            // 승리 비율 계산
            float winPercentage = cumulativeRounds > 0 ? ((float)cumulativeWins / cumulativeRounds) * 100f : 0f;
            
            return new AgentSimulationRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                episodeId = episodeId,
                agentName = agentName,
                agentType = agentType,
                winResult = winResult,
                finalHP = finalHP,
                initialHP = initialHP,
                battleDuration = battleDuration,
                totalActions = actionTracker.TotalActions,
                averageExecutionTime = actionTracker.AverageExecutionTime,
                enemyAgentName = enemyAgentName,
                attackAttempts = actionTracker.AttackAttempts,
                attackSuccesses = actionTracker.AttackSuccesses,
                defenseAttempts = actionTracker.DefenseAttempts,
                defenseSuccesses = actionTracker.DefenseSuccesses,
                dodgeAttempts = actionTracker.DodgeAttempts,
                dodgeSuccesses = actionTracker.DodgeSuccesses,
                
                // 🆕 누적 통계
                cumulativeRounds = cumulativeRounds,
                cumulativeWins = cumulativeWins,
                winPercentage = winPercentage
            };
        }
        
        /// <summary>
        /// 빈 레코드 생성 (테스트용)
        /// </summary>
        public static AgentSimulationRecord CreateEmpty(string agentName = "TestAgent")
        {
            return new AgentSimulationRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                episodeId = "TEST_EP",
                agentName = agentName,
                agentType = "Test",
                winResult = 0f,
                finalHP = 100f,
                initialHP = 100f,
                battleDuration = 0f,
                totalActions = 0,
                averageExecutionTime = 0f,
                enemyAgentName = "TestEnemy",
                attackAttempts = 0,
                attackSuccesses = 0,
                defenseAttempts = 0,
                defenseSuccesses = 0,
                dodgeAttempts = 0,
                dodgeSuccesses = 0,
                
                // 🆕 누적 통계 기본값
                cumulativeRounds = 0,
                cumulativeWins = 0,
                winPercentage = 0f
            };
        }
        
        /// <summary>
        /// 레코드 유효성 검증
        /// </summary>
        /// <returns>유효하면 true</returns>
        public bool IsValid()
        {
            // 필수 필드 체크
            if (string.IsNullOrEmpty(episodeId) || string.IsNullOrEmpty(agentName))
                return false;
            
            // 논리적 검증
            if (finalHP < 0 || initialHP <= 0 || battleDuration < 0)
                return false;
            
            if (attackSuccesses > attackAttempts || 
                defenseSuccesses > defenseAttempts || 
                dodgeSuccesses > dodgeAttempts)
                return false;
            
            if (winResult < 0 || winResult > 1)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// 레코드 정보 요약
        /// </summary>
        /// <returns>요약 문자열</returns>
        public string GetSummary()
        {
            float attackSuccessRate = attackAttempts > 0 ? (float)attackSuccesses / attackAttempts : 0f;
            float defenseSuccessRate = defenseAttempts > 0 ? (float)defenseSuccesses / defenseAttempts : 0f;
            float dodgeSuccessRate = dodgeAttempts > 0 ? (float)dodgeSuccesses / dodgeAttempts : 0f;
            
            return $"[{agentName}] {episodeId}\n" +
                   $"결과: {(winResult == 1 ? "승리" : winResult == 0 ? "패배" : "무승부")} " +
                   $"(HP: {finalHP:F0}/{initialHP:F0})\n" +
                   $"지속시간: {battleDuration:F1}초, 총 행동: {totalActions}개\n" +
                   $"성공률 - 공격: {attackSuccessRate:P1}, 방어: {defenseSuccessRate:P1}, 회피: {dodgeSuccessRate:P1}";
        }
        
        /// <summary>
        /// 다른 레코드와 비교
        /// </summary>
        /// <param name="other">비교할 레코드</param>
        /// <returns>비교 결과</returns>
        public string CompareTo(AgentSimulationRecord other)
        {
            float thisAttackRate = attackAttempts > 0 ? (float)attackSuccesses / attackAttempts : 0f;
            float otherAttackRate = other.attackAttempts > 0 ? (float)other.attackSuccesses / other.attackAttempts : 0f;
            
            float thisDefenseRate = defenseAttempts > 0 ? (float)defenseSuccesses / defenseAttempts : 0f;
            float otherDefenseRate = other.defenseAttempts > 0 ? (float)other.defenseSuccesses / other.defenseAttempts : 0f;
            
            return $"[{agentName} vs {other.agentName}]\n" +
                   $"승부: {winResult:F1} vs {other.winResult:F1}\n" +
                   $"공격 성공률: {thisAttackRate:P1} vs {otherAttackRate:P1}\n" +
                   $"방어 성공률: {thisDefenseRate:P1} vs {otherDefenseRate:P1}\n" +
                   $"생존율: {(finalHP/initialHP):P1} vs {(other.finalHP/other.initialHP):P1}";
        }
        
        /// <summary>
        /// 디버그용 전체 정보 출력
        /// </summary>
        /// <returns>상세 정보 문자열</returns>
        public override string ToString()
        {
            return $"AgentSimulationRecord[{episodeId}:{agentName}] " +
                   $"Win:{winResult} HP:{finalHP}/{initialHP} " +
                   $"Actions:{totalActions} Duration:{battleDuration:F1}s " +
                   $"Attack:{attackSuccesses}/{attackAttempts} " +
                   $"Defense:{defenseSuccesses}/{defenseAttempts} " +
                   $"Dodge:{dodgeSuccesses}/{dodgeAttempts}";
        }
    }
}

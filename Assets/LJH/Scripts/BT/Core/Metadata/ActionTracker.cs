using UnityEngine;
using System;

namespace LJH.BT
{
    /// <summary>
    /// 에이전트의 행동 통계를 실시간으로 추적하는 클래스
    /// 공격/방어/회피의 시도 횟수와 성공 횟수를 기록
    /// </summary>
    [System.Serializable]
    public class ActionTracker
    {
        [Header("공격 통계")]
        [SerializeField] private int attackAttempts = 0;
        [SerializeField] private int attackSuccesses = 0;
        
        [Header("방어 통계")]
        [SerializeField] private int defenseAttempts = 0;
        [SerializeField] private int defenseSuccesses = 0;
        
        [Header("회피 통계")]
        [SerializeField] private int dodgeAttempts = 0;
        [SerializeField] private int dodgeSuccesses = 0;
        
        [Header("추가 통계")]
        [SerializeField] private int totalActions = 0;
        [SerializeField] private float totalExecutionTime = 0f;
        
        // 퍼블릭 프로퍼티 (읽기 전용)
        public int AttackAttempts => attackAttempts;
        public int AttackSuccesses => attackSuccesses;
        public int DefenseAttempts => defenseAttempts;
        public int DefenseSuccesses => defenseSuccesses;
        public int DodgeAttempts => dodgeAttempts;
        public int DodgeSuccesses => dodgeSuccesses;
        public int TotalActions => totalActions;
        public float TotalExecutionTime => totalExecutionTime;
        
        // 성공률 계산 프로퍼티
        public float AttackSuccessRate => attackAttempts > 0 ? (float)attackSuccesses / attackAttempts : 0f;
        public float DefenseSuccessRate => defenseAttempts > 0 ? (float)defenseSuccesses / defenseAttempts : 0f;
        public float DodgeSuccessRate => dodgeAttempts > 0 ? (float)dodgeSuccesses / dodgeAttempts : 0f;
        public float AverageExecutionTime => totalActions > 0 ? totalExecutionTime / totalActions : 0f;
        
        /// <summary>
        /// 공격 행동 기록
        /// </summary>
        /// <param name="success">공격 성공 여부</param>
        /// <param name="executionTime">실행 시간 (밀리초)</param>
        public void RecordAttack(bool success, float executionTime = 0f)
        {
            attackAttempts++;
            if (success)
            {
                attackSuccesses++;
            }
            
            RecordAction(executionTime);
            
            Debug.Log($"[ActionTracker] 공격 기록: {(success ? "성공" : "실패")} " +
                     $"(성공률: {AttackSuccessRate:P1}, 시도: {attackAttempts})");
        }
        
        /// <summary>
        /// 방어 행동 기록
        /// </summary>
        /// <param name="success">방어 성공 여부</param>
        /// <param name="executionTime">실행 시간 (밀리초)</param>
        public void RecordDefense(bool success, float executionTime = 0f)
        {
            defenseAttempts++;
            if (success)
            {
                defenseSuccesses++;
            }
            
            RecordAction(executionTime);
            
            Debug.Log($"[ActionTracker] 방어 기록: {(success ? "성공" : "실패")} " +
                     $"(성공률: {DefenseSuccessRate:P1}, 시도: {defenseAttempts})");
        }
        
        /// <summary>
        /// 회피 행동 기록
        /// </summary>
        /// <param name="success">회피 성공 여부</param>
        /// <param name="executionTime">실행 시간 (밀리초)</param>
        public void RecordDodge(bool success, float executionTime = 0f)
        {
            dodgeAttempts++;
            if (success)
            {
                dodgeSuccesses++;
            }
            
            RecordAction(executionTime);
            
            Debug.Log($"[ActionTracker] 회피 기록: {(success ? "성공" : "실패")} " +
                     $"(성공률: {DodgeSuccessRate:P1}, 시도: {dodgeAttempts})");
        }
        
        /// <summary>
        /// 공통 행동 통계 업데이트
        /// </summary>
        /// <param name="executionTime">실행 시간</param>
        private void RecordAction(float executionTime)
        {
            totalActions++;
            totalExecutionTime += executionTime;
        }
        
        /// <summary>
        /// 통계 초기화
        /// </summary>
        public void Reset()
        {
            attackAttempts = 0;
            attackSuccesses = 0;
            defenseAttempts = 0;
            defenseSuccesses = 0;
            dodgeAttempts = 0;
            dodgeSuccesses = 0;
            totalActions = 0;
            totalExecutionTime = 0f;
            
            Debug.Log("[ActionTracker] 통계가 초기화되었습니다.");
        }
        
        /// <summary>
        /// 현재 통계 요약 반환
        /// </summary>
        /// <returns>통계 요약 문자열</returns>
        public string GetSummary()
        {
            return $"[ActionTracker 요약]\n" +
                   $"총 행동: {totalActions}\n" +
                   $"공격: {attackSuccesses}/{attackAttempts} ({AttackSuccessRate:P1})\n" +
                   $"방어: {defenseSuccesses}/{defenseAttempts} ({DefenseSuccessRate:P1})\n" +
                   $"회피: {dodgeSuccesses}/{dodgeAttempts} ({DodgeSuccessRate:P1})\n" +
                   $"평균 실행시간: {AverageExecutionTime:F2}ms";
        }
        
        /// <summary>
        /// 다른 ActionTracker와 통계 비교
        /// </summary>
        /// <param name="other">비교할 ActionTracker</param>
        /// <returns>비교 결과 문자열</returns>
        public string CompareTo(ActionTracker other)
        {
            if (other == null) return "비교 대상이 없습니다.";
            
            return $"[통계 비교]\n" +
                   $"공격 성공률: {AttackSuccessRate:P1} vs {other.AttackSuccessRate:P1}\n" +
                   $"방어 성공률: {DefenseSuccessRate:P1} vs {other.DefenseSuccessRate:P1}\n" +
                   $"회피 성공률: {DodgeSuccessRate:P1} vs {other.DodgeSuccessRate:P1}\n" +
                   $"총 행동 수: {TotalActions} vs {other.TotalActions}";
        }
        
        /// <summary>
        /// JSON 직렬화를 위한 데이터 구조체 반환
        /// </summary>
        public ActionTrackerData ToData()
        {
            return new ActionTrackerData
            {
                attackAttempts = this.attackAttempts,
                attackSuccesses = this.attackSuccesses,
                defenseAttempts = this.defenseAttempts,
                defenseSuccesses = this.defenseSuccesses,
                dodgeAttempts = this.dodgeAttempts,
                dodgeSuccesses = this.dodgeSuccesses,
                totalActions = this.totalActions,
                totalExecutionTime = this.totalExecutionTime
            };
        }
        
        /// <summary>
        /// 데이터 구조체에서 통계 복원
        /// </summary>
        public void FromData(ActionTrackerData data)
        {
            attackAttempts = data.attackAttempts;
            attackSuccesses = data.attackSuccesses;
            defenseAttempts = data.defenseAttempts;
            defenseSuccesses = data.defenseSuccesses;
            dodgeAttempts = data.dodgeAttempts;
            dodgeSuccesses = data.dodgeSuccesses;
            totalActions = data.totalActions;
            totalExecutionTime = data.totalExecutionTime;
        }
    }
    
    /// <summary>
    /// ActionTracker 데이터 직렬화용 구조체
    /// </summary>
    [System.Serializable]
    public struct ActionTrackerData
    {
        public int attackAttempts;
        public int attackSuccesses;
        public int defenseAttempts;
        public int defenseSuccesses;
        public int dodgeAttempts;
        public int dodgeSuccesses;
        public int totalActions;
        public float totalExecutionTime;
    }
}

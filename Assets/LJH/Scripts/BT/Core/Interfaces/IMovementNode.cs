using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 이동 특화 노드 인터페이스
    /// 적에게 이동, 거리 유지, 아레나 복귀, 순찰 등에 사용
    /// </summary>
    public interface IMovementNode : IBTNode
    {
        /// <summary>
        /// 목표 위치 계산
        /// 현재 상황에 따른 최적 목표 위치 결정
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>목표 위치</returns>
        Vector3 CalculateTargetPosition(GameObservation observation);
        
        /// <summary>
        /// 이동 방향 계산
        /// 목표 위치를 향한 정규화된 방향 벡터
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>이동 방향 (정규화된 벡터)</returns>
        Vector3 GetMoveDirection(GameObservation observation);
        
        /// <summary>
        /// 정지 거리 반환
        /// 목표 위치까지 이 거리 이내에 도달하면 성공으로 간주
        /// </summary>
        /// <returns>정지 거리</returns>
        float GetStoppingDistance();
        
        /// <summary>
        /// 목표 도달 여부 확인
        /// 현재 위치가 목표 위치에 충분히 가까운지 판단
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>목표 도달 여부</returns>
        bool IsTargetReached(GameObservation observation);
        
        /// <summary>
        /// 이동 속도 반환 (ML 최적화용)
        /// 상황에 따른 적응적 이동 속도
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>이동 속도 배율 (0.0 ~ 1.0)</returns>
        float GetMoveSpeed(GameObservation observation);
        
        /// <summary>
        /// 경로 상의 장애물 확인
        /// 목표 위치까지의 경로에 장애물이 있는지 검사
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>장애물 존재 여부</returns>
        bool HasPathObstacle(GameObservation observation);
        
        /// <summary>
        /// 대안 경로 계산
        /// 직선 경로에 장애물이 있을 때 우회 경로 제공
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>대안 이동 방향</returns>
        Vector3 GetAlternativePath(GameObservation observation);
        
        /// <summary>
        /// 이동 우선순위 반환
        /// 여러 이동 노드가 동시에 실행될 때 우선순위 결정
        /// </summary>
        /// <returns>우선순위 (높을수록 우선)</returns>
        int GetMovementPriority();
        
        /// <summary>
        /// 목표까지의 예상 이동 시간
        /// 경로 계획 및 의사결정에 활용
        /// </summary>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>예상 이동 시간 (초)</returns>
        float GetEstimatedTravelTime(GameObservation observation);
        
        /// <summary>
        /// 이동 중 회피 행동 허용 여부
        /// 이동 중 적의 공격을 회피할 수 있는지 확인
        /// </summary>
        /// <returns>회피 허용 여부</returns>
        bool AllowEvasionDuringMovement();
        
        /// <summary>
        /// 정지 거리 설정 (ML 최적화용)
        /// 학습을 통해 상황별 최적 정지 거리 조정
        /// </summary>
        /// <param name="distance">새로운 정지 거리</param>
        void SetStoppingDistance(float distance);
        
        /// <summary>
        /// 이동 파라미터 조정 (ML 최적화용)
        /// 속도, 회전 속도, 가속도 등 조정
        /// </summary>
        /// <param name="parameters">새로운 이동 파라미터들</param>
        void SetMovementParameters(float[] parameters);
        
        /// <summary>
        /// 현재 이동 파라미터 반환
        /// </summary>
        /// <returns>현재 이동 파라미터들</returns>
        float[] GetMovementParameters();
        
        /// <summary>
        /// 이동 영역 제한 확인
        /// 아레나 경계 등 이동 가능 영역 내에서만 이동
        /// </summary>
        /// <param name="targetPosition">확인할 목표 위치</param>
        /// <param name="observation">현재 게임 상황 정보</param>
        /// <returns>이동 가능 여부</returns>
        bool IsValidMoveTarget(Vector3 targetPosition, GameObservation observation);
    }
}
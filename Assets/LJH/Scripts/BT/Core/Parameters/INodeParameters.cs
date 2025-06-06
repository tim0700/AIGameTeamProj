using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 모든 노드 파라미터가 구현해야 하는 기본 인터페이스
    /// ML 학습과 런타임 파라미터 조정을 위한 공통 기능 제공
    /// </summary>
    public interface INodeParameters
    {
        /// <summary>
        /// 파라미터 이름 (디버깅 및 식별용)
        /// </summary>
        string GetParameterName();
        
        /// <summary>
        /// 파라미터 설명 (UI 표시용)
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// 파라미터 유효성 검사
        /// </summary>
        /// <returns>유효성 검사 결과</returns>
        bool IsValid();
        
        /// <summary>
        /// 파라미터를 기본값으로 리셋
        /// </summary>
        void ResetToDefault();
        
        /// <summary>
        /// 파라미터를 배열로 변환 (ML 최적화용)
        /// </summary>
        /// <returns>파라미터 값들의 배열</returns>
        float[] ToArray();
        
        /// <summary>
        /// 배열에서 파라미터 값 설정 (ML 최적화용)
        /// </summary>
        /// <param name="values">새로운 파라미터 값들</param>
        void FromArray(float[] values);
        
        /// <summary>
        /// 파라미터 개수 반환
        /// </summary>
        /// <returns>파라미터 개수</returns>
        int GetParameterCount();
        
        /// <summary>
        /// 파라미터 제약조건 반환
        /// </summary>
        /// <returns>제약조건 정보</returns>
        ParameterConstraints GetConstraints();
        
        /// <summary>
        /// 파라미터 복사 생성
        /// </summary>
        /// <returns>복사된 파라미터 객체</returns>
        INodeParameters Clone();
    }
    
    /// <summary>
    /// 파라미터 제약조건 정보
    /// </summary>
    [System.Serializable]
    public struct ParameterConstraints
    {
        public float[] minValues;    // 최소값들
        public float[] maxValues;    // 최대값들
        public bool[] isInteger;     // 정수 여부
        public string[] paramNames;  // 파라미터 이름들
        
        public static ParameterConstraints Create(int paramCount)
        {
            return new ParameterConstraints
            {
                minValues = new float[paramCount],
                maxValues = new float[paramCount],
                isInteger = new bool[paramCount],
                paramNames = new string[paramCount]
            };
        }
        
        /// <summary>
        /// 값이 제약조건을 만족하는지 확인
        /// </summary>
        public bool IsValidValue(int index, float value)
        {
            if (index < 0 || index >= minValues.Length) return false;
            
            if (value < minValues[index] || value > maxValues[index]) return false;
            
            if (isInteger[index] && value != Mathf.Round(value)) return false;
            
            return true;
        }
        
        /// <summary>
        /// 값을 제약조건에 맞게 클램핑
        /// </summary>
        public float ClampValue(int index, float value)
        {
            if (index < 0 || index >= minValues.Length) return value;
            
            float clampedValue = Mathf.Clamp(value, minValues[index], maxValues[index]);
            
            if (isInteger[index])
            {
                clampedValue = Mathf.Round(clampedValue);
            }
            
            return clampedValue;
        }
    }
}

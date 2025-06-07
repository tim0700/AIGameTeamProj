using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [Header("무기 데미지 정보")]
    public float damage = 20f;
    public AgentController owner; // 무기를 소유한 에이전트

    /// <summary>
    /// 무기 데미지 설정
    /// </summary>
    public void SetDamage(float damageAmount, AgentController weaponOwner)
    {
        damage = damageAmount;
        owner = weaponOwner;
    }

    /// <summary>
    /// 현재 설정된 데미지 반환
    /// </summary>
    public float GetDamage()
    {
        return damage;
    }

    /// <summary>
    /// 무기 소유자 반환
    /// </summary>
    public AgentController GetOwner()
    {
        return owner;
    }
}

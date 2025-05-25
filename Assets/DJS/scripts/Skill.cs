using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Skill : MonoBehaviour
{
	[SerializeField]
	private	string			skillName;				// 해당 스킬 이름
	[SerializeField]
	private	float			maxCooldownTime;		// 해당 스킬 재사용 대기 시간
	[SerializeField]
	private	Image			imageCooldownTime;		// 재사용 대기 시간을 이미지로 출력하는 Image UI

	private	float			currentCooldownTime;	// 현재 재사용 대기 시간
	private	bool			isCooldown;             // 현재 쿨타임이 적용중인지 체크
	
	private void Awake()
	{
		SetCooldownIs(false);
		isCooldown = false;

    }

	/// <summary>
	/// 외부에서 스킬을 사용할 때 호출하는 메소드
	/// </summary>
	public void UseSkill()
	{
		if (!isCooldown)
		{
			StartCoroutine(nameof(OnCooldownTime), maxCooldownTime);
		}
	}

	/// <summary>
	/// 실제 스킬의 재사용 대기 시간을 제어하는 코루틴 메소드
	/// </summary>
	private IEnumerator OnCooldownTime(float maxCooldownTime)
	{
		// 스킬 재사용 대기 시간 저장
		currentCooldownTime = maxCooldownTime;

		SetCooldownIs(true);

		while ( currentCooldownTime > 0 )
		{
			currentCooldownTime -= Time.deltaTime;
			// Image UI의 fiilAmount를 조절해 채워지는 이미지 모양 설정
			imageCooldownTime.fillAmount = currentCooldownTime/maxCooldownTime;
			isCooldown = true;
			yield return null;
		}

		SetCooldownIs(false);
	}

	private void SetCooldownIs(bool boolean)
	{
		isCooldown					= boolean;
		imageCooldownTime.enabled	= boolean;
	}
}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillSystem : MonoBehaviour
{
	[SerializeField]
	private	GraphicRaycaster	graphicRaycaster;
	[SerializeField]
	private	Skill[]				skills;

	private	List<RaycastResult>	raycastResults;
	private	PointerEventData	pointerEventData;

	private void Awake()
	{
		raycastResults	 = new List<RaycastResult>();
		pointerEventData = new PointerEventData(null);
	}

	private void Update()
	{
		if ( !Input.anyKeyDown ) return;

		// 1 ~ skills.Length 숫자키를 눌러 스킬 시전
		if ( int.TryParse(Input.inputString, out int key) && (key >= 1 && key <= skills.Length) )
		{
			skills[key-1].UseSkill();
		}
	}
}


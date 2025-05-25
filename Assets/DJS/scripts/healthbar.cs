using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthBar;                // UI Image (fillAmount 조절 대상)
    public float maxHealth = 100f;         // 최대 체력
    private float currentHealth;           // 현재 체력
    public float animationDuration = 0.5f; // 애니메이션 지속 시간(초)

    void Start()
    {
        // 시작할 때 현재 체력을 최대 체력으로 초기화하고 즉시 UI에 반영
        currentHealth = maxHealth;
        healthBar.fillAmount = 1f;

    }

    void Update()
    {
        // E 키를 누르면 체력을 10만큼 감소
        if (Input.GetKeyDown(KeyCode.E))
        {
            ChangeHealth(-10f);
        }
    }

    void ChangeHealth(float delta)
    {
        // 감소 전 체력 비율
        float fromFraction = currentHealth / maxHealth;

        // 체력 값을 delta만큼 조정하고 0~maxHealth 범위로 Clamp
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, maxHealth);

        // 감소 후 체력 비율
        float toFraction = currentHealth / maxHealth;

        // 혹시 이전에 실행 중이던 코루틴이 있으면 중단
        StopAllCoroutines();

        // 부드럽게 채워지는 코루틴 시작
        StartCoroutine(AnimateHealthBar(fromFraction, toFraction));
    }

    IEnumerator AnimateHealthBar(float from, float to)
    {
        float elapsed = 0f;

        // elapsed가 animationDuration에 도달할 때까지 매 프레임마다 보간
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            healthBar.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }

        // 최종적으로 정확히 목표치에 맞춤
        healthBar.fillAmount = to;
    }
}

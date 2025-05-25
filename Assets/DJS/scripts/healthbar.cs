using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthBar;                // UI Image (fillAmount ���� ���)
    public float maxHealth = 100f;         // �ִ� ü��
    private float currentHealth;           // ���� ü��
    public float animationDuration = 0.5f; // �ִϸ��̼� ���� �ð�(��)

    void Start()
    {
        // ������ �� ���� ü���� �ִ� ü������ �ʱ�ȭ�ϰ� ��� UI�� �ݿ�
        currentHealth = maxHealth;
        healthBar.fillAmount = 1f;

    }

    void Update()
    {
        // E Ű�� ������ ü���� 10��ŭ ����
        if (Input.GetKeyDown(KeyCode.E))
        {
            ChangeHealth(-10f);
        }
    }

    void ChangeHealth(float delta)
    {
        // ���� �� ü�� ����
        float fromFraction = currentHealth / maxHealth;

        // ü�� ���� delta��ŭ �����ϰ� 0~maxHealth ������ Clamp
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, maxHealth);

        // ���� �� ü�� ����
        float toFraction = currentHealth / maxHealth;

        // Ȥ�� ������ ���� ���̴� �ڷ�ƾ�� ������ �ߴ�
        StopAllCoroutines();

        // �ε巴�� ä������ �ڷ�ƾ ����
        StartCoroutine(AnimateHealthBar(fromFraction, toFraction));
    }

    IEnumerator AnimateHealthBar(float from, float to)
    {
        float elapsed = 0f;

        // elapsed�� animationDuration�� ������ ������ �� �����Ӹ��� ����
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            healthBar.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }

        // ���������� ��Ȯ�� ��ǥġ�� ����
        healthBar.fillAmount = to;
    }
}

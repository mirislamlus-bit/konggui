using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ChapterTitleFade : MonoBehaviour
{
    [SerializeField] private float visibleSeconds = 3f;
    [SerializeField] private float fadeSeconds = 1f;

    private TextMeshProUGUI titleText;

    private void Awake()
    {
        titleText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (titleText == null)
        {
            titleText = GetComponent<TextMeshProUGUI>();
        }

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (titleText == null)
        {
            yield break;
        }

        Color color = titleText.color;
        color.a = 0.82f;
        titleText.color = color;

        yield return new WaitForSeconds(visibleSeconds);

        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.82f, 0f, elapsed / fadeSeconds);
            titleText.color = color;
            yield return null;
        }

        color.a = 0f;
        titleText.color = color;
    }
}

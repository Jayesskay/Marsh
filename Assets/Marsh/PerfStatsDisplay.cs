using System.Collections;
using TMPro;
using UnityEngine;

public class PerfStatsDisplay : MonoBehaviour
{
    TMP_Text _text;

    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        StartCoroutine(UpdateText());
    }

    private IEnumerator UpdateText()
    {
        while (true)
        {
            var dt = Time.deltaTime;
            var ms = dt * 1000.0;
            var fps = 1.0 / dt;
            _text.SetText($"FPS: {fps:#}({ms:#.##} ms)");
            yield return new WaitForSeconds(0.1f);
        }
    }
}

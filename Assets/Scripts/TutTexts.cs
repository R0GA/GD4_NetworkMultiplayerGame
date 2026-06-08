using UnityEngine;
using TMPro;
using System.Collections;

public class TextFader : MonoBehaviour
{
    public float zoomDuration;   
    public float displayTime;     
    public float fadeDuration;     

    public Vector3 startScale = Vector3.zero;       
    public Vector3 targetScale = Vector3.one;       

    private TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        transform.localScale = startScale;        
        StartCoroutine(ZoomThenFade());
    }

    IEnumerator ZoomThenFade()
    {
      
        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);  
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            yield return null;
        }
        transform.localScale = targetScale;

       
        yield return new WaitForSeconds(displayTime);

    
        elapsed = 0f;
        Color originalColor = tmp.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
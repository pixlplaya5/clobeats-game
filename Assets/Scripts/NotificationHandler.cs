using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationHandler : MonoBehaviour
{
    public float showingYPosition = -300f;
    public float showingXPosition = 472f;
    public float downHidingYPosition = -410f;
    public float rightHidingXPosition = 800f;
    public float showSpeed = 5f;
    public string currentNotifText = "";
    public TMPro.TextMeshProUGUI textMesh;
    public enum RiseDirection
    {
        Up,
        Down,
        UpFromRight,
        DownToRight,
        DownToBottom
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        textMesh.text = currentNotifText;
    }
    public void SetNotifText(string text)
    {
        currentNotifText = text;
    }
    public void ShowNotif()
    {
        StartCoroutine(RiseNotif(RiseDirection.Up));
    }
    public void HideNotif()
    {
        StartCoroutine(RiseNotif(RiseDirection.Down));
    }
    public IEnumerator ShowNotifImmediatelyWithTimeout(string text, float timeout)
    {
        SetNotifText(text);
        ShowNotif();
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        HideNotif();
    }
    public IEnumerator RiseNotif(RiseDirection direction)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 shownPosition = new Vector2(showingXPosition, showingYPosition);
        Vector2 bottomPosition = new Vector2(showingXPosition, downHidingYPosition);
        Vector2 rightPosition = new Vector2(rightHidingXPosition, downHidingYPosition);
        Vector2 startPosition;
        Vector2 targetPosition;

        switch (direction)
        {
            case RiseDirection.Up:
                startPosition = bottomPosition;
                targetPosition = shownPosition;
                break;
            case RiseDirection.Down:
                startPosition = rectTransform.anchoredPosition;
                targetPosition = new Vector2(startPosition.x, downHidingYPosition);
                break;
            case RiseDirection.UpFromRight:
                startPosition = rightPosition;
                targetPosition = shownPosition;
                break;
            case RiseDirection.DownToRight:
                startPosition = rectTransform.anchoredPosition;
                targetPosition = rightPosition;
                break;
            case RiseDirection.DownToBottom:
                startPosition = shownPosition;
                targetPosition = bottomPosition;
                break;
            default:
                yield break;
        }

        rectTransform.anchoredPosition = startPosition;
        float duration = showSpeed > 0f ? 1f / showSpeed : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
}

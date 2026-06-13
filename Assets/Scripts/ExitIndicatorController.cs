using UnityEngine;

public class ExitIndicatorController : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private GameObject indicatorSprite;

    [Header("Animation")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomAmount = 0.08f;

    private Vector3 baseScale;
    private bool activated;

    private void Awake()
    {
        if (indicatorSprite != null)
        {
            baseScale = indicatorSprite.transform.localScale;
            indicatorSprite.SetActive(false);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (!activated && GameManager.Instance.HasObjective())
        {
            ActivateIndicator();
        }

        if (activated)
        {
            AnimateIndicator();
        }
    }

    private void ActivateIndicator()
    {
        activated = true;

        if (indicatorSprite != null)
            indicatorSprite.SetActive(true);
    }

    private void AnimateIndicator()
    {
        if (indicatorSprite == null)
            return;

        float pulse =
            1f + Mathf.Sin(Time.time * zoomSpeed) * zoomAmount;

        indicatorSprite.transform.localScale =
            baseScale * pulse;
    }
}
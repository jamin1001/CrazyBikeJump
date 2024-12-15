using UnityEngine;

public class Scaler : MonoBehaviour
{
    [Tooltip("Shape of scale.")]
    public AnimationCurve ScaleCurve;

    [Tooltip("Enabling this goes from 0 to 1 instead of the default 1 to 0 represented in the curve picture. If not checked, Begin to End will be reversed.")]
    public bool FlipCurve;
    
    [Tooltip("How fast it scales.")]
    public float ScaleSpeed = 0.1f;

    [Tooltip("How much it will be scaled at the beginning.")]
    public Vector3 ScaleBegin = Vector3.zero;
    
    [Tooltip("How much it will be scaled at the end.")]
    public Vector3 ScaleEnd = Vector3.zero;

    float normalValue;

    void OnEnable()
    {
        normalValue = 0;
        if (FlipCurve)
            transform.localScale = ScaleEnd;
        else
            transform.localScale = ScaleBegin;
    }

    void Update()
    {
        normalValue += ScaleSpeed * Time.deltaTime;
        if (normalValue > 1f)
            normalValue = 1f;

        float curveValue = ScaleCurve.Evaluate(normalValue);

        float adjustedValue;
        if (FlipCurve)
            adjustedValue = 1f - curveValue;
        else
            adjustedValue = curveValue;

        Vector3 adjustedScale = Vector3.Lerp(ScaleBegin, ScaleEnd, adjustedValue);

        // Note: there is only local, no global scale possible in Unity, for reasons.
        transform.localScale = adjustedScale;

        //Debug.Log($"Normal is: {normalValue}, Adjusted is: {adjustedValue}, Curve is: {curveValue}, AdjustedScale is: {adjustedScale}");
    }
}

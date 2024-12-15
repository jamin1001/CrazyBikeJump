using UnityEngine;

public class Translater : MonoBehaviour
{
    public enum SpaceEnum { Local, World };

    [Tooltip("Whether it rotates translates to local frame or world frame.")]
    public SpaceEnum TranslateSpace;

    [Tooltip("Shape of scale.")]
    public AnimationCurve ScaleCurve;

    [Tooltip("Enabling this goes from 0 to 1 instead of the default 1 to 0 represented in the curve picture. If not checked, Begin to End will be reversed.")]
    public bool FlipCurve;

    [Tooltip("Position interpreted with respect to current position.")]
    public bool RelativeToCurrent;

    [Tooltip("How fast it scales.")]
    public float TranslateSpeed = 0.1f;

    [Tooltip("How much it will be scaled at the end.")]
    public Vector3 PositionBegin = Vector3.zero;

    [Tooltip("How much it will be scaled at the end.")]
    public Vector3 PositionEnd = Vector3.zero;

    float normalValue;
    Vector3 originalLocalPosition;

    void OnEnable()
    {
        normalValue = 0;
        originalLocalPosition = transform.localPosition;
        if (FlipCurve)
        {
            if (TranslateSpace == SpaceEnum.Local)
                transform.localPosition = originalLocalPosition;
            else
                transform.position = PositionBegin;
        }
        else
        {
            if (TranslateSpace == SpaceEnum.Local)
                transform.localPosition = originalLocalPosition + PositionEnd;
            else
                transform.position = PositionEnd;
        }
    }

    void Update()
    {
        normalValue += TranslateSpeed * Time.deltaTime;
        if (normalValue > 1f)
            normalValue = 1f;

        float curveValue = ScaleCurve.Evaluate(normalValue);

        float adjustedValue;
        if (FlipCurve)
            adjustedValue = 1f - curveValue;
        else
            adjustedValue = curveValue;

        Vector3 adjustedTranslation = Vector3.Lerp(PositionBegin, PositionEnd, adjustedValue);

        if (RelativeToCurrent)
        {
            if (TranslateSpace == SpaceEnum.Local)
                transform.localPosition = originalLocalPosition + adjustedTranslation;
            else
                transform.position = originalLocalPosition + adjustedTranslation;
        }
        else
             if (TranslateSpace == SpaceEnum.Local)
            transform.localPosition = adjustedTranslation;
        else
            transform.position = adjustedTranslation;

        //Debug.Log($"Normal is: {normalValue}, Adjusted is: {adjustedValue}, Curve is: {curveValue}, AdjustedTranslation is: {adjustedTranslation}");
    }
}

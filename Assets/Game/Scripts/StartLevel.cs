using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StartLevel : MonoBehaviour
{
    static float DelayBetweenCharacters = 0.02f;
    static WaitForSeconds WaitCharacter = new WaitForSeconds(DelayBetweenCharacters);
    static WaitForSeconds WaitDisappear = new WaitForSeconds(2f);

    public TMP_Text TextMeshProText;
    string _textToAnimate;

    private void Awake()
    {
        TextMeshProText.text = "";
        _textToAnimate = "";
        //TextMeshProText.ForceMeshUpdate();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(TextMeshProText.rectTransform);
    }
    public void StartAnimatedText(string newText)
    {
        _textToAnimate = newText;
        StartCoroutine(AnimateText());
    }

    IEnumerator AnimateText()
    {
        for (int i = 0; i < _textToAnimate.Length; i++)
        {
            TextMeshProText.text += _textToAnimate[i];

            // Get the current color of the last character
            Color lastCharColor = TextMeshProText.textInfo.characterInfo[i].color;

            // Fade in the current character
            for (float t = 0; t < 1; t += Time.deltaTime / DelayBetweenCharacters)
            {
                Color lerpedColor = Color.Lerp(Color.clear, lastCharColor, t);
                TextMeshProText.textInfo.characterInfo[i].color = lerpedColor;
                TextMeshProText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }

            yield return WaitCharacter;// new WaitForSeconds(DelayBetweenCharacters);
        }

        yield return WaitDisappear;
        TextMeshProText.text = "";
        _textToAnimate = "";
    }
}
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Experimental.GraphView;
using System.Timers;

public class GameGui : MonoBehaviour
{
    static float DelayBetweenCharacters = 0.02f;
    static WaitForSeconds WaitCharacter = new WaitForSeconds(DelayBetweenCharacters);
    static WaitForSeconds WaitDisappear = new WaitForSeconds(2f);

    public TMP_Text TextMeshProText;
    string _textToAnimate;

    public Image[] StarsOff = new Image[3];
    public Image[] StarsOn  = new Image[3];
    public TMP_Text[] StarsCount = new TMP_Text[3];

    public Image[] FlagsOff = new Image[3];
    public Image[] FlagsOn = new Image[3];
    public TMP_Text[] FlagsCount = new TMP_Text[3];

    public TMP_Text RaceTime;
    public TMP_Text ExtraStarPlus;
    public Image ExtraStar;

    private float elapsedSeconds;
    private bool isTimeStopped = false;

    private void Awake()
    {
        TextMeshProText.text = "";
        _textToAnimate = "";
        //TextMeshProText.ForceMeshUpdate();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(TextMeshProText.rectTransform);
    }

    private void Update()
    {
        if (!isTimeStopped)
        {
            float dt = Time.deltaTime;

            if ((int)(elapsedSeconds + dt) > (int)elapsedSeconds)
            {
                elapsedSeconds += dt;
                UpdateRaceTime(elapsedSeconds);
            }
            else
            {
                elapsedSeconds += dt;
            }
        }
    }

    public void StopTime()
    {
        isTimeStopped = true;
    }

    private void UpdateRaceTime(float totalRaceSeconds)
    {
        int totalSeconds = (int)totalRaceSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        string minutesText = minutes > 0 ? minutes.ToString() : "";
        RaceTime.text = $"{minutesText}:{seconds:00}";
    }

    public void ResetRaceTime()
    {
        elapsedSeconds = 0;
        UpdateRaceTime(0);
        isTimeStopped = false;
    }

    public void ShowStar(int kind, int count)
    {
        if (count == 0)
        {
            StarsOff[kind].gameObject.SetActive(true);
            StarsOn[kind].gameObject.SetActive(false);
            StarsCount[kind].gameObject.SetActive(false);
        }
        else
        {
            StarsOff[kind].gameObject.SetActive(false);
            StarsOn[kind].gameObject.SetActive(true);

            if (count == 1)
            {
                StarsCount[kind].gameObject.SetActive(false);
            }
            else
            {
                StarsCount[kind].gameObject.SetActive(true);
                StarsCount[kind].text = count.ToString();
            }
        }
    }

    public void ShowFlag(int kind, int count)
    {
        if (count == 0)
        {
            FlagsOff[kind].gameObject.SetActive(true);
            FlagsOn[kind].gameObject.SetActive(false);
            FlagsCount[kind].gameObject.SetActive(false);
        }
        else
        {
            FlagsOff[kind].gameObject.SetActive(false);
            FlagsOn[kind].gameObject.SetActive(true);

            if (count == 1)
            {
                FlagsCount[kind].gameObject.SetActive(false);
            }
            else
            {
                FlagsCount[kind].gameObject.SetActive(true);
                FlagsCount[kind].text = count.ToString();
            }
        }
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
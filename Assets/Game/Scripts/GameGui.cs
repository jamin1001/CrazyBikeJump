using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class GameGui : MonoBehaviour
{
    static float DelayBetweenCharacters = 0.02f;
    static WaitForSeconds WaitCharacter = new WaitForSeconds(DelayBetweenCharacters);
    static WaitForSeconds WaitDisappear = new WaitForSeconds(2f);

    public Transform GuiPoolFolder;

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
    public Image[] ExtraStars = new Image[3];

    public RawImage SpinningCoin;

    private float elapsedRaceSeconds;
    private bool isRaceTimeStopped = false;
    private int extraStar = -1;
    private bool isTransactingAtm = false;

    const int ATM_TRANSACT_POOL = 10;

    public float AtmNextStarFlagTimeout = 0.5f;
    public float AtmTravelSpeed = 10f;

    
    private Image[,] atmTransferImages = new Image[6, ATM_TRANSACT_POOL]; // (StarFlagIndex) x 10
    private int[,] atmTransferStates = new int[6, 2];                     // (StarFlagIndex) x (startIndex, endIndex)
    private readonly int[] IndexToStarFlag = new int[] { 2, 3, 1, 4, 0, 5 };
    private readonly Image[] StarFlagImages = new Image[6];
    private float starFlagElapsed = 0f;
    private int starFlagIndex = 0;

    // indexToStarFlag:
    // stars (b s g)   flags (y r b)
    // 0 1 2           3 4 5   
    // 4 2 0           1 3 5

    private void Awake()
    {
        TextMeshProText.text = "";
        _textToAnimate = "";
        //TextMeshProText.ForceMeshUpdate();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(TextMeshProText.rectTransform);

        StarFlagImages[0] = StarsOn[0];
        StarFlagImages[1] = StarsOn[1];
        StarFlagImages[2] = StarsOn[2];
        StarFlagImages[3] = FlagsOn[0];
        StarFlagImages[4] = FlagsOn[1];
        StarFlagImages[5] = FlagsOn[2];

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < ATM_TRANSACT_POOL; j++)
            {
                atmTransferImages[i, j] = Instantiate<Image>(StarFlagImages[i], GuiPoolFolder);

                /*
                if (i == 0)
                    atmTransferImages[i, j] = Instantiate<Image>(StarsOn[0]);
                else if (i == 1)
                    atmTransferImages[i, j] = Instantiate<Image>(StarsOn[1]);
                else if (i == 2)
                    atmTransferImages[i, j] = Instantiate<Image>(StarsOn[2]);
                else if (i == 3)
                    atmTransferImages[i, j] = Instantiate<Image>(FlagsOn[0]);
                else if (i == 4)
                    atmTransferImages[i, j] = Instantiate<Image>(FlagsOn[1]);
                else if (i == 5)
                    atmTransferImages[i, j] = Instantiate<Image>(FlagsOn[2]);
                */
            }
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (!isRaceTimeStopped)
        {
            int seconds = (int)elapsedRaceSeconds;
            float nextElapsedSeconds = elapsedRaceSeconds + dt;
            int nextSeconds = (int)nextElapsedSeconds;

            if (nextSeconds > seconds)
            {
                UpdateRaceTime(nextSeconds);
                UpdateExtraStar(nextSeconds);
            }
            elapsedRaceSeconds = nextElapsedSeconds;
        }

        if(isTransactingAtm)
        {
            // Shoot out the next star or flag.
            if(starFlagElapsed > AtmNextStarFlagTimeout)
            {
                starFlagElapsed -= AtmNextStarFlagTimeout;

                int nextCount = 0;
                while(nextCount < 6) // Keep going until we went through them all.
                {
                    int starFlag = IndexToStarFlag[starFlagIndex];

                    if(Game.Inst.StarFlagCount(starFlag) > 0) // Still a star/flag to transfer.
                    {
                        int extendIndex = -1;
                        if (atmTransferStates[starFlag, 0] == -1)
                        {
                            Debug.Assert(atmTransferStates[starFlag, 1] == -1);

                            atmTransferStates[starFlag, 0] = 0;
                            atmTransferStates[starFlag, 1] = 0;
                            extendIndex = 0;
                        }
                        else
                        {
                            atmTransferStates[starFlag, 1] = (atmTransferStates[starFlag, 1] + 1) % ATM_TRANSACT_POOL;
                            extendIndex = atmTransferStates[starFlag, 1];

                            if(extendIndex == atmTransferStates[starFlag, 0])
                            {
                                Debug.LogError("Cannot start another star/flag because pool is full - exiting early.");
                                return;
                            }
                        }

                        // Turn on the next one on in the circular queue.
                        atmTransferImages[starFlag, extendIndex].transform.position = StarFlagImages[starFlag].transform.position; // start from original position
                        atmTransferImages[starFlag, extendIndex].gameObject.SetActive(true);

                    }

                    starFlagIndex = (starFlagIndex + 1) % 6;
                    nextCount++;
                }
            }

            int transactingCount = 0;
            Vector3 targetPos = SpinningCoin.transform.position;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < ATM_TRANSACT_POOL; j++)
                {
                    // All active towards SpinningCoin!
                    GameObject starFlagOb = atmTransferImages[i, j].gameObject;
                    
                    if (starFlagOb.activeSelf)
                    {
                        Vector3 startPos = starFlagOb.transform.position;
                        starFlagOb.transform.position = Vector3.Lerp(startPos, targetPos, AtmTravelSpeed * dt);

                        if((starFlagOb.transform.position - targetPos).sqrMagnitude < Vector3.kEpsilon)
                        {
                            // Close enough, now do the tabulation and update the list.
                            starFlagOb.SetActive(false);
                            atmTransferStates[i, 0] = (atmTransferStates[i, 0] + 1) % 6;
                        }
                        else
                        {
                            transactingCount++;
                        }
                    }
                }
            }

            if (transactingCount == 0)
            {
                // All done, nothing left to transact.
                isTransactingAtm = false;
                if (starFlagElapsed == 0)
                {
                    // Already at zero, yet we never counted! Means that there was nothing to transact, play a buzz or something.
                    Game.Inst.PlayAnimal(Game.Inst.CowHitClip);
                }
                else
                {
                    // Reset.
                    starFlagElapsed = 0;
                }
            }
            else
            {
                starFlagElapsed += Time.deltaTime;
            }
        }
    }

    public void ResetAtm()
    {
        for (int i = 0; i < 6; i++)
        {
            // Reset ranges.
            atmTransferStates[i, 0] = -1;
            atmTransferStates[i, 1] = -1;
            for (int j = 0; j < ATM_TRANSACT_POOL; j++)
            {
                atmTransferImages[i, j].gameObject.SetActive(false);
            }
        }
    }

    public void TransactAtm()
    {
        isTransactingAtm = true;
    }

    public void StopTime()
    {
        isRaceTimeStopped = true;
    }

    void UpdateExtraStar(int currentSeconds)
    {
        if (extraStar >= 0 && extraStar <= 2)
        {
            if (currentSeconds == Game.Inst.GetExtraStarSeconds(extraStar) + 1) // for example, if gold is set at 5, then at 6 seconds turn to silver
            {
                ExtraStars[extraStar].gameObject.SetActive(false);
                extraStar--;

                if(extraStar > -1)
                    ExtraStars[extraStar].gameObject.SetActive(true); // Active new one (if any left)
                else
                    ExtraStarPlus.gameObject.SetActive(false); // No more stars, so hide it.
            }
        }
    }

    private void UpdateRaceTime(int currentSeconds)
    {
        int minutes = currentSeconds / 60;
        int seconds = currentSeconds % 60;
        string minutesText = minutes > 0 ? minutes.ToString() : "";
        RaceTime.text = $"{minutesText}:{seconds:00}";
    }

    public void ResetRaceTime()
    {
        extraStar = 2;
        ExtraStars[0].gameObject.SetActive(false);
        ExtraStars[1].gameObject.SetActive(false);
        ExtraStars[2].gameObject.SetActive(true);
        ExtraStarPlus.gameObject.SetActive(true);

        elapsedRaceSeconds = 0;
        UpdateRaceTime(0);
        isRaceTimeStopped = false;
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
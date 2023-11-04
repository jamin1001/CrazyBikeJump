using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class GameGui : MonoBehaviour
{
    public GameObject SubStar;

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
    public TMP_Text SpinningCoinCount;

    private float elapsedRaceSeconds;
    private bool isRaceTimeStopped = false;
    private int extraStar = -1;
    private int atmTransactionsRemaining = 0;

    const int ATM_TRANSACT_POOL = 4;

    public float AtmNextStarFlagTimeout = 0.5f;
    public float AtmTravelSpeed = 10f;
    public float LoseTravelSpeed = 200f;
    public float CoinSpinAccel = 60f;
    public float CoinSpinDecel = 0.1f;

    public float StarFlagWiggleSpeed = 3.0f;
    public float StarFlagWiggleAmplitude = 5.0f;
    public float StarFlagWiggleDuration = 5;
    public float LosingTransactionFallingTimeout = 2.0f;

    private float starFlagWiggleDistance = 0f;
    private float[] originalWigglePositions = new float[6];

    private Image[,] atmTransferImages = new Image[6, ATM_TRANSACT_POOL]; // (StarFlagIndex) x 10
    private int[,] atmTransferStates = new int[6, 2];                     // (StarFlagIndex) x (startIndex, endIndex)
    private readonly int[] IndexToStarFlag = new int[] { 0, 3, 1, 4, 2, 5 };
    private readonly Image[] StarFlagImages = new Image[6];
    private float starFlagElapsed = 0f;
    private int starFlagIndex = 0;
    private float coinSpinSpeed = 0;
    private float coinSpinDistance = 0;
    private int coinSpinIndex = 0;
    private float[,] coinSpinUvs = new float[4, 2] {
        { 0.0f, 0.5f },
        { 0.5f, 0.5f },
        { 0.0f, 0.0f },
        { 0.5f, 0.0f },
    };



    // indexToStarFlag:
    // stars (b s g)   flags (y r b)
    // 0 1 2           3 4 5   
    // 0 2 4           1 3 5

    private bool losingTransaction = false;
    private bool losingTransactionFalling = false;
   
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
            originalWigglePositions[i] = StarFlagImages[i].transform.position.x;

            for (int j = 0; j < ATM_TRANSACT_POOL; j++)
            {
                atmTransferImages[i, j] = Instantiate<Image>(StarFlagImages[i], GuiPoolFolder);
                atmTransferImages[i, j].gameObject.SetActive(false);
            }
        }

        SpinningCoinCount.text = Game.Inst.CoinCount().ToString();
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

        if(atmTransactionsRemaining > 0)
        {
            //Debug.LogError("star child 0 world pos is: " + atmTransferImages[0, 0].transform.GetChild(0).position);

            if (losingTransaction)
            {
                starFlagWiggleDistance += StarFlagWiggleSpeed * dt;

                if (starFlagWiggleDistance > StarFlagWiggleDuration)
                {
                    //Debug.LogError($"Stars Done falling: {StarFlagWiggleSpeed}, {starFlagWiggleDistance}, {StarFlagWiggleDuration}");
                    for (int i = 0; i < 6; i++)
                    {
                        float resetPos = originalWigglePositions[i];
                        StarFlagImages[i].transform.position = new Vector3(resetPos, StarFlagImages[i].transform.position.y, 0);
                    }
                    starFlagWiggleDistance = 0;
                }
                else
                {
                    float wavyOffset = StarFlagWiggleAmplitude * Mathf.Sin(starFlagWiggleDistance);
                    // First wiggle them, then follow the same rules as ATM but a little different (no redemption or towards coins, just fall).
                    for (int i = 0; i < 6; i++)
                    {
                        float wavyPos = originalWigglePositions[i] + wavyOffset;
                        Vector3 existingPos = StarFlagImages[i].transform.position;
                        StarFlagImages[i].transform.position = new Vector3(wavyPos, existingPos.y, 0);
                    }
                }

                if(starFlagElapsed > LosingTransactionFallingTimeout)
                {
                    starFlagElapsed = AtmNextStarFlagTimeout;
                    losingTransactionFalling = true;
                }
            }
            

            if(!losingTransaction || losingTransactionFalling)
            {
                // Shoot out the next star or flag.
                if (starFlagElapsed >= AtmNextStarFlagTimeout)
                {
                    starFlagElapsed -= AtmNextStarFlagTimeout;

                    int nextCount = 0;
                    while (nextCount < 6) // Keep going until we went through them all.
                    {
                        int starFlag = IndexToStarFlag[starFlagIndex];

                        if (Game.Inst.StarFlagCount(starFlag) > 0) // Still a star/flag to transfer.
                        {
                            // Turn on the back of the queue, and advance it for next time.
                            int newImageIndexToActivate = atmTransferStates[starFlag, 1];
                            atmTransferImages[starFlag, newImageIndexToActivate].transform.position = StarFlagImages[starFlag].transform.position; // start from original position
                            atmTransferImages[starFlag, newImageIndexToActivate].gameObject.SetActive(true);

                            atmTransferStates[starFlag, 1] = (newImageIndexToActivate + 1) % ATM_TRANSACT_POOL;
                            Debug.LogWarning($"Start transfer starFlag={starFlag} ({atmTransferStates[starFlag, 0]},{atmTransferStates[starFlag, 1]})");

                            // Deduct the star from what the game is counting.
                            Game.Inst.TransactStarFlag(starFlag);
                            //Debug.Log("Transacted starFlag index: " + starFlag);


                            // We are only transfering one star/flag at this time. Wait for timeout for next one.
                            starFlagIndex = (starFlagIndex + 1) % 6;

                            if (!losingTransaction)
                            {
                                if (coinSpinSpeed < 130)
                                    coinSpinSpeed = 130;
                            }

                            break;
                        }

                        starFlagIndex = (starFlagIndex + 1) % 6;
                        nextCount++;
                    }
                }
            }

            // Continue moving the star(s) or flag(s) that were launched.
            Vector3 targetPos;
            for (int i = 0; i < 6; i++)
            {
                if (losingTransaction)
                    targetPos = new Vector3(StarFlagImages[i].transform.position.x, -1, 0); // bottom of screen (NOT USED)
                else
                    targetPos = SpinningCoin.transform.position; // towards coin

                for (int j = 0; j < ATM_TRANSACT_POOL; j++)
                {
                    // All active ones go towards the SpinningCoin targetPos!
                    GameObject starFlagOb = atmTransferImages[i, j].gameObject;
                    
                    if (starFlagOb.activeSelf)
                    {
                        Vector3 startPos = starFlagOb.transform.position;
                        if (losingTransaction)
                            starFlagOb.transform.position -= new Vector3(0, LoseTravelSpeed * dt, 0);// faster
                        else
                            starFlagOb.transform.position = Vector3.Lerp(startPos, targetPos, AtmTravelSpeed * dt);

                        bool closeEnough = false;
                        if (losingTransaction)
                            closeEnough = starFlagOb.transform.position.y < -1.0f;
                        else
                            closeEnough = (starFlagOb.transform.position - targetPos).sqrMagnitude < 100 * Vector3.kEpsilon;

                        if (closeEnough)
                        {
                            // Close enough, now do the tabulation and update the list.
                            starFlagOb.SetActive(false);
                            Debug.LogError($"SetActive FALSE of {starFlagOb.name}");

                            if (losingTransaction)
                            {

                            }
                            else
                            {
                                if (i == 0)
                                    Game.Inst.AddCoins(1);
                                if (i == 1)
                                    Game.Inst.AddCoins(2);
                                if (i == 2)
                                    Game.Inst.AddCoins(3);
                                if (i == 3)
                                    Game.Inst.AddCoins(1);
                                if (i == 4)
                                    Game.Inst.AddCoins(2);
                                if (i == 5)
                                    Game.Inst.AddCoins(3);
                            }

                            // Update the coins just added.
                            SpinningCoinCount.text = Game.Inst.CoinCount().ToString();


                            // Advance front of queue, since we are done with this one.
                            atmTransferStates[i, 0] = (atmTransferStates[i, 0] + 1) % 6;

                            // This one transacted.
                            atmTransactionsRemaining--;

                            int starFlag = -1;
                            for (int k = 0; k < 6; k++)
                            {
                                if (starFlagOb.name.Contains(StarFlagImages[i].name)) // e.g. "Flag1(Clone)" contains "Flag1"
                                {
                                    starFlag = i;
                                    break;
                                }
                            }
                            Debug.Assert(starFlag != -1);
                            Debug.Log($"End transfer transfer starFlag={starFlag} ({atmTransferStates[starFlag, 0]},{atmTransferStates[starFlag, 1]})");

                            if (!losingTransaction)
                            {
                                coinSpinSpeed += CoinSpinAccel * dt;

                                if (coinSpinSpeed > 600)
                                    coinSpinSpeed = 600;
                            
                                //Game.Inst.PlayOneShot(Game.Inst.CoinTransferClip, 0.2f, 0.1f);
                                //Game.Inst.PlayOneShot(Game.Inst.CoinTransferClip, 0.4f, 0.2f);
                                Game.Inst.PlayOneShot(Game.Inst.CoinTransferClip);//, 0.6f, 0.3f);
                            }

                            Debug.Log("Transaction remaining is now: " + atmTransactionsRemaining);
                        }
                    }
                }
            }

            //if(losingTransaction)
            {

            }
            /*else*/ if (atmTransactionsRemaining == 0)
            {
                // Reset.
                starFlagElapsed = 0;
                losingTransaction = false; // if it WAS true
                losingTransactionFalling = false;
            }
            else
            {
                starFlagElapsed += Time.deltaTime;
            }
        }

        // Coins will spin if there happens to be any left.
        coinSpinDistance += coinSpinSpeed * dt;
        if (coinSpinDistance > 4)
        {
            coinSpinDistance -= 4;
            coinSpinIndex = (coinSpinIndex + 1) % 4;
        }

        Rect uvRect = SpinningCoin.uvRect;
        uvRect.x = coinSpinUvs[coinSpinIndex, 0];
        uvRect.y = coinSpinUvs[coinSpinIndex, 1];
        SpinningCoin.uvRect = uvRect;

        coinSpinSpeed -= CoinSpinDecel * dt;
        if (coinSpinSpeed < 0)
            coinSpinSpeed = 0;

        if (atmTransactionsRemaining == 0 && coinSpinIndex != 0)
        {
            // Give a little last oomph to get it to the starting frame again.
            coinSpinSpeed = 15f;
        }
        
    }

    public void ResetAtm()
    {
        for (int i = 0; i < 6; i++)
        {
            // Reset ranges.
            atmTransferStates[i, 0] = 0;
            atmTransferStates[i, 1] = 0;
            for (int j = 0; j < ATM_TRANSACT_POOL; j++)
            {
                atmTransferImages[i, j].gameObject.SetActive(false);
            }
        }
    }

    public void TransactAtm()
    {
        if (atmTransactionsRemaining > 0) // Already transacting (or losing it all!)
            return;

        atmTransactionsRemaining = Game.Inst.StarFlagCountTotal();

        if (atmTransactionsRemaining > 0)
        {
            starFlagElapsed = AtmNextStarFlagTimeout; // To force start the first one.
        }
        else
        {
            // Play empty sound.

        }
    }

    public void LoseStarFlags()
    {
        losingTransaction = true;
        TransactAtm();
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
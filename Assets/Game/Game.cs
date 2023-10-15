using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
//[RequireComponent(typeof(GridPicker))]
public class Game : MonoBehaviour
{
    public Bike GameBike;
    public GameObject HandleBars;
    public RectTransform SpeedGaugeMain;
    public Image SpeedGaugeImage;
    public RectTransform SpeedGaugeAdjuster;

    public float BikeAccel = 10;
    public float BikeDecel = 5;
    public float BikeMaxSpeed = 40;
    public float BikeTurn = 3;
    public float BikeTurnTranslation = 3;
    public float BikeHandleTurn = 20;
    public float BikeSteerScreenWidth = 50;
    public float BikeSteerSwipeScale = 0.2f;
    public float BikeSteerPercentEasyJump = 0.4f;
    public int BikeSteerSwipeScaleLimit = 20;
    

    public float BikeJumpStartSpeed = 30f;
    public float BikeJumpDecelRise = 0.02f;
    public float BikeJumpDecelFall = 0.02f;
    public float BikeJumpDecelForward = 2f;
    public float BikeJumpLandingSlowdown = 5f;

    public float BikeJumpTilt = 0.5f;
    public float BikeJumpTiltThreshold = 0.8f;
    public float BikeJumpSailTimeout = 1.2f;

    public AudioClip BikeCycleClip;
    public AudioClip BikeCrashClip;
    public AudioClip BikeJumpClip;
    public AudioClip BikeJump2Clip;
    public AudioClip BikeSailClip;
    public AudioClip BikeCollectClip;
    public AudioClip BikeSwerveClip;
    public AudioClip FinishLineClip;
    public AudioClip FinishedBadClip;
    public AudioClip FinishedGoodClip;
    public AudioClip FinishedWonderfulClip;
    public AudioClip ConfettiBangClip;
    public AudioClip CowHitClip;
    public AudioClip CowSwerveClip;
    public AudioClip HorseHitClip;
    public AudioClip HorseSwerveClip;
    public AudioClip SheepHitClip;
    public AudioClip SheepSwerveClip;
    public List<AudioClip> ConfettiPopClips;


    public float BikeCyclePitchScale = 2.0f;
    public float CamFollowDistX = 0f;
    public float CamFollowDistY = 2f;
    public float CamFollowDistZ = 2f;
    public float CamFollowSpeedX = 6.0f;
    public float CamFollowSpeedY = 4.0f;
    public float CamFollowSpeedZ = 6.0f;
    public float CamTiltScaleJump = 30f;
    public List<GameObject> obstaclePrefabs; // For instancing the obstacles.

    float originalBikeEulerY;
    float originalHandleBarEulerY;
    float originalCamEulerX;
    float screenMiddleBorderLeft;
    float screenMiddleBorderRight;
    float bikeSpeed = 0f;
    float bikeJumpSpeed = 0;
    bool isJumping = false;
    bool isSailing = false;
    int currentLevel = -1;
    public bool IsRestarting { get; set; } = false;
    public bool IsFinished { get; set; } = false;
    float sailElapsed = 0f;

    Camera gameCam;
    AudioSource audioSource;
    AudioSource audioSourceOneShot;
    AudioSource audioSourceOnOff;
    GridPicker gridPicker;

    int levelCount;
    List<int> gridCounts;
    int obstacleCount;
    Dictionary<int, int> obstacleMaxAnyLevel = new();
    Dictionary<string, int> emojiToObstacle = new();

    List<List<List<int>>> choicesGrid = new();
    List<List<GameObject>> obstaclePools = new();

    static public WaitForSecondsRealtime WaitParticlesStop = new WaitForSecondsRealtime(2.0f);
    static public WaitForSecondsRealtime WaitRestartFinish = new WaitForSecondsRealtime(1.0f);
    static public WaitForSecondsRealtime WaitConfettiStart = new WaitForSecondsRealtime(2.0f);
    static public WaitForSecondsRealtime WaitConfettiStop = new WaitForSecondsRealtime(4.0f);
    static public WaitForSecondsRealtime WaitGaugeBorderFade = new WaitForSecondsRealtime(0.2f);

    /*
    static public List<WaitForSecondsRealtime> = 
        new(){ new WaitForSecondsRealtime(0.2f), new WaitForSecondsRealtime(2.0f) };
    */

    static public List<WaitForSecondsRealtime> WaitTimes1 = new List<WaitForSecondsRealtime>
    {
        new WaitForSecondsRealtime(0.2f),
        new WaitForSecondsRealtime(0.4f),
        new WaitForSecondsRealtime(0.6f),
        new WaitForSecondsRealtime(0.8f),
        new WaitForSecondsRealtime(1.0f),
    };

    static public List<WaitForSecondsRealtime> WaitTimes2 = new List<WaitForSecondsRealtime>
    {
        new WaitForSecondsRealtime(0.2f),
        new WaitForSecondsRealtime(0.4f),
        new WaitForSecondsRealtime(0.6f),
        new WaitForSecondsRealtime(0.8f),
        new WaitForSecondsRealtime(1.0f),
        new WaitForSecondsRealtime(1.2f),
        new WaitForSecondsRealtime(1.4f),
        new WaitForSecondsRealtime(1.6f),
        new WaitForSecondsRealtime(1.8f),
        new WaitForSecondsRealtime(2.0f),
    };

static public int Rows = GridPicker.NumRows; // Number of rows in each grid
    static public int Cols = GridPicker.NumCols; // Number of columns in each grid

    static public Game Inst;

    public void PlayOneShotRandom(List<AudioClip> clips)
    {
        AudioClip randomClip = clips[Random.Range(0, clips.Count)];
        PlayOneShot(randomClip);
    }

    public void PlayOneShot(AudioClip clip, float delay = 0)
    {
        audioSourceOneShot.clip = clip;
        audioSourceOneShot.PlayDelayed(delay);
        //audioSourceOneShot.PlayOneShot(clip);
    }

    public void PlayOnOff(AudioClip clip, float pitch = 1.0f)
    {
        StopOnOff();
        audioSourceOnOff.pitch = pitch;
        audioSourceOnOff.PlayOneShot(clip);
    }

    public void StopOnOff()
    {
        if (audioSourceOnOff.isPlaying)
            audioSourceOnOff.Stop();
    }

    void Awake()
    {
        Inst = this;

        // Scale GUI appopriate to device.

        //SpeedGaugeMain.position = 10f * SpeedGaugeMain.position;
        //SpeedGaugeMain.localScale = 10f * SpeedGaugeMain.localScale;


        audioSource = GetComponent<AudioSource>(); // continuous chug chug
        audioSourceOneShot = gameObject.AddComponent<AudioSource>(); // jump, collect, etc
        audioSourceOnOff = gameObject.AddComponent<AudioSource>(); // sailing, bumping, etc

        audioSourceOneShot.playOnAwake = false;
        audioSourceOnOff.playOnAwake = false;

        gridPicker = GetComponent<GridPicker>();

        gameCam = Camera.main;

        // Go through all the levels and determine how many instances you need of each type.
        GridPickerUtil.SerialToGrid(choicesGrid, gridPicker.LevelStats, gridPicker.GridChoices); // populates choicesGrid
        levelCount = gridPicker.LevelStats.Count;
        gridCounts = gridPicker.LevelStats;
        obstacleCount = GridPicker.Emojis.Length - 1; // minus the zero space
        Debug.Assert(obstacleCount == obstaclePrefabs.Count); // Should be as many prefabs linked as defined obstacles

        for (int i = 0; i < obstacleCount; i++) obstaclePools.Add(new());// List<GameObject>())
        for (int i = 0; i < GridPicker.Emojis.Length; i++) emojiToObstacle[GridPicker.Emojis[i]] = i;
        for (int i = 0; i < obstacleCount; i++) obstacleMaxAnyLevel[i] = 0;

        Dictionary<int, int> obstacleMaxThisLevel = new();
        for (int l = 0; l < levelCount; l++)
        {
            // Clear it, since we recount each level.
            for (int i = 0; i < obstacleCount; i++)
                obstacleMaxThisLevel[i] = 0;

            for (int g = 0; g < gridCounts[l]; g++)
            {
                for (int c = 0; c < Rows * Cols; c++)
                {
                    int choice = choicesGrid[l][g][c];
                    if (choice > 0)
                    {
                        obstacleMaxThisLevel[choice - 1]++;
                    }
                }
            }

            // Update the maxes with what was just found. We only need to know the max per level in order
            // to allocate a big enough pool of objects per obstacle type for the duration of the whole game.
            for (int i = 0; i < obstacleCount; i++)
                if (obstacleMaxThisLevel[i] > obstacleMaxAnyLevel[i])
                    obstacleMaxAnyLevel[i] = obstacleMaxThisLevel[i];
        }

        // Instance the required obstacles and intially disable them.
        for (int i = 0; i < obstacleCount; i++)
        {
            for (int j = 0; j < obstacleMaxAnyLevel[i]; j++)
            {
                GameObject newObstacle = Instantiate(obstaclePrefabs[i]);
                obstaclePools[i].Add(newObstacle);
                newObstacle.SetActive(false); // reactivate per level spec
            }
        }

        NextLevel();

    }

    public void NextLevel()
    {
        currentLevel++;

        // Disable all obstacles to prepare for the next level. Also prep the obstacle counter.
        Dictionary<int, int> obstacleCountNextLevel = new();
        for (int i = 0; i < obstacleCount; i++)
        {
            foreach (GameObject ob in obstaclePools[i])
                ob.SetActive(false);

            obstacleCountNextLevel[i] = 0;
        }

        // Polyperfect terrain tiles are 15 x 15 units square X and Z. Each platform should be raised 0.15 in Y.
        // The first tile is centered at the world origin.
        // To layout, start from the middle of the top left square in the tile's grid which will be our origin point.
        // The grid will be split into 3 x 3 per the picker, so each of the 9 squares will be 15 / 3 = 5 units in height and width.
        // So that makes the coordinates of the origin (-5, 5), since we go back/forward 2.5 
        // through the origin tile, then another back/forwadrd 2.5 into half of the topmost and leftmost tile.
        //
        // 0 1 2 (cols 0-2)
        //                                      +Z
        // + S S (row 0)                        ^   (refernce axis, position here is not meaningful)
        // S O S (row 1)  (one grid segment G)  | 
        // S S S (row 2)                        + --> +X 
        //
        // S = square
        // O = world origin middle of square
        // + = our grid origin middle of square (top left of grid)
        //
        // Also note we are placing our 3D grid from the bottom up rather than top down of what is represented
        // in the Unity Editor, since that matches the extending of the grid forward into the +Z camera distance:
        //
        // Unity Editor (scrolls down)
        // ...
        // Level X:
        // G1
        // G2
        // G3
        // G4
        // G5
        //
        // Our grid in world presented in reverse:
        // G5 ^ +Z
        // G4 |
        // G3 | 
        // G2 |
        // G1 (world origin)

        int grids = gridCounts[currentLevel];
        Vector3 gridOrigin = new Vector3(-5f, 0.15f, 5f);
        float gridExtent = 15;
        float tileExtent = gridExtent / Rows; // or Cols

        for (int g = 0; g < grids; g++) // g = 0 is on bottom, g = 1 is going in positive Z direction
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    // This is always within the given grid block we are referencing.
                    int gridIndex = Cols * r + c;

                    // The trailing -1 is required to go from the list with a space at position 0 (not an obstacle) to the list without.
                    // The (grids - 1) - g is reversing the sense of g as the increasing grid index and is pulling from the bottom up of the Editor scroll area.
                    int obstacle = choicesGrid[currentLevel][(grids - 1) - g][gridIndex] - 1;

                    // -1 means found the blank, which means do nothing (> -1 is any real obstacle)
                    if (obstacle > -1)
                    {
                        int nextObIndex = obstacleCountNextLevel[obstacle];
                        GameObject ob = obstaclePools[obstacle][nextObIndex];
                        ob.SetActive(true);
                        ob.transform.position = gridOrigin + new Vector3(
                            c * tileExtent, // skip over current columns
                            0,                                                                                                                          
                            g * gridExtent - r * tileExtent); // skip over previous grids and current rows

                        // Will be enabling the next in line in the next go-around (if we get there).
                        obstacleCountNextLevel[obstacle]++;
                    }
                }

        if (currentLevel == levelCount)
        {
            // Beat the game! Celebrate and start over!
        }
    }

    void Start()
    {
        originalBikeEulerY = GameBike.transform.localEulerAngles.y;
        originalHandleBarEulerY = HandleBars.transform.localEulerAngles.y;

        originalCamEulerX = gameCam.transform.localEulerAngles.x;

        screenMiddleBorderLeft = Screen.width / 2 - BikeSteerScreenWidth / 2;
        screenMiddleBorderRight = Screen.width / 2 + BikeSteerScreenWidth / 2;
    }

    void Update()
    {
        //Game.Inst = this;

        // Cache some values.
        float dt = Time.deltaTime;
        //Vector3 mousePos = Input.mousePosition;
        float pixelsAwayFromMiddle = 0;
        float fromMiddle = 0;

        // Move camera always.
        Vector3 camPos = gameCam.transform.position;
        //float magDiff = (Bike.transform.position - (gameCam.transform.position + new Vector3(CamFollowDistX, CamFollowDistY, CamFollowDistZ))).sqrMagnitude;
        float magDiff = (GameBike.transform.position - gameCam.transform.position + new Vector3(CamFollowDistX, CamFollowDistY, CamFollowDistZ)).sqrMagnitude;
        //if (magDiff > 2.0f)
        if (magDiff > 0.2f)
        {
            gameCam.transform.position += new Vector3(
                dt * CamFollowSpeedX * (GameBike.transform.position.x + CamFollowDistX - gameCam.transform.position.x),
                dt * CamFollowSpeedY * (GameBike.transform.position.y + CamFollowDistY - gameCam.transform.position.y),
                dt * CamFollowSpeedZ * (GameBike.transform.position.z + CamFollowDistZ - gameCam.transform.position.z));

            Vector3 currentCamEulers = gameCam.transform.localEulerAngles;
            currentCamEulers.x = originalCamEulerX + CamTiltScaleJump * GameBike.transform.position.y;
            gameCam.transform.localEulerAngles = currentCamEulers;

        }

        SpeedGaugeAdjuster.localPosition = new Vector3(0, SpeedGaugeAdjuster.rect.height * (bikeSpeed / BikeMaxSpeed), 0);

        if (IsRestarting)
            return;

        if (IsFinished)
            return;

        // Bike chug sound mechanism.
        if (!isJumping && bikeSpeed > 2f)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(BikeCycleClip);
                audioSource.pitch = BikeCyclePitchScale * bikeSpeed;
            }
        }

        //SpeedGaugeImage.color = new Color(0, 0, 0, 0.7f);


#if UNITY_TOUCH_SUPPORTED
        if (Input.touchCount > 0 && Input.touches[0].phase != TouchPhase.Canceled)
#else
        if (Input.GetMouseButton(0)) // button is being held down now
#endif
        {
            // Accel & Steering (pressed while not in air).
            if (!isJumping)// && mousePos.x > screenMiddleBorderLeft && mousePos.x < screenMiddleBorderRight)
            {
                
#if UNITY_TOUCH_SUPPORTED
                Touch firstTouch = Input.GetTouch(0);
                Vector2 contactPos = firstTouch.position;
                Vector2 contactDelta = firstTouch.deltaPosition;
#else
                Vector2 contactPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
                pixelsAwayFromMiddle = contactPos.x - Screen.width / 2;

                //Debug.Log($"Accel contact from mid: {pixelsAwayFromMiddle}             X: {contactPos.x}");
                fromMiddle = Mathf.Sign(pixelsAwayFromMiddle) * Mathf.Min(BikeSteerSwipeScaleLimit, BikeSteerSwipeScale * Mathf.Abs(pixelsAwayFromMiddle));

                Vector3 currentHandleEulers = HandleBars.transform.localEulerAngles;
                currentHandleEulers.y = originalHandleBarEulerY - BikeHandleTurn * fromMiddle; // rot y is opposite of bike direction so need minus sign
                HandleBars.transform.localEulerAngles = currentHandleEulers;

                Vector3 currentBikeEulers = GameBike.transform.localEulerAngles;
                currentBikeEulers.y = originalBikeEulerY + BikeTurn * fromMiddle;
                GameBike.transform.localEulerAngles = currentBikeEulers;

                bikeSpeed += BikeAccel * dt;
                if (bikeSpeed > BikeMaxSpeed)
                    bikeSpeed = BikeMaxSpeed;

                //SpeedGaugeImage.color = new Color(0, 0, 0, 0.3f);
            }
        }

#if UNITY_TOUCH_SUPPORTED
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended)
#else
        if (Input.GetMouseButtonUp(0)) // button is let go now
#endif
        {
            if (!isJumping)
            {
                PlayOneShot(BikeJumpClip);
                isJumping = true;
                bikeJumpSpeed = BikeJumpStartSpeed;
            }
        }

        // Jumping sail.
        if (isJumping)
        {
            if (sailElapsed < BikeJumpSailTimeout && Input.GetMouseButton(0))
            {
                if (!isSailing)
                {
                    PlayOnOff(BikeSailClip, 0.1f);
                    GameBike.StartWind();
                }

                isSailing = true;
            }
            else
            {
                if (isSailing)
                {
                    StopOnOff();
                    GameBike.StopWind();
                }

                isSailing = false;
            }
        }
        else
        {
            // Grounded and slowing down.
            bikeSpeed -= BikeDecel * dt;
            if (bikeSpeed < 0)
                bikeSpeed = 0;
        }

        
        // Jumping Vertical Accel/Decel.
        if (isJumping)
        {
            Vector3 oldPos = GameBike.transform.position;

            GameBike.transform.position += new Vector3(0, dt * bikeJumpSpeed, 0);

            if (GameBike.transform.position.y > oldPos.y) // rising
            {
                bikeJumpSpeed += dt * -BikeJumpDecelRise;
            }
            else // falling
            {
                if (isSailing)
                {
                    sailElapsed += dt;
                    //if (sailElapsed > BikeJumpSailTimeout)
                    //    isSailing = false;
                }
                else
                {
                    bikeJumpSpeed += dt * -BikeJumpDecelFall;

                    bikeSpeed -= BikeJumpDecelForward * dt;
                    if (bikeSpeed < 0)
                        bikeSpeed = 0;
                }
            }

            // Tilting back.
            float bikeTilt = 0;
            if (GameBike.transform.position.y > BikeJumpTiltThreshold)
                bikeTilt = BikeJumpTilt * GameBike.transform.position.y;

            // Hitting the ground.            
            if (GameBike.transform.position.y < 0)
            {
                GameBike.transform.position = new Vector3(GameBike.transform.position.x, 0, GameBike.transform.position.z);
                bikeJumpSpeed = 0f;
                isJumping = false;
                bikeTilt = 0f;
                sailElapsed = 0f;
                bikeSpeed -= BikeJumpLandingSlowdown; // skitter slow as you hit the ground
                if (bikeSpeed < 0)
                    bikeSpeed = 0;
            }

            // Apply jump tilt (if any).
            Vector3 currentBikeEulers = GameBike.transform.localEulerAngles;
            currentBikeEulers.x = bikeTilt;
            GameBike.transform.localEulerAngles = currentBikeEulers;
        }

        // Moving.
        if (bikeSpeed > 0)
        {
            GameBike.transform.position += new Vector3(dt * BikeTurnTranslation * fromMiddle, 0, dt * bikeSpeed + transform.position.z);
        }
    }

    public void ResetBikeRotation()
    {
        Vector3 currentHandleEulers = HandleBars.transform.localEulerAngles;
        currentHandleEulers.y = originalHandleBarEulerY;
        HandleBars.transform.localEulerAngles = currentHandleEulers;

        Vector3 currentBikeEulers = GameBike.transform.localEulerAngles;
        currentBikeEulers.y = originalBikeEulerY;
        GameBike.transform.localEulerAngles = currentBikeEulers;
    }

    public void BikeStopped()
    {
        bikeSpeed = 0f;
    }

    public void Restart()
    {
        //Debug.Log("Restart, before DoRestart.");
        StartCoroutine(DoRestart());
       // Debug.Log("Restart, after DoRestart.");
    }

    IEnumerator DoRestart()
    {
        //Debug.Log("DoRestart, before yield null.");

        // Skip a frame to prevent lockup?
        yield return null;

        //Debug.Log("DoRestart, aftger yield null.");


        IsRestarting = true;
        yield return Game.WaitRestartFinish;

        //Debug.Log("DoRestart, aftger yield WaitRestartFinish.");



        GameBike.transform.position = Vector3.zero;
        ResetBikeRotation();
        IsRestarting = false;

        //Debug.Log("DoRestart, aftger everything.");


    }
}

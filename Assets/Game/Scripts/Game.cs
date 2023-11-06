using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UI;
using static UnityEngine.InputManagerEntry;
#endif

[RequireComponent(typeof(AudioSource))]
//[RequireComponent(typeof(GridPicker))] // Add this back when done mucking with it.
[RequireComponent(typeof(GameGui))]
public class Game : MonoBehaviour
{
    // Object Refs
    public Bike GameBike;
    public GameObject FrontWheel;
    public GameObject BackWheel;
    public GameObject HandleBars;
    public GameObject FinishBlock;

    // Gui Params
    public RectTransform SpeedGaugeAdjuster;

    // Bike Params
    public float BikeAccel = 10;
    public float BikeDecel = 5;
    public float BikeSwerveDecel = 12; // Should be greater than BikeAccel, since we are overcompensating.
    public float BikeMaxSpeed = 40;
    public float BikeMaxSpeed2 = 42;
    public float BikeMaxSpeedGaugePercent = 60;
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
    public float BikeJump2Wait = 0.8f;
    public float BikeJump2Timeout = 1.2f; // should be more than wait
    public float BikeJump2SpeedBoost = 12f;
    public float BikeJump2DecelRise = 0.005f;
    public float BikeJump2DecelFall = 0.005f;
    public float BikeJump3Wait = 0.8f;
    public float BikeJump3Timeout = 1.2f; // should be more than wait
    public float BikeJump3SpeedBoost = 12f;
    public float BikeJump3DecelRise = 0.005f;
    public float BikeJump3DecelFall = 0.005f;
    public float BikeJumpUntiltSpeed = 20f;

    public float BikeJumpFlipSpeed = 10f;
    public float BikeJumpFlipAmount = 400;
    public float BikeJumpFlipSpeedBoost = 30f;
    public float BikeJumpFlipDecelRise = 0.01f;
    public float BikeJumpFlipDecelFall = 0.01f;

    public float BikeWheelSpeedVisualFactor = 0.2f;

    // Sounds
    public AudioClip BikeCycleClip;
    public AudioClip BikeCrashClip;
    public AudioClip BikeCrashWallClip;
    public AudioClip BikeJumpClip;
    public AudioClip BikeJump2Clip;
    public AudioClip BikeJump3Clip;
    public AudioClip BikeSailClip;
    public AudioClip BikeCollectStarClip;
    public AudioClip BikeCollectFlagClip;
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
    public AudioClip CoinTransferClip;
    public List<AudioClip> ConfettiPopClips;

    public float BikeCyclePitchScale = 2.0f;
    public float CamFollowDistX = 0f;
    public float CamFollowDistY = 2f;
    public float CamFollowDistZ = 2f;
    public float CamFollowSpeedX = 6.0f;
    public float CamFollowSpeedY = 4.0f;
    public float CamFollowSpeedZ = 6.0f;
    public float CamTiltScaleJump = 30f;
    public List<GameObject> ObstaclePrefabs; // For instancing the obstacles.
    public List<GameObject> LandPrefabs; // Must match Land enum in GridPicker.
    public List<GameObject> SeaPrefabs; // Must match Sea enum in GridPicker.

    // Bike Controls
    float originalBikeEulerY;
    float originalHandleBarEulerY;
    float originalCamEulerX;
    float bikeSpeed = 0f;
    float bikeJumpSpeed = 0;
    bool isJumping = false;
    bool isSailing = false;
    int currentLevel = -1;


    public int JumpCount { get; set; } = 0;
    public bool IsRestarting { get; set; } = false;
    public bool IsFinished { get; set; } = false;

    float sailElapsed = 0f;
    float jumpElapsed = 0f;
    float savedBikeTilt = 0f;
    bool swerveApplied = false;

    Camera gameCam;
    AudioSource audioSource;
    AudioSource audioSourceOneShot;
    AudioSource audioSourceOnOff;
    AudioSource audioSourceAnimal;
    GridPicker gridPicker;
    public GameGui GameGui { get; set; }

    // Game Layout
    int levelCount;
    List<int> gridCounts;
    int obstacleCount;
    int landCount;
    int seaCount;
    Dictionary<int, int> obstacleMaxAnyLevel = new();
    Dictionary<int, int> landMaxAnyLevel = new();
    Dictionary<int, int> seaMaxAnyLevel = new();
    Dictionary<string, int> emojiToObstacle = new();

    List<List<List<int>>> choicesGrid = new();
    List<List<GameObject>> obstaclePools = new();
    List<List<GameObject>> landPools = new();
    List<List<GameObject>> seaPools = new();

    Transform obstacleFolder;
    Transform landFolder;
    Transform seaFolder;

    // Game State
    int[] starCount = new int[3];
    int[] flagCount = new int[3];
    int coinCount = 0;

    // Timings
    static public WaitForSecondsRealtime WaitParticlesStop = new WaitForSecondsRealtime(2.0f);
    static public WaitForSecondsRealtime WaitRestartFinish = new WaitForSecondsRealtime(1.4f);
    static public WaitForSecondsRealtime WaitConfettiStart = new WaitForSecondsRealtime(2.0f);
    static public WaitForSecondsRealtime WaitConfettiStop = new WaitForSecondsRealtime(2.0f);


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

    public void PlayOneShot(AudioClip clip, float pitch = 1f, float delay = 0)
    {
        audioSourceOneShot.clip = clip;
        audioSourceOneShot.pitch = pitch;
        audioSourceOneShot.PlayDelayed(delay);
        //audioSourceOneShot.PlayOneShot(clip);
    }

    public void PlayAnimal(AudioClip clip)
    {
        audioSourceAnimal.PlayOneShot(clip);
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

    public void StartAtm()
    {
        GameGui.TransactAtm();
    }

    public int GetExtraStarSeconds(int starKind)
    {
        if(starKind == 0)
            return gridPicker.LevelBronzeSeconds[currentLevel];
        else if(starKind == 1)
            return gridPicker.LevelSilverSeconds[currentLevel];
        else if (starKind == 2)
            return gridPicker.LevelGoldSeconds[currentLevel];

        return 0;
    }

    void Awake()
    {
        Inst = this;

        // TODO: Scale GUI appopriate to device.

        //SpeedGaugeMain.position = 10f * SpeedGaugeMain.position;
        //SpeedGaugeMain.localScale = 10f * SpeedGaugeMain.localScale;

        //// REF REQUIRED COMPONENTS

        audioSource = GetComponent<AudioSource>(); // continuous chug chug
        audioSourceOneShot = gameObject.AddComponent<AudioSource>(); // jump, collect, etc
        audioSourceOnOff = gameObject.AddComponent<AudioSource>(); // sailing, bumping, etc
        audioSourceAnimal = gameObject.AddComponent<AudioSource>(); // animal moo, etc
        gridPicker = GetComponent<GridPicker>();
        GameGui = GetComponent<GameGui>();

        audioSourceOneShot.playOnAwake = false;
        audioSourceOnOff.playOnAwake = false;


        //// CAMERA

        gameCam = Camera.main;
        

        //// LEVEL LOADING


        // Go through all the levels and determine how many instances you need of each type.
        GridPickerUtil.SerialToGrid(choicesGrid, gridPicker.LevelStats, gridPicker.GridChoices); // populates choicesGrid
        levelCount = gridPicker.LevelStats.Count;
        gridCounts = gridPicker.LevelStats;
        obstacleCount = GridPicker.Emojis.Length - 1; // minus the zero space
        Debug.Assert(obstacleCount == ObstaclePrefabs.Count); // Should be as many prefabs linked as defined obstacles

        landCount = GridPicker.LevelLand.Length;
        seaCount = GridPicker.LevelSea.Length;

        for (int i = 0; i < obstacleCount; i++) obstaclePools.Add(new());
        for (int i = 0; i < landCount; i++) landPools.Add(new());
        for (int i = 0; i < seaCount; i++) seaPools.Add(new());
        for (int i = 0; i < GridPicker.Emojis.Length; i++) emojiToObstacle[GridPicker.Emojis[i]] = i;
        for (int i = 0; i < obstacleCount; i++) obstacleMaxAnyLevel[i] = 0;
        for (int i = 0; i < landCount; i++) landMaxAnyLevel[i] = 0;
        for (int i = 0; i < seaCount; i++) seaMaxAnyLevel[i] = 0;

        obstacleFolder = GameObject.Find("Obstacles").transform;
        landFolder = GameObject.Find("Lands").transform; ;
        seaFolder = GameObject.Find("Seas").transform; ;

        Dictionary<int, int> obstacleMaxThisLevel = new(); // obstacle type -> max
        Dictionary<int, int> landMaxThisLevel = new(); // land type -> max
        Dictionary<int, int> seaMaxThisLevel = new(); // sea type -> max
        for (int l = 0; l < levelCount; l++)
        {
            // OBSTACLE

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


            // LAND, similar

            for (int i = 0; i < landCount; i++)
                landMaxThisLevel[i] = 0;

            for (int i = 0; i < landCount; i++)
                if (i == gridPicker.LevelLands[l]) // if the land for this level matches with type i
                    landMaxThisLevel[i] = gridCounts[l];
                else
                    landMaxThisLevel[i] = 0;

            for (int i = 0; i < landCount; i++)
                if (landMaxThisLevel[i] > landMaxAnyLevel[i])
                    landMaxAnyLevel[i] = landMaxThisLevel[i];



            // SEA, similar

            for (int i = 0; i < seaCount; i++)
                seaMaxThisLevel[i] = 0;

            for (int i = 0; i < seaCount; i++)
                if (i == gridPicker.LevelSeas[l]) // if the sea for this level matches with type i
                    seaMaxThisLevel[i] = gridCounts[l];
                else
                    seaMaxThisLevel[i] = 0;

            for (int i = 0; i < seaCount; i++)
                if (seaMaxThisLevel[i] > seaMaxAnyLevel[i])
                    seaMaxAnyLevel[i] = seaMaxThisLevel[i];

        }

        // Instance the required obstacles and intially disable them.
        for (int i = 0; i < obstacleCount; i++)
        {
            for (int j = 0; j < obstacleMaxAnyLevel[i]; j++)
            {
                GameObject newObstacle = Instantiate(ObstaclePrefabs[i], obstacleFolder);
                obstaclePools[i].Add(newObstacle);
                newObstacle.SetActive(false); // reactivate later per level spec
            }
        }

        // Instance the required lands and intially disable them.
        for (int i = 0; i < landCount; i++)
        {
            for (int j = 0; j < landMaxAnyLevel[i]; j++)
            {
                GameObject newLand = Instantiate(LandPrefabs[i], landFolder);
                landPools[i].Add(newLand);
                newLand.SetActive(false); // reactivate later per level spec

                // Custom stuff for land piece placement.
                newLand.transform.position = new Vector3(0, -0.24f, 15f * j);
            }
        }

        // Instance the required seas and intially disable them. (TODO) 

        //StartTheNextLevel();

    }

    public void StartTheNextLevel()
    {
        currentLevel++;

        GameGui.ResetRaceTime();
        GameGui.StartAnimatedText(gridPicker.LevelNames[currentLevel]);

        // Disable all obstacles to prepare for the next level. Also prep the obstacle counter.
        Dictionary<int, int> obstacleCountNextLevel = new();
        for (int i = 0; i < obstacleCount; i++)
        {
            foreach (GameObject ob in obstaclePools[i])
                ob.SetActive(false);

            obstacleCountNextLevel[i] = 0;
        }

        // Disable all the lands.
        for (int i = 0; i < landCount; i++)
            foreach (GameObject landOb in landPools[i])
                landOb.SetActive(false);

        // Disable all the seas.
        for (int i = 0; i < seaCount; i++)
            foreach (GameObject seaOb in seaPools[i])
                seaOb.SetActive(false);

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


        // Enable the land objects.
        for (int g = 0; g < grids; g++)
        {
            int landType = gridPicker.LevelLands[currentLevel];
            GameObject landOb = landPools[landType][g];
            landOb.SetActive(true);
        }

        // Enable the sea objects. (TODO: per grid or whole level?)
        /*
        for (int g = 0; g < grids; g++)
        {
            int seaType = gridPicker.LevelSeas[currentLevel];
            GameObject seaOb = seaPools[seaType][g];
            seaOb.SetActive(true);
        }
        */

        FinishBlock.transform.position = new Vector3(0, -0.24f, grids * 15f);

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

        ResetGui();
        StartTheNextLevel();
    }

    void Update()
    {
        //Game.Inst = this;

        // Cache some values.
        float dt = Time.deltaTime;
        //Vector3 mousePos = Input.mousePosition;
        float pixelsAwayFromMiddle = 0;
        float fromMiddle = 0;
        Vector3 oldPos = GameBike.transform.position;
 
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

            if (JumpCount == 2 || JumpCount == 3)
            {
                Vector3 currentCamEulers = gameCam.transform.localEulerAngles;
                currentCamEulers.x = originalCamEulerX + CamTiltScaleJump * Mathf.Sqrt(GameBike.transform.position.y);
                gameCam.transform.localEulerAngles = currentCamEulers;
            }
        }

        float newGaugeHeight;
        if (bikeSpeed < BikeMaxSpeed)
        {
            // partial up to BikeMaxSpeed
            newGaugeHeight = SpeedGaugeAdjuster.rect.height * ((BikeMaxSpeedGaugePercent / 100f) * (bikeSpeed / BikeMaxSpeed));
        }
        else
        {
            newGaugeHeight = SpeedGaugeAdjuster.rect.height * (BikeMaxSpeedGaugePercent / 100f) + SpeedGaugeAdjuster.rect.height * ((1f - BikeMaxSpeedGaugePercent / 100f) * ((bikeSpeed - BikeMaxSpeed) / (BikeMaxSpeed2 - BikeMaxSpeed)));
        }
        
        SpeedGaugeAdjuster.localPosition = new Vector3(0, newGaugeHeight, 0);

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

                if (swerveApplied)
                {
                    bikeSpeed -= BikeSwerveDecel * dt;

                    if(bikeSpeed < BikeMaxSpeed)
                    {
                        bikeSpeed = BikeMaxSpeed;
                        swerveApplied = false;
                    }
                }
                else
                {
                    bikeSpeed += BikeAccel * dt;

                    if (bikeSpeed > BikeMaxSpeed)
                        bikeSpeed = BikeMaxSpeed;
                }
                
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
                JumpCount++;
                bikeJumpSpeed = BikeJumpStartSpeed;
            }
        }

        // Jumping sail.
        if (isJumping)
        {
            jumpElapsed += dt;
            //Debug.Log("jumpElapsed: " + jumpElapsed);
            if (JumpCount == 1 && jumpElapsed > BikeJump2Wait && Input.GetMouseButtonDown(0) && jumpElapsed < BikeJump2Timeout)
            {
                PlayOneShot(BikeJump2Clip, 2.0f);
                bikeJumpSpeed += BikeJump2SpeedBoost;
                JumpCount++;
                jumpElapsed = 0;
                //Debug.Log("START FLIPPING!");
            }
            else if(JumpCount == 2 && jumpElapsed > BikeJump3Wait && Input.GetMouseButtonDown(0) && jumpElapsed < BikeJump3Timeout)
            {
                PlayOneShot(BikeJump3Clip);
                bikeJumpSpeed += BikeJump3SpeedBoost;
                JumpCount++;
                jumpElapsed = 0;
                savedBikeTilt = BikeJumpTilt * GameBike.transform.position.y;

                // Special FX since this is the 3RD jump after all...
                GameBike.StartWind();

                //Debug.Log("START FLIPPING!");
            }
            else if (sailElapsed > BikeJump2Timeout && sailElapsed < BikeJumpSailTimeout &&
#if UNITY_TOUCH_SUPPORTED
                Input.touchCount > 0 && Input.touches[0].phase != TouchPhase.Canceled)
#else
                Input.GetMouseButton(0)) // button is being held down now
#endif
            {
                if (!isSailing)
                {
                    //PlayOnOff(BikeSailClip, 0.1f);
                    //GameBike.StartWind();
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
            GameBike.transform.position += new Vector3(0, dt * bikeJumpSpeed, 0);

            if (GameBike.transform.position.y > oldPos.y) // rising
            {
                if(JumpCount == 1)
                    bikeJumpSpeed += dt * -BikeJumpDecelRise;
                else if(JumpCount == 2)
                    bikeJumpSpeed += dt * -BikeJump2DecelRise;
                else if(JumpCount == 3)
                    bikeJumpSpeed += dt * -BikeJump3DecelRise;
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
                    if (JumpCount == 1)
                        bikeJumpSpeed += dt * -BikeJumpDecelFall;
                    else if (JumpCount == 2)
                        bikeJumpSpeed += dt * -BikeJump2DecelFall;
                    else if (JumpCount == 3)
                        bikeJumpSpeed += dt * -BikeJump3DecelFall;

                    bikeSpeed -= BikeJumpDecelForward * dt;
                    if (bikeSpeed < 0)
                        bikeSpeed = 0;
                }
            }

            // Tilting back.
            float bikeTilt = 0;
            if (GameBike.transform.position.y > BikeJumpTiltThreshold || JumpCount == 3)
            {
                if (JumpCount == 3)
                {
                    bikeTilt = savedBikeTilt; // Tilt down more for the 3rd one.
                    savedBikeTilt += BikeJumpUntiltSpeed * dt; // Will continue to keep tilting forward.
                }
                else
                    bikeTilt = BikeJumpTilt * GameBike.transform.position.y;
            }

            // Hitting the ground.            
            if (GameBike.transform.position.y < 0)
            {
                GameBike.transform.position = new Vector3(GameBike.transform.position.x, 0, GameBike.transform.position.z);
                bikeJumpSpeed = 0f;
                isJumping = false;
                JumpCount = 0;
                bikeTilt = 0f;
                savedBikeTilt = 0f;
                jumpElapsed = 0f;
                sailElapsed = 0f;
                bikeSpeed -= BikeJumpLandingSlowdown; // Skitter slow as you hit the ground.
                if (bikeSpeed < 0f)
                    bikeSpeed = 0f;

                GameBike.StopWind();
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

            FrontWheel.transform.localRotation *= Quaternion.Euler(dt * BikeWheelSpeedVisualFactor * bikeSpeed, 0, 0);
            BackWheel.transform.localRotation *= Quaternion.Euler(dt * BikeWheelSpeedVisualFactor * bikeSpeed, 0, 0);
        }
    }

    public void ResetBikeRotation()
    {
        Vector3 currentHandleEulers = HandleBars.transform.localEulerAngles;
        currentHandleEulers.y = originalHandleBarEulerY;
        HandleBars.transform.localEulerAngles = currentHandleEulers;

        //Vector3 currentBikeEulers = GameBike.transform.localEulerAngles;
        //currentBikeEulers.y = originalBikeEulerY;
        //GameBike.transform.localEulerAngles = currentBikeEulers;

        GameBike.transform.localEulerAngles = Vector3.zero;
    }

    public void BikeStopped()
    {
        bikeSpeed = 0f;
        jumpElapsed = 0f;
        sailElapsed = 0f;

        GameGui.StopTime();
    }

    public void BikeCrashed()
    {
        BikeStopped();
        GameGui.LoseStarFlags();
    }

    public void Swerve()
    {
        swerveApplied = true;
        bikeSpeed = BikeMaxSpeed2;
    }

    public void Restart(bool nextLevel)
    {
        Debug.Log("Restart, before DoRestart.");
        StartCoroutine(DoRestart(nextLevel));
        Debug.Log("Restart, after DoRestart.");
    }

    IEnumerator DoRestart(bool nextLevel)
    {
        Debug.Log("DoRestart, before yield null.");

        // Skip a frame to prevent lockup?
        yield return null;

        Debug.Log("DoRestart, aftger yield null.");


        IsRestarting = true;
        yield return Game.WaitRestartFinish;

        Debug.Log("DoRestart, aftger yield WaitRestartFinish.");

        GameBike.transform.position = Vector3.zero;
        ResetBikeRotation();

        yield return null;
        
        IsRestarting = false;


        yield return null;
        
        if (nextLevel)
        {
            StartTheNextLevel();
        }

        Debug.Log("DoRestart, aftger everything.");

    }

    //// GUI HANDLING
    /// <summary>
    /// 
    /// 


    public int StarCount(int kind) // 0-2
    {
        if (kind >= 0 && kind <= 2)
            return starCount[kind];

        return -1;
    }

    public int FlagCount(int kind) // 0-2
    {
        if (kind >= 0 && kind <= 2)
            return flagCount[kind];

        return -1;
    }

    public int StarFlagCount(int kind) // 0-5 (s1, s2, s3, f1, f2, f3)
    {
        if (kind >= 0 && kind <= 2)
            return starCount[kind];
        else if (kind >= 3 && kind <= 5)
            return flagCount[kind - 3];

        return -1;
    }

    public int StarFlagCountTotal()
    {
        int total = 0;
        for(int i = 0; i < 6; i++)
            total += Game.Inst.StarFlagCount(i);

        return total;
    }

    public int CoinCount()
    {
        return coinCount;
    }

    public void ResetGui()
    {
        for (int i = 0; i < 3; i++) { starCount[i] = 0; GameGui.ShowStar(i, 0); }
        for (int i = 0; i < 3; i++) { flagCount[i] = 0; GameGui.ShowFlag(i, 0); }
    }

    /// </summary>
    /// <param name="kind"></param>
    public void CollectStar(int kind) // 0-2
    {
        if (!GameGui.LosingTransaction)
        {
            starCount[kind]++;

            GameGui.ShowStar(kind, starCount[kind]);
        }
    }

    public void CollectFlag(int kind) // 0-2
    {
        if (!GameGui.LosingTransaction)
        {
            flagCount[kind]++;

            GameGui.ShowFlag(kind, flagCount[kind]);
        }
    }

    public int TransactStar(int kind) // 0-2
    {
        
        if (starCount[kind] > 0)
        {
            starCount[kind]--;
            GameGui.ShowStar(kind, starCount[kind]);
        }
        
        return starCount[kind];
    }

    public int TransactFlag(int kind) // 0-2
    {
        
        if (flagCount[kind] > 0)
        {
            flagCount[kind]--;
            GameGui.ShowFlag(kind, flagCount[kind]);
        }
        
        return flagCount[kind];
    }

    public int TransactStarFlag(int kind) // 0-5 (s1, s2, s3, f1, f2, f3)
    {
        if (kind >= 0 && kind <= 2)
            return TransactStar(kind);
        else if (kind >= 3 && kind <= 5)
            return TransactFlag(kind - 3);

        return -1;
    }

    public void ResetCoins()
    {
        coinCount = 0;
    }

    public void AddCoins(int number)
    {
        coinCount += number;
    }
}

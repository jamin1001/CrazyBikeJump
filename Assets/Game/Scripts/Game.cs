using Cinemachine;
using JetBrains.Annotations;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.InputManagerEntry;
#endif

//[RequireComponent(typeof(GridPicker))] // Add this back when done mucking with it.
[RequireComponent(typeof(GameGui))]
public class Game : MonoBehaviour
{
    // For instancing the obstacles. Size is preset in editor already, in case fixed prefabs are assigned.
    public List<GameObject> ObstaclePrefabs = new List<GameObject>(new GameObject[23]);
#if false
    public List<GameObject> LandPrefabs; // Must match Land enum in GridPicker.
    public List<GameObject> SeaPrefabs; // Must match Sea enum in GridPicker.
#endif
    // For instancing the terrain parts. Size is preset in editor already, in case fixed prefabs are assigned.
    public List<GameObject> TerrainPartPrefabs;

    public float CarAccel = 20f;
    public float CarStartSpeed = 5f;

    public bool IsWorldLoaded { get; set; }
    public bool IsWorldPopulated { get; set; }
    public bool IsLevelRestarting { get; set; }

    public int[] StarsThisLevel { get; set; } = new int[3];

    GridPicker gridPicker;
    public GameGui GameGui { get; set; }
    
    // Game Layout
    int levelCount;
    List<int> gridCounts;
    int obstacleCount;
    int landCount;
    int seaCount;
    int terrainPartCount;

    Dictionary<int, int> obstacleMaxAnyLevel = new();
    Dictionary<int, int> landMaxAnyLevel = new();
    Dictionary<int, int> seaMaxAnyLevel = new();
    Dictionary<int, int> terrainPartMaxAnyLevel = new();
    Dictionary<string, int> emojiToObstacle = new();

    List<List<List<int>>> choicesGrid = new();
    List<List<List<int>>> terrainPartsGrid = new();
    List<List<GameObject>> obstaclePools = new();
    List<List<GameObject>> landPools = new();
    List<List<GameObject>> seaPools = new();
    List<List<GameObject>> terrainPartsPools = new();

    public Bike TheBike;
    public GameObject FinishBlock;
    public CinemachineVirtualCamera VirtualCam;

    Transform worldFolder;
    Transform obstacleFolder;
    Transform landFolder;
    Transform seaFolder;
    Transform terrainPartsFolder;

    private List<GameObject> movingCars = new();
    private List<float> movingCarsSpeed = new();
    private List<Vector3> originalCarPositions = new();

    // Game State
    int currentLevel = -1;
    int[] starCount = new int[3];
    int[] flagCount = new int[3];
    int coinCount = 0;
    Vector3 virtualCamInitPosition;
    
    // Timings
    public WaitForSecondsRealtime WaitParticlesStop = new WaitForSecondsRealtime(2.0f);
    public WaitForSecondsRealtime WaitRestartFinish = new WaitForSecondsRealtime(2.0f);
    public WaitForSecondsRealtime WaitConfettiStart = new WaitForSecondsRealtime(1.4f);
    public WaitForSecondsRealtime WaitConfettiStop = new WaitForSecondsRealtime(1.0f);
    public WaitForSecondsRealtime WaitCheck = new WaitForSecondsRealtime(0.1f);
    public WaitForSecondsRealtime WaitCheck2 = new WaitForSecondsRealtime(0.2f);
    public WaitForSecondsRealtime JumpTextBonus = new WaitForSecondsRealtime(0.4f);
    public WaitForSecondsRealtime JumpTextDisappear = new WaitForSecondsRealtime(1.0f);


    static public List<WaitForSecondsRealtime> WaitTimes1 = new List<WaitForSecondsRealtime>
    {
        new WaitForSecondsRealtime(0.6f),
        new WaitForSecondsRealtime(0.8f),
        new WaitForSecondsRealtime(1.0f),
    };

    static public List<WaitForSecondsRealtime> WaitTimes1Half = new List<WaitForSecondsRealtime>
    {
        new WaitForSecondsRealtime(0.6f / 2),
        new WaitForSecondsRealtime(0.8f / 2),
        new WaitForSecondsRealtime(1.0f / 2),
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

    public void StartAtm()
    {
        GameGui.TransactAtm();
    }

    public int GetExtraStarSeconds(int starKind)
    {
        if (starKind == 0)
            return gridPicker.LevelBronzeSeconds[currentLevel];
        else if (starKind == 1)
            return gridPicker.LevelSilverSeconds[currentLevel];
        else if (starKind == 2)
            return gridPicker.LevelGoldSeconds[currentLevel];

        return 0;
    }

    void Awake()
    {
        // There is a single game instance, that runs everything, including the menu.
        // Levels are loaded dynamically depending on the situation.
        Inst = this;
        GameGui = GetComponent<GameGui>();

        worldFolder = GameObject.Find("LoadedWorld").transform;
        obstacleFolder = GameObject.Find("Obstacles").transform;
        landFolder = GameObject.Find("Lands").transform; ;
        seaFolder = GameObject.Find("Seas").transform; ;
        terrainPartsFolder = GameObject.Find("TerrainParts").transform;
        TheBike.gameObject.SetActive(false);
        gridPicker = worldFolder.GetChild(0).GetComponent<GridPicker>();

        Addressables.LoadAssetsAsync<Object>("CityPackAsset", OnAssetLoaded).Completed += OnLoadComplete;

    }

    void Start()
    {
        ResetGui();
        virtualCamInitPosition = VirtualCam.transform.position;
    }

    private List<Object> loadedAssets = new List<Object>();

    private void OnAssetLoaded(Object obj)
    {
        // This gets called for each addressable asset loaded and assigns it to the correct
        // category so that we can instance from these prefab references, which are special
        // for each world. For example, race cars and red road tracks for the race world.

        loadedAssets.Add(obj);

        // Get the content before the first underscore.
        string objNamePrefix = obj.name.Split('_')[0];

        if (GridPicker.ObjNameToEmojiIndex.ContainsKey(objNamePrefix))
        {
            int emojiIndex = GridPicker.ObjNameToEmojiIndex[objNamePrefix];
            ObstaclePrefabs[emojiIndex] = obj.GameObject();
        }
        else if (GridPicker.ObjNameToTerrainPartIndex.ContainsKey(objNamePrefix))
        {
            int terrainPartIndex = GridPicker.ObjNameToTerrainPartIndex[objNamePrefix];
            TerrainPartPrefabs[terrainPartIndex] = obj.GameObject();
        }

    }

    // Callback when all assets are finished loading
    private void OnLoadComplete(AsyncOperationHandle<IList<Object>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"Loaded {loadedAssets.Count} assets.");

            // Use the loaded assets in the scene
            foreach (var asset in loadedAssets)
            {
                if (asset is GameObject)
                {
                    //Instantiate(asset as GameObject);
                }
            }
        }
        else
        {
            Debug.LogError("Failed to load assets.");
        }

        // Setup based on loaded stuff.



        //// REF REQUIRED COMPONENTS

        

        IsWorldLoaded = true;

        //bikeInst.SetActive(true);
    }


    void PopulateWorld()
    {
        // TODO: Scale GUI appopriate to device.

        //SpeedGaugeMain.position = 10f * SpeedGaugeMain.position;
        //SpeedGaugeMain.localScale = 10f * SpeedGaugeMain.localScale;


        //// LEVEL LOADING SETUP

        // Go through all the levels and determine how many instances you need of each type.
        GridPickerUtil.SerialToGrid(choicesGrid, terrainPartsGrid, gridPicker.LevelStats,
            gridPicker.GridChoices, gridPicker.GridTerrainParts); // populates choicesGrid, terrainParts
        levelCount = gridPicker.LevelStats.Count;
        gridCounts = gridPicker.LevelStats;
        obstacleCount = GridPicker.Emojis.Length; // Element 0 blank is counted as obstacle but not used.
        Debug.Assert(obstacleCount == ObstaclePrefabs.Count); // Should be as many prefabs linked as defined obstacles

        landCount = GridPicker.LevelLand.Length;
        seaCount = GridPicker.LevelSea.Length;
        terrainPartCount = GridPicker.ObjNameToTerrainPartIndex.Count; 

        for (int i = 0; i < obstacleCount; i++) obstaclePools.Add(new());
        for (int i = 0; i < landCount; i++) landPools.Add(new());
        for (int i = 0; i < seaCount; i++) seaPools.Add(new());
        for (int i = 0; i < terrainPartCount; i++) terrainPartsPools.Add(new());
        for (int i = 0; i < GridPicker.Emojis.Length; i++) emojiToObstacle[GridPicker.Emojis[i]] = i;
        for (int i = 0; i < obstacleCount; i++) obstacleMaxAnyLevel[i] = 0;
        for (int i = 0; i < landCount; i++) landMaxAnyLevel[i] = 0;
        for (int i = 0; i < seaCount; i++) seaMaxAnyLevel[i] = 0;
        for (int i = 0; i < terrainPartCount; i++) terrainPartMaxAnyLevel[i] = 0;

        Dictionary<int, int> obstacleMaxThisLevel = new(); // obstacle type -> max
        Dictionary<int, int> landMaxThisLevel = new(); // land type -> max
        Dictionary<int, int> seaMaxThisLevel = new(); // sea type -> max
        Dictionary<int, int> terrainPartMaxThisLevel = new(); // terrain part -> max


        //// INSTANCE MAX CALCULATION

        // Here we calculuation max for each thing so that we can instantiate in pools what we need.
        for (int l = 0; l < levelCount; l++)
        {
            // OBSTACLE MAXES

            // Clear it, since we recount obstacles each level.
            for (int i = 0; i < obstacleCount; i++)
                obstacleMaxThisLevel[i] = 0;

            // Increment obstacles per type across the board so we track the maxes.
            for (int g = 0; g < gridCounts[l]; g++)
            {
                for (int c = 0; c < Rows * Cols; c++)
                {
                    int choice = choicesGrid[l][g][c];
                    obstacleMaxThisLevel[choice]++; // consider off by one here
                }
            }

            // Update the maxes with what was just found. We only need to know the max per level in order
            // to allocate a big enough pool of objects per obstacle type for the duration of the whole world.
            for (int i = 0; i < obstacleCount; i++)
                if (obstacleMaxThisLevel[i] > obstacleMaxAnyLevel[i])
                    obstacleMaxAnyLevel[i] = obstacleMaxThisLevel[i];

#if false
            // LAND

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

            // SEA

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

#endif

            // TERRAIN MAXES

            // Clear it, since we recount terrain parts each level.
            for (int i = 0; i < terrainPartCount; i++)
                terrainPartMaxThisLevel[i] = 0;

            // Increment terrain parts per type across the board so we track the maxes.
            for (int g = 0; g < gridCounts[l]; g++)
            {
                for (int c = 0; c < Rows * Cols; c++)
                {
                    int terrainPart = terrainPartsGrid[l][g][c];
                    terrainPartMaxThisLevel[terrainPart]++;
                }
            }

            // Update the maxes with what was just found. We only need to know the max per level in order
            // to allocate a big enough pool of objects per obstacle type for the duration of the whole world.
            for (int i = 0; i < terrainPartCount; i++)
                if (terrainPartMaxThisLevel[i] > terrainPartMaxAnyLevel[i])
                    terrainPartMaxAnyLevel[i] = terrainPartMaxThisLevel[i];
        }


        // OBSTACLE POOL INSTANCING

        // Instance the required obstacles (except null first element so i starts at 1)
        // and intially disable them.
        for (int i = 1; i < obstacleCount; i++)
        {
            // Make a pool of obstacles for the particular type, the max
            // of any given level dictating how many to reserve the pool for.
            for (int j = 0; j < obstacleMaxAnyLevel[i]; j++)
            {
                GameObject newObstacle = Instantiate(ObstaclePrefabs[i], obstacleFolder);
                obstaclePools[i].Add(newObstacle);
                newObstacle.SetActive(false); // reactivate later per level spec
            }
        }

#if false
        // Instance the required lands and intially disable them.
        for (int i = 0; i < landCount; i++)
        {
            for (int j = 0; j < landMaxAnyLevel[i]; j++)
            {
                GameObject newLand = Instantiate(LandPrefabs[i], landFolder);
                landPools[i].Add(newLand);
                newLand.SetActive(false); // reactivate later per level spec

                // Custom stuff for land piece placement.
                newLand.transform.position = new Vector3(0, -0.24f, 2 * 15f * j); // Update 2: x2 in z dir
            }
        }

        // Instance the required seas and intially disable them. (TODO) 
#endif

        // TERRAIN POOL INSTANCING
        
        // Instance the required terrain parts and intially disable them.
        for (int i = 0; i < terrainPartCount; i++)
        {
            // Make a pool of terrain parts for the particular type, the max
            // of any given level dictating how many to reserve the pool for.
            for (int j = 0; j < terrainPartMaxAnyLevel[i]; j++)
            {
                GameObject newTerrainPart = Instantiate(TerrainPartPrefabs[i], terrainPartsFolder);
                terrainPartsPools[i].Add(newTerrainPart);
                newTerrainPart.SetActive(false); // reactivate later per level spec
            }
        }

        StartTheNextLevel();

    }

    public void StartTheNextLevel()
    {
        currentLevel++;

        if (currentLevel >= levelCount)
        {
            // Beat the game! Celebrate and start over!
            GameGui.StartAnimatedText("DEMO OVER\nThanks for playing!", Color.magenta);

            return;
        }
        
        // Reset awards.
        StarsThisLevel[0] = 0;
        StarsThisLevel[1] = 0;
        StarsThisLevel[2] = 0;

        // Reset GUI.
        GameGui.ResetRaceTime();
        
        // Disable all obstacles to prepare for the next level. Also prep the obstacle counter.
        // This is just a way to enable the objects of the level (the "next" one per type) in
        // order since the queried objects over a given set of grids will not be in any
        // specified order, yet we have to enable whatever type we come upon. 
        Dictionary<int, int> obstacleCountNextLevel = new();
        for (int i = 0; i < obstacleCount; i++)
        {
            foreach (GameObject ob in obstaclePools[i])
            {
                // Restore flag properties if they were turned off previously.
                if (ob.name.Contains("Flag"))
                {
                    ob.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
                    ob.GetComponent<BoxCollider>().enabled = true;
                }

                ob.SetActive(false);

            }

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

        // Disable all the terrain parts. See obstacle parts above for more details.
        Dictionary<int, int> terrainPartsCountNextLevel = new();
        for (int i = 0; i < terrainPartCount; i++)
        {
            foreach (GameObject terrainPart in terrainPartsPools[i])
                terrainPart.SetActive(false);
            terrainPartsCountNextLevel[i] = 0;
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
        // Also note we are placing our 3D grid from top to bottom in the Unity Editor, since that matches
        // the extending of the grid forward into the +Z camera distance:
        //
        // Unity Editor (scrolls down)
        // ...
        // Level X (grids G extends "up"):
        // G5
        // G4
        // G3
        // G2
        // G1
        //
        // Our grids in world also extends "up":
        // G5 ^ +Z
        // G4 |
        // G3 | 
        // G2 |
        // G1 (world origin)

        // Update 1: Y position of tile is changed to -0.24; accomdation made for squeezeX (obstacles moved closer to center where track is, than just block 3x3 grid) (Fall 2023?)
        // Update 2: Z of tile is stretched to 2x, so that obstacles spread more in Z direction, they were too tight for jumping (May 2024)

        int grids = gridCounts[currentLevel];
        //Vector3 gridOrigin = new Vector3(-5f, -0.24f, 5f);
        //float gridExtent = 15;

        Vector3 gridTerrainOrigin = new Vector3(-5f, 0, 5f);

        float squeezeX = 3.5f; // 0.8f; // Update 2: even closer
        Vector3 gridOrigin = new Vector3(-5f + squeezeX, -0.24f, 5f);
        float gridExtent = 15;

        float tileExtentX = gridExtent / Rows - squeezeX; // or Cols
        float tileExtentZ = gridExtent / Rows; // or Cols

        for (int g = 0; g < grids; g++) // g = 0 is on bottom, g = 1 is going in positive Z direction
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    // This is always within the given grid block we are referencing.
                    int gridIndex = Cols * r + c;

                    //// TERRAIN INSTANCING

                    int terrainPart = terrainPartsGrid[currentLevel][(grids - 1) - g][gridIndex]; // not there is no -1

                    // Instance all the terrain parts.
                    {
                        int nextTpIndex = terrainPartsCountNextLevel[terrainPart];
                        GameObject tp = terrainPartsPools[terrainPart][nextTpIndex];
                        tp.SetActive(true);
                        tp.transform.position = gridTerrainOrigin + new Vector3(
                            c * 5, // skip over current columns
                            0,
                            g * 15 - r * 5); // skip over previous grids and current rows; x2 to double scale along z now (Update 2)

                        // Will be enabling the next in line in the next go-around (if we get there).
                        terrainPartsCountNextLevel[terrainPart]++;
                    }

                    //// OBSTACLE INSTANCING

                    // The (grids - 1) - g is reversing the sense of g as the increasing grid index and is pulling from the bottom up of the Editor scroll area.
                    int obstacle = choicesGrid[currentLevel][(grids - 1) - g][gridIndex];

                    if (obstacle > 0) // Instance everything but the null obstacle.
                    {
                        int nextObIndex = obstacleCountNextLevel[obstacle];
                        GameObject ob = obstaclePools[obstacle][nextObIndex];
                        ob.SetActive(true);


                        // Same as terrain.
                        ob.transform.position = gridTerrainOrigin + new Vector3(
                        c * 5,
                        0,
                        g * 15 - r * 5);

                        /* This is too premature, seems we must wait a frame.
                        int layerMask = LayerMask.GetMask("Terrain");
                        Ray downRay = new Ray(ob.transform.position + new Vector3(-2, 5, 0), Vector3.down);
                        if (Physics.Raycast(downRay, out RaycastHit downHit, Mathf.Infinity, layerMask))
                        {
                            ob.transform.position = downHit.point;
                        }
                        */

                        /*
                        ob.transform.position = gridOrigin + new Vector3(
                            c * tileExtentX, // skip over current columns
                            0,
                            g * 2 * gridExtent - r * 2 * tileExtentZ); // skip over previous grids and current rows; x2 to double scale along z now (Update 2)
                        */



                        // Will be enabling the next in line in the next go-around (if we get there).
                        obstacleCountNextLevel[obstacle]++;
                    }

                    
                }

#if false
// Land and Sea deprecated for now. The intent was to have a variety of land or sea blocks extended
// out on the level, but the terrain addition made the land layout more complicated. So this concept
// could still work for water types, but not really important right now so commenting out.

        // Enable the land objects.
        for (int g = 0; g < grids; g++)
        {
            int landType = gridPicker.LevelLands[currentLevel];
            GameObject landOb = landPools[landType][g];
            landOb.SetActive(true);
        }

        // Enable the sea objects. (TODO: per grid or whole level?)
        for (int g = 0; g < grids; g++)
        {
            int seaType = gridPicker.LevelSeas[currentLevel];
            GameObject seaOb = seaPools[seaType][g];
            seaOb.SetActive(true);
        }
#endif

        // Leave one grid past finish for nothing.
        // TODO: May need to adjust this Y based on terrain Y. 
        FinishBlock.transform.position = new Vector3(0, 0, grids * 15f); // Update 2: x2 in z direction
    }

    /* DEBUG
     
    public KeyCode pauseKey = KeyCode.P; // The key to toggle pause
    private bool isPaused = false;

    void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log("Game " + (isPaused ? "Paused" : "Resumed"));
    }
    */

    void Update()
    {
        /*
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
        */


        if (!IsWorldLoaded)
            return;

        if (!IsWorldPopulated)
        {
            PopulateWorld();
            IsWorldPopulated = true;
            return;
        }

        if (!TheBike.gameObject.activeSelf)
        {
            TheBike.gameObject.SetActive(true);

            for (int c = 0; c < obstacleFolder.childCount; c++)
            {
                Transform tr = obstacleFolder.GetChild(c);
                if (tr.gameObject.name.Contains("Terrain"))
                    continue; // Skip water, etc.


                // Place down on terrain.
                int layerMask = LayerMask.GetMask("Terrain");
                Ray downRay = new Ray(tr.position + new Vector3(0, 5, 0), Vector3.down);
                if (Physics.Raycast(downRay, out RaycastHit downHit, Mathf.Infinity, layerMask))
                {
                    tr.localPosition = downHit.point;
                }
            }
        }


        float dt = Time.deltaTime;

        for (int c = 0; c < movingCars.Count; c++)
        {
            GameObject car = movingCars[c];
            float carSpeed = movingCarsSpeed[c];
            carSpeed += CarAccel * dt;
            movingCarsSpeed[c] = carSpeed;

            car.transform.position = new Vector3(
               car.transform.position.x,
               car.transform.position.y,
               car.transform.position.z - carSpeed * dt);
        }

        

    }

    public void ResetMovingCars()
    {
        for (int c = 0; c < originalCarPositions.Count; c++)
            movingCars[c].transform.position = originalCarPositions[c];

        movingCars.Clear();
        movingCarsSpeed.Clear();
        originalCarPositions.Clear();
    }

    public void CarDanger(GameObject carOb)
    {
        movingCars.Add(carOb);
        movingCarsSpeed.Add(CarStartSpeed);
        originalCarPositions.Add(carOb.transform.position);

        carOb.GetComponent<AudioSource>().Play();
    }

    public void LevelRestart(bool nextLevel)
    {
        StartCoroutine(DoLevelRestart(nextLevel));
    }

    IEnumerator DoLevelRestart(bool nextLevel)
    {
        IsLevelRestarting = true;

        yield return Game.Inst.WaitRestartFinish;

        ResetMovingCars();
        
        // Reset bouncing animals.
        for (int c = 0; c < obstacleFolder.childCount; c++)
        {
            Transform tr = obstacleFolder.GetChild(c);

            if (tr.gameObject.activeSelf && tr.gameObject.name.Contains("1animal"))
            {
                // Stop hopping.
                Transform trc = tr.GetChild(0);
                MMPathMovement mpath = trc.transform.GetComponent<MMPathMovement>();
                mpath.enabled = false;
                trc.localPosition = new Vector3(trc.localPosition.x, 0, trc.localPosition.z); 

                // Place down on terrain again.
                int layerMask = LayerMask.GetMask("Terrain");
                Ray downRay = new Ray(tr.position + new Vector3(0, 5, 0), Vector3.down);
                if (Physics.Raycast(downRay, out RaycastHit downHit, Mathf.Infinity, layerMask))
                {
                    tr.localPosition = downHit.point;
                }
            }
        }

        if (nextLevel)
        {
            StartTheNextLevel();
        }

        IsLevelRestarting = false;
    }

    public void ActivateCamera(bool active)
    {
        VirtualCam.gameObject.SetActive(active);
    }

    public void ResetCamera()
    {
        // This prevents weird stretch behavior back to beginning of level.
        
        VirtualCam.transform.position = virtualCamInitPosition;
        //VirtualCam.gameObject.SetActive(true);
    }

    public void BikeCrashed()
    {
        StarsThisLevel[0] = 0;
        StarsThisLevel[1] = 0;
        StarsThisLevel[2] = 0;
        GameGui.LoseStarFlags();
    }

    public void FinishStarted()
    {
        GameGui.StopTime();
        TheBike.FinishStart();
    }

    public void FinishEnded()
    {
        TheBike.FinishEnd();
        //LevelRestart(true);
    }

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

    public void StartGuiLevelText()
    {
        GameGui.StartAnimatedText(gridPicker.LevelNames[currentLevel], Color.white);
    }

    public void CollectStar(int kind) // 0-2
    {
        if (!GameGui.LosingTransaction)
        {
            starCount[kind]++;

            GameGui.ShowStar(kind, starCount[kind]);
        }
        // Add on this one on top too.
        if (GameGui.AtmTransactionsRemaining > 0)
            GameGui.AtmTransactionsRemaining++;

        StarsThisLevel[kind]++;
    }

    public void CollectFlag(int kind) // 0-2
    {
        if (!GameGui.LosingTransaction)
        {
            flagCount[kind]++;

            GameGui.ShowFlag(kind, flagCount[kind]);
        }
        // Add on this one on top too.
        if (GameGui.AtmTransactionsRemaining > 0)
            GameGui.AtmTransactionsRemaining++;
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

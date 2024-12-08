
//using static Game;
using UnityEditor;
using UnityEngine;
//using Unity.VisualScripting;
using System.Collections.Generic;
using System;
//using PlasticPipe.PlasticProtocol.Messages;
//using log4net.Core;
//using UnityEngine.Rendering;
using System.Linq;
//using static Cinemachine.DocumentationSortingAttribute;

[CustomEditor(typeof(GridPicker))]
public class GridPickerEditor : Editor
{
    //static public string[] Emojis = { "x",
    // "🐀", "🐕", "🦕",         // Animal
    // "⚠", "🛢️", "🚧",         // Barrier
    // "1️", "2, "3"               // Jump
    // "🍔", "🌳", "🏢",         // Scenery
    // "🚗", "🚓", "🚚"          // Vehicle
    // "❤1", "❤2", "❤3",        // Holes
    // };
    static public int Rows = GridPicker.NumRows; // Number of rows in each grid
    static public int Cols = GridPicker.NumCols; // Number of columns in each grid
    static private float popupWidth = 30;
#if false
    static private float popupLandWidth = 100;
    static private float popupSeaWidth  = 100;
#endif
    static private float buttonWidth = 180;

    // Initialize the list of lists for selected indices
    private List<List<List<int>>> choicesGrid = new();
    private List<List<List<int>>> terrainPartsGrid = new(); // Contains [0-14] for water [0-4], tar [5-9], hole indices [10-14]
    private List<string> levelNames = new();
    private List<int> levelLands = new();
    private List<int> levelSeas = new();
    private List<int> levelBronzes = new();
    private List<int> levelSilvers = new();
    private List<int> levelGolds = new();
    private List<Texture2D> optionTextures = new();

    private void OnEnable()
    {
        // Load or assign a Texture2D

        for (int i = 0; i < GridPicker.Emojis.Length; i++)
        {
            Texture2D loadedTexture = EditorGUIUtility.Load($"Assets/Game/Editor/Icons/Icon{i}.png") as Texture2D;
            optionTextures.Add(loadedTexture);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty levelOpenProperty = serializedObject.FindProperty("LevelOpen");
        SerializedProperty levelStatsProperty = serializedObject.FindProperty("LevelStats");
        SerializedProperty levelNamesProperty = serializedObject.FindProperty("LevelNames");
        SerializedProperty levelLandsProperty = serializedObject.FindProperty("LevelLands");
        SerializedProperty levelSeasProperty = serializedObject.FindProperty("LevelSeas");
        SerializedProperty levelBronzesProperty = serializedObject.FindProperty("LevelBronzeSeconds");
        SerializedProperty levelSilversProperty = serializedObject.FindProperty("LevelSilverSeconds");
        SerializedProperty levelGoldsProperty = serializedObject.FindProperty("LevelGoldSeconds");
        SerializedProperty gridChoicesProperty = serializedObject.FindProperty("GridChoices");
        SerializedProperty gridTerrainPartsProperty = serializedObject.FindProperty("GridTerrainParts");

        // The choices are empty, so Inspector must have disappeared, so need to sync from the serial data which is not empty.
        if (choicesGrid.Count == 0 && levelStatsProperty.arraySize > 0)
        {
            int cellTotalIndex = 0;

            // This is the current number of levels as loaded. They can be further increased in the UI.
            int levels = levelStatsProperty.arraySize;

            for (int l = 0; l < levels; l++)
            {
                // Add Level.
                choicesGrid.Add(new List<List<int>>());
                terrainPartsGrid.Add(new List<List<int>>());

                // Add Level Name.
                levelNames.Add(levelNamesProperty.GetArrayElementAtIndex(l).stringValue);
                levelLands.Add(levelLandsProperty.GetArrayElementAtIndex(l).intValue);
                levelSeas.Add(levelSeasProperty.GetArrayElementAtIndex(l).intValue);
                levelBronzes.Add(levelBronzesProperty.GetArrayElementAtIndex(l).intValue);
                levelSilvers.Add(levelSilversProperty.GetArrayElementAtIndex(l).intValue);
                levelGolds.Add(levelGoldsProperty.GetArrayElementAtIndex(l).intValue);

                int gridsThisLevel = levelStatsProperty.GetArrayElementAtIndex(l).intValue;
                for (int g = 0; g < gridsThisLevel; g++)
                {
                    choicesGrid[l].Add(Enumerable.Repeat(0, Rows * Cols).ToList());
                    terrainPartsGrid[l].Add(Enumerable.Repeat(0, Rows * Cols).ToList());

                    // Populate grid.
                    for (int row = 0; row < Rows; row++)
                    {
                        for (int col = 0; col < Cols; col++)
                        {
                            int gridIndexXY = row * Cols + col;
                            try
                            {
                                int choiceXY = gridChoicesProperty.GetArrayElementAtIndex(cellTotalIndex).intValue;
                                choicesGrid[l][g][gridIndexXY] = choiceXY;
                            }
                            catch(Exception ex)
                            {
                                Debug.LogError($"GridChoice index out of range: choiceIndex={cellTotalIndex} l={l} g={g} gridIndexXY={gridIndexXY} ex={ex.Message}");
                            }
                            try
                            {
                                int terrainPartXY = gridTerrainPartsProperty.GetArrayElementAtIndex(cellTotalIndex).intValue;
                                terrainPartsGrid[l][g][gridIndexXY] = terrainPartXY;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"TerrainPart index out of range: choiceIndex={cellTotalIndex} l={l} g={g} gridIndexXY={gridIndexXY} ex={ex.Message}");
                            }

                            // Keeps incrementing since we progress linearly through the integer list of all grid entry values.
                            cellTotalIndex++;
                        }
                    }
                }
            }
        }

        // Loop through each level
        for (int level = 0; level < choicesGrid.Count; level++)
        {
            bool isLevelOpen = levelOpenProperty.GetArrayElementAtIndex(level).boolValue;
            isLevelOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isLevelOpen, "Level " + (level + 1));
            if (isLevelOpen)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Custom Level Name.
                levelNames[level] = EditorGUILayout.TextField(levelNames[level]);
#if false
// Land and Sea deprecated for now.

                // Land choice.
                Rect popupRectLand = EditorGUILayout.GetControlRect(GUILayout.Width(popupLandWidth));
                int landChoice = levelLands[level];
                levelLands[level] = EditorGUI.Popup(popupRectLand, landChoice, GridPicker.LevelLand);

                // Sea choice.
                Rect popupRectSea = EditorGUILayout.GetControlRect(GUILayout.Width(popupSeaWidth));
                int seaChoice = levelSeas[level];
                levelSeas[level] = EditorGUI.Popup(popupRectSea, seaChoice, GridPicker.LevelSea);
#endif
                // Custom Finish Seconds (3 types).
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Seconds to get gold:");
                string updatedGold = EditorGUILayout.TextField(levelGolds[level].ToString());
                int gold = 0;
                int.TryParse(updatedGold, out gold);
                levelGolds[level] = gold;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Seconds to get silver:");
                string updatedSilver = EditorGUILayout.TextField(levelSilvers[level].ToString());
                int silver = 0;
                int.TryParse(updatedSilver, out silver);
                levelSilvers[level] = silver;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Seconds to get bronze:");
                string updatedBronze = EditorGUILayout.TextField(levelBronzes[level].ToString());
                int bronze = 0;
                int.TryParse(updatedBronze, out bronze);
                levelBronzes[level] = bronze;
                EditorGUILayout.EndHorizontal();

                Rect buttonRect = EditorGUILayout.GetControlRect(GUILayout.Width(buttonWidth));
                bool clicked = GUI.Button(buttonRect, "Re-estimate seconds");
                if (clicked)
                {
                    int bronzeEstimate = choicesGrid[level].Count;

                    levelBronzes[level] = Math.Max(1, bronzeEstimate + 1);
                    levelSilvers[level] = Math.Max(1, bronzeEstimate - 0);
                    levelGolds[level]   = Math.Max(1, bronzeEstimate - 1);
                }

                // Loop through each grid in the current level
                for (int grid = 0; grid < choicesGrid[level].Count; grid++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("Grid " + (choicesGrid[level].Count - grid));

                    // Loop through each row
                    for (int row = 0; row < Rows; row++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Loop through each column
                        for (int col = 0; col < Cols; col++)
                        {
                            int choice = choicesGrid[level][grid][row * Cols + col];

                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                            // Property field for the array element with fixed width
                            Rect popupRectChoice = EditorGUILayout.GetControlRect(GUILayout.Width(popupWidth));

                            choicesGrid[level][grid][row * Cols + col] = EditorGUI.Popup(popupRectChoice, choice, GridPicker.Emojis);//, customStyle);//, styles[styleIndex]);

                            GUIStyle style = new GUIStyle(GUI.skin.label);
                            style.normal.background = optionTextures[choice];

                            // Display the label with the Texture2D as the background
                            GUILayout.Label("", style, GUILayout.Height(30), GUILayout.Width(30));

                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }

                // Add a button to dynamically add a new grid within the current level
                if (GUILayout.Button("Add Grid"))
                {
                    choicesGrid[level].Add(Enumerable.Repeat(0, Rows * Cols).ToList());
                    terrainPartsGrid[level].Add(Enumerable.Repeat(0, Rows * Cols).ToList());
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            levelOpenProperty.GetArrayElementAtIndex(level).boolValue = isLevelOpen;
        }

        // Add a button to dynamically add a new level with an empty grid
        if (GUILayout.Button("Add Level"))
        {
            levelOpenProperty.InsertArrayElementAtIndex(choicesGrid.Count);
            levelOpenProperty.GetArrayElementAtIndex(choicesGrid.Count).boolValue = true;

            choicesGrid.Add(new List<List<int>>());
            terrainPartsGrid.Add(new List<List<int>>());
            levelNames.Add("The Level Name");
            levelLands.Add(0);
            levelSeas.Add(0);
            levelBronzes.Add(6); // Sensible defaults.
            levelSilvers.Add(5);
            levelGolds.Add(4);

            // Make a minimum of one grid.
            choicesGrid[0].Add(Enumerable.Repeat(0, Rows * Cols).ToList());
            terrainPartsGrid[0].Add(Enumerable.Repeat(0, Rows * Cols).ToList());
        }

        // Sync the serialized version now that it has been updated above.
        levelStatsProperty.ClearArray(); // No entries means no levels.
        gridChoicesProperty.ClearArray();
        gridTerrainPartsProperty.ClearArray();
        levelNamesProperty.ClearArray();
        levelLandsProperty.ClearArray();
        levelSeasProperty.ClearArray();
        levelBronzesProperty.ClearArray();
        levelSilversProperty.ClearArray();
        levelGoldsProperty.ClearArray();

        int arraySize = 0;// selectedOptionIndicesProperty.arraySize;
        for (int level = 0; level < choicesGrid.Count; level++)
        {
            // Following numbers are how many grids for each level.
            levelStatsProperty.InsertArrayElementAtIndex(level); // This level index.
            levelStatsProperty.GetArrayElementAtIndex(level).intValue = choicesGrid[level].Count; // How many grids there are in this level.

            // Level name, for each level, too.
            levelNamesProperty.InsertArrayElementAtIndex(level); // This level index.
            levelNamesProperty.GetArrayElementAtIndex(level).stringValue = levelNames[level];

            // Level land, for each level, too.
            levelLandsProperty.InsertArrayElementAtIndex(level); // This level index.
            levelLandsProperty.GetArrayElementAtIndex(level).intValue = levelLands[level];

            // Level sea, for each level, too.
            levelSeasProperty.InsertArrayElementAtIndex(level); // This level index.
            levelSeasProperty.GetArrayElementAtIndex(level).intValue = levelSeas[level];

            // Level seconds (bronze)
            levelBronzesProperty.InsertArrayElementAtIndex(level); // This level index.
            levelBronzesProperty.GetArrayElementAtIndex(level).intValue = levelBronzes[level];

            // Level seconds (silver)
            levelSilversProperty.InsertArrayElementAtIndex(level); // This level index.
            levelSilversProperty.GetArrayElementAtIndex(level).intValue = levelSilvers[level];

            // Level seconds (gold)
            levelGoldsProperty.InsertArrayElementAtIndex(level); // This level index.
            levelGoldsProperty.GetArrayElementAtIndex(level).intValue = levelGolds[level];

            int grids = choicesGrid[level].Count;
            for (int grid = 0; grid < grids; grid++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int col = 0; col < Cols; col++)
                    {
                        //// Grab the value from the data structure and put into the property.
                        
                        int cell = row * Cols + col;

                        int obIndex = choicesGrid[level][grid][cell];
                        gridChoicesProperty.InsertArrayElementAtIndex(arraySize);
                        gridChoicesProperty.GetArrayElementAtIndex(arraySize).intValue = obIndex;

                        int terrainPartIndex = terrainPartsGrid[level][grid][cell];
                        gridTerrainPartsProperty.InsertArrayElementAtIndex(arraySize);
                        gridTerrainPartsProperty.GetArrayElementAtIndex(arraySize).intValue = terrainPartIndex;

                        arraySize++;


                        //// Terrain part details.

                        // Some useful obIndex values.
                        int WATER = 16;
                        int TAR = 17;
                        int PIT = 18;

                        // 0 1 2
                        // 3 4 5
                        // 6 7 8

                        int terrainPart = 0; // default is no hole

                        Func<int, int> getObTypeAbove = c => c <= 2 ? choicesGrid[level][grid - 1][c + 6] : choicesGrid[level][grid][c - 3]; // !isUpperRow
                        Func<int, int> getObTypeBelow = c => c >= 6 ? choicesGrid[level][grid + 1][c - 6] : choicesGrid[level][grid][c + 3]; // !isLowerRow

                        Func<int, bool> isUpperRow  = c => grid == 0 && c <= 2; // Upper row in whole level.
                        Func<int, bool> isLowerRow = c => grid == grids - 1 && c >= 6; // Lower row in whole level.
                        //Func<int, bool> isCenterRow = c => !isUpperRow(c) && !isLowerRow(c);

                        Func<int, bool> isSameTypeAbove = c => !isUpperRow(c) && getObTypeAbove(c) == obIndex;
                        Func<int, bool> isSameTypeBelow = c => !isLowerRow(c) && getObTypeBelow(c) == obIndex;

                        // These are all holes. Need to configure part based on neighbors.
                        if (obIndex == WATER || obIndex == TAR || obIndex == PIT)
                        {
                           terrainPart = 1; // hole

                           if (isSameTypeAbove(cell) && isSameTypeBelow(cell))
                           {
                                terrainPart = 3; // center part
                           }
                           else if(isSameTypeAbove(cell))
                           {
                                terrainPart = 4; // lower part
                           }
                           else if(isSameTypeBelow(cell))
                           {
                                terrainPart = 2; // upper part
                           }
                        }
                       
                        terrainPart += (cell % 3) * 5; // Slot cell into left(0), mid(5), or right(10)

                        // Store it.
                        terrainPartsGrid[level][grid][cell] = terrainPart;
                    }
                }
            }
        }

        // Apply any changes to serialized properties.
        serializedObject.ApplyModifiedProperties();
    }
}


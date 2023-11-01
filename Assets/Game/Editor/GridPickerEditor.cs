
using static Game;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;
using PlasticPipe.PlasticProtocol.Messages;
using log4net.Core;
using UnityEngine.Rendering;

[CustomEditor(typeof(GridPicker))]
public class GridPickerEditor : Editor
{
    //static public string[] Emojis = { "x", "🐂", "🐴", "🐑", "🛢️", "🚧", "⭕", "⚫", "🏎️", "🚌", "🚗", "🚓", "🚚" };
    static public int Rows = GridPicker.NumRows; // Number of rows in each grid
    static public int Cols = GridPicker.NumCols; // Number of columns in each grid
    static private float popupWidth = 30;
    static private float popupLandWidth = 100;
    static private float popupSeaWidth  = 100;
    static private float buttonWidth = 180;

    // Initialize the list of lists for selected indices
    private List<List<List<int>>> choicesGrid = new();// List<List<List<int>>>();
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
        

        // The choices are empty, so Inspector must have disappeared, so need to sync from the serial data which is not empty.
        if (choicesGrid.Count == 0 && levelStatsProperty.arraySize > 0)
        {
            int choiceIndex = 0;

            int levels = levelStatsProperty.arraySize;

            for (int l = 0; l < levels; l++)
            {
                // Add Level.
                choicesGrid.Add(new List<List<int>>());

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
                    // Add grid.
                    List<int> gridValues = new List<int>(Rows * Cols);
                    for (int i = 0; i < Rows * Cols; i++)
                    {
                        gridValues.Add(0); // Initialize with default values (0 in this case)
                    }
                    choicesGrid[l].Add(gridValues);

                    // Populate grid.
                    for (int row = 0; row < Rows; row++)
                    {
                        for (int col = 0; col < Cols; col++)
                        {
                            int gridIndexXY = row * Cols + col;
                            try
                            {
                                int choiceXY = gridChoicesProperty.GetArrayElementAtIndex(choiceIndex).intValue;
                                choicesGrid[l][g][gridIndexXY] = choiceXY;
                            }
                            catch(Exception ex)
                            {
                                Debug.LogError($"Index out of range: choiceIndex={choiceIndex} l={l} g={g} gridIndexXY={gridIndexXY} ex={ex.Message}");
                            }

                            // Keeps incrementing since we progress linearly through the integer list of all grid entry values.
                            choiceIndex++;
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

                // Land choice.
                Rect popupRectLand = EditorGUILayout.GetControlRect(GUILayout.Width(popupLandWidth));
                int landChoice = levelLands[level];
                levelLands[level] = EditorGUI.Popup(popupRectLand, landChoice, GridPicker.LevelLand);

                // Sea choice.
                Rect popupRectSea = EditorGUILayout.GetControlRect(GUILayout.Width(popupSeaWidth));
                int seaChoice = levelSeas[level];
                levelSeas[level] = EditorGUI.Popup(popupRectSea, seaChoice, GridPicker.LevelSea);

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

                    EditorGUILayout.LabelField("Grid " + (grid + 1));

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
                    //selectedOptionIndices[level].Add(new List<int>(numRows * numCols));

                    List<int> gridValues = new List<int>(Rows * Cols);
                    for (int i = 0; i < Rows * Cols; i++)
                    {
                        gridValues.Add(0); // Initialize with default values (0 in this case)
                    }
                    choicesGrid[level].Add(gridValues);

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
            levelNames.Add("The Level Name");
            levelLands.Add(0);
            levelSeas.Add(0);
            levelBronzes.Add(6); // Sensible defaults.
            levelSilvers.Add(5);
            levelGolds.Add(4);
        }


        // Sync the serialized version now that it has been updated above.
        levelStatsProperty.ClearArray(); // No entries means no levels.
        gridChoicesProperty.ClearArray();
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


            for (int grid = 0; grid < choicesGrid[level].Count; grid++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int col = 0; col < Cols; col++)
                    {
                        int valueToInsert = choicesGrid[level][grid][row * Cols + col];
                        gridChoicesProperty.InsertArrayElementAtIndex(arraySize);
                        gridChoicesProperty.GetArrayElementAtIndex(arraySize).intValue = valueToInsert; 
                        arraySize++;
                    }
                }
            }
        }

        // Apply any changes to serialized properties
        serializedObject.ApplyModifiedProperties();
    }
}


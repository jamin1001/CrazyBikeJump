using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Cinemachine.DocumentationSortingAttribute;

[ExecuteInEditMode]
public class GridPicker : MonoBehaviour
{
    //static public string[] Emojis = { "x", "🐂", "🐴", "🐑", "🛢️", "🚧", "⚫", "⭕", "🏎️", "🚌", "🚗", "🚓", "🚚", "f1", "f2", "f3", "fc", "atm" }; // grid choice indexes this

    static public string[] Emojis = { "x",

     "🐀1", "🐕2", "🦕3",        // Animal
     "⚠1", "🛢️2", "🚧3",        // Barrier
     "🔘1", "🔘2", "🔘3",        // Jump
     "🍔1", "🌳2", "🏢3",        // Scenery
     "🚗1", "🚓2", "🚚3",        // Vehicle
     "🕳1", "🕳2", "🕳3",        // Holes - water, tar, pit
     "🚩1", "🚩2", "🚩3",        // Flags
     "🚩💰",                      // Special
     }; // grid choice indexes this

    static public Dictionary<string, int> ObjNameToEmojiIndex = new() {
        { "None0", 0 }, 

        { "1animal", 1 },
        { "2animal", 2 },
        { "3animal", 3 },
        { "1barrier", 4 },
        { "2barrier", 5 },
        { "3barrier", 6 },
        { "1jump", 7 },
        { "2jump", 8 },
        { "3jump", 9 },
        { "1scenery", 10 },
        { "2scenery", 11 },
        { "3scenery", 12 },
        { "1vehicle", 13 },
        { "2vehicle", 14 },
        { "3vehicle", 15 },

        { "None16", 16 }, // water
        { "None17", 17 }, // tar
        { "None18", 18 }, // pit
        { "None19", 19 },
        { "None20", 20 },
        { "None21", 21 },
        { "None22", 22 },
    };

    static public Dictionary<string, int> ObjNameToTerrainPartIndex = new()
    {
        // Index used determined by z neighbors 

        // If cell 0,1,2 add 0 (/3 * 5) - obstacle index 16
        { "terrain-L-H0", 0 }, // no hole - not water, tar, or hole
        { "terrain-L-H1", 1 }, // hole - no neighbor above or below
        { "terrain-L-H2", 2 }, // hole upper - another hole below (same type)
        { "terrain-L-H3", 3 }, // hold center - another hole above AND another hole below (both same type)
        { "terrain-L-H4", 4 }, // hole lower - another hole above (same type)

        // If cell 3,4,5 add 5 (/3 * 5) - obstacle index 17
        { "terrain-M-H0", 5 },
        { "terrain-M-H1", 6 },
        { "terrain-M-H2", 7 },
        { "terrain-M-H3", 8 },
        { "terrain-M-H4", 9 },

        // If cell 5,7,8 add 10 (/3 * 5) - obstacle index 18
        { "terrain-R-H0", 10 },
        { "terrain-R-H1", 11 },
        { "terrain-R-H2", 12 },
        { "terrain-R-H3", 13 },
        { "terrain-R-H4", 14 },
    };

    static public int NumRows = 3; // Number of rows in each grid
    static public int NumCols = 3; // Number of columns in each grid

    static public string[] LevelLand = // land choice indexes this
    {
        "Grass",
        "Dirt",
        "City",
    };

    static public string[] LevelSea = // sea choice indexes this
    {
        "BlueWater",
        "GreenWater",
        "RedWater",
    };

    // These are [SerializedField]s and must be defined so that OnInspectorGUI()
    // in GridPickerEditor.cs picks up on them as "Properties" of the
    // GridPicker CustomEditor control.
    public List<bool>       LevelOpen    = new(); // Size is how many levels, each entry is whether level is opened (not folded) or not.
    public List<int>        LevelStats   = new(); // Size is how many levels, each entry is how many grids per level.
    public List<string>     LevelNames   = new(); // Size is how many levels, each entry is the level name.
    public List<int>        LevelLands   = new();
    public List<int>        LevelSeas    = new();
    public List<int>        LevelBronzeSeconds = new();
    public List<int>        LevelSilverSeconds = new();
    public List<int>        LevelGoldSeconds = new();
    public List<int>        GridChoices  = new(); // All the grid choice content from fixed sized grids
    public List<int>        GridTerrainParts = new(); // All the terrain part ids from fixed sized grids
}

public class GridPickerUtil
{
    static public int Rows = GridPicker.NumRows; // Number of rows in each grid
    static public int Cols = GridPicker.NumCols; // Number of columns in each grid

    static public void SerialToGrid(List<List<List<int>>> choicesGrid, List<List<List<int>>> terrainPartsGrid, 
        List<int> levelStats, List<int> gridChoiceList, List<int> terrainPartsList)
    {
        //// What's below is the same as what's been implemented in GridPickerEditor but with slightly different array types (lists instead of properties).
        //// (Bonus task if it can be slightly translated and reused with this code instead, to get rid of redundancy).
        
        if (choicesGrid.Count == 0 && levelStats.Count > 0)
        {
            Debug.Assert(terrainPartsGrid.Count == 0);

            int levels = levelStats.Count;
            int cellTotalIndex = 0;
            for (int l = 0; l < levels; l++)
            {
                // Add Level.
                choicesGrid.Add(new List<List<int>>());
                terrainPartsGrid.Add(new List<List<int>>());

                int gridsThisLevel = levelStats[l];
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
                                int choiceXY = gridChoiceList[cellTotalIndex];
                                choicesGrid[l][g][gridIndexXY] = choiceXY;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Index out of range: choiceIndex={cellTotalIndex} l={l} g={g} gridIndexXY={gridIndexXY} ex={ex.Message}");
                            }
                            try
                            {
                                int terrainPartXY = terrainPartsList[cellTotalIndex];
                                terrainPartsGrid[l][g][gridIndexXY] = terrainPartXY;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"TerrainPart Index out of range: terrainPartIndex={cellTotalIndex} l={l} g={g} gridIndexXY={gridIndexXY} ex={ex.Message}");
                            }

                            // Keeps incrementing since we progress linearly through the integer list of all grid entry values (for either obstacle or terrain part).
                            cellTotalIndex++;
                        }
                    }
                }
            }
        }
    }


}

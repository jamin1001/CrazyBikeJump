using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GridPicker : MonoBehaviour
{
    static public string[] Emojis = { "x", "🐂", "🐴", "🐑", "🛢️", "🚧", "⚫", "⭕", "🏎️", "🚌", "🚗", "🚓", "🚚", "f1", "f2", "f3" }; // grid choice indexes this
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

    public List<bool>       LevelOpen    = new(); // Size is how many levels, each entry is whether level is opened (not folded) or not.
    public List<int>        LevelStats   = new(); // Size is how many levels, each entry is how many grids per level.
    public List<string>     LevelNames   = new(); // Size is how many levels, each entry is the level name.
    public List<int>        LevelLands   = new();
    public List<int>        LevelSeas    = new();
    public List<int>        GridChoices  = new(); // All the grid choice content from fixed sized grids
}

public class GridPickerUtil
{
    static public int Rows = GridPicker.NumRows; // Number of rows in each grid
    static public int Cols = GridPicker.NumCols; // Number of columns in each grid

    static public void SerialToGrid(List<List<List<int>>> choicesGrid, List<int> levelStats, List<int> gridChoices)
    {
        //// What's below is the same as what's been implemented in GridPickerEditor but with slightly different array types (lists instead of properties).
        //// (Bonus task if it can be slightly translated and reused with this code instead, to get rid of redundancy).
        
        if (choicesGrid.Count == 0 && levelStats.Count > 0)
        {
            int choiceIndex = 0;

            int levels = levelStats.Count;
            for (int l = 0; l < levels; l++)
            {
                // Add Level.
                choicesGrid.Add(new List<List<int>>());

                int gridsThisLevel = levelStats[l];
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
                                int choiceXY = gridChoices[choiceIndex];
                                choicesGrid[l][g][gridIndexXY] = choiceXY;
                            }
                            catch (Exception ex)
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
    }


}

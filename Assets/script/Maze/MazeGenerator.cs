using UnityEngine;
using System.Collections;

public class MazeGenerator : MazeChunk
{
    private int iteration;
    public void GenerateGrid(GameObject cell, int width, int height, int cellSize)
    {
        if (cell == null || width <= 0 || height <= 0 || cellSize <= 0)
            return;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, 0, y * cellSize);
                
                MazeCell newCell =  Instantiate(cell, cellPosition, Quaternion.identity).GetComponent<MazeCell>();
                newCell.Init(iteration);
                _chunkCells.Add(newCell);
              
                CheckNeighBords(newCell, x, y);

                iteration++;
            }
        }
    }

    private void CheckNeighBords(MazeCell cell, int x, int y)
    {
        if (x == 0 && y == 0)
            return;

        if (x != 0)
        {
            MazeCell neighborX = _chunkCells[x - 1 * _height + y];
            cell._neighbordsCells.Add(neighborX);
            neighborX._neighbordsCells.Add(cell);
        }
        
        if (y != 0)
        {
            MazeCell neighborY = _chunkCells[x * _height + y - 1];
            cell._neighbordsCells.Add(neighborY);
            neighborY._neighbordsCells.Add(cell);
        }
    }
    

    public override void CallGenerateMaze()
    {  
        GenerateGrid(_cellPrefab.gameObject, _width, _height, _size);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class MazeChunkSafeZone : MazeChunk
{
    public override void CallGenerateMaze()
    {
        Debug.Log($"[{name}] Safe zone générée.");
        _isGenerated = true;
    }

    public override void RegenerateMaze()
    {
        throw new System.NotImplementedException();
    }

    public void TryOpenNeighbordWall()
    {
        if (_neighbordsChunks == null || _neighbordsChunks.Count < 4)
            return;

        OpenNeighbordWall();
    }

    private void OpenNeighbordWall()
    {
        DestroyWallOnNeighbor(_neighbordsChunks[0], WallOrientation.Right);
        DestroyWallOnNeighbor(_neighbordsChunks[1], WallOrientation.Up);
        DestroyWallOnNeighbor(_neighbordsChunks[2], WallOrientation.Left);
        DestroyWallOnNeighbor(_neighbordsChunks[3], WallOrientation.Down);
    }

    private void DestroyWallOnNeighbor(MazeChunk neighbor, WallOrientation wallToDestroy)
    {
        if (neighbor == null || neighbor._chunkCells == null)
            return;

        int midX = neighbor._width / 2;
        int midY = neighbor._height / 2;
        int index = -1;

        switch (wallToDestroy)
        {
            case WallOrientation.Up:
                index = (neighbor._height - 1) * neighbor._width + midX;
                break;
            case WallOrientation.Down:
                index = midX;
                break;
            case WallOrientation.Left:
                index = midY * neighbor._width;
                break;
            case WallOrientation.Right:
                index = midY * neighbor._width + (neighbor._width - 1);
                break;
        }

        if (index < 0 || index >= neighbor._chunkCells.Count)
            return;

        neighbor._chunkCells[index].DestroyWall(wallToDestroy, true);
    }
}

using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class MazeChunkSafeZone : MazeChunk
{
    public override void CallGenerateMaze()
    {
        Debug.Log($"[{name}] Safe zone générée.");
    }


    public void OpenNeighbordWall()
    {

    }
}
using UnityEngine;

public class MazeChunkSafeZone : MazeChunk
{
    public override void CallGenerateMaze()
    {
        Debug.Log($"[{name}] Safe zone générée.");
    }
}
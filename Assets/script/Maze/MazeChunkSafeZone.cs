using UnityEngine;

public class MazeChunkSafeZone : MazeChunk
{
    public override void CallGenerateMaze()
    {
        Debug.Log("[MazeChunkSafeZone] Safe zone généré");
    }
}

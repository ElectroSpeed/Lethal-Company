using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeCell : MonoBehaviour
{
    public int _cellNumber;
    public Color _cellColor;

    [HideInInspector] public readonly List<MazeCell> _neighbordsCells = new();
    [HideInInspector] public bool _visited = false;

    [SerializeField] private Transform _wallContainer;

    public void Init(int id)
    {
        _cellNumber = id;
    }


    public void DestroyWall(WallOrientation orientation, bool isBorder, MazeChunkLabyrinth chunk)
    {
        int wallIndex = orientation switch
        {
            WallOrientation.Left => 0,
            WallOrientation.Right => 1,
            WallOrientation.Up => 2,
            WallOrientation.Down => 3,
            _ => -1
        };
        if (wallIndex >= 0 && wallIndex < _wallContainer.childCount)
        {
            GameObject destroyedWall = _wallContainer.GetChild(wallIndex).gameObject;
            destroyedWall.SetActive(false);
            if (!isBorder)
            {
                chunk._wallDestroyed.Add(destroyedWall);
            }
        }
    }
}
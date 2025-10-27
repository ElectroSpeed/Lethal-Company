using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeCell : MonoBehaviour
{
    public int _cellNumber;
    public Color _cellColor;

    [HideInInspector] public readonly List<MazeCell> _neighbordsCells = new();
    [HideInInspector] public bool _visited = false;

    public void Init(int id)
    {
        _cellNumber = id;
        _cellColor = new Color(Random.value, Random.value, Random.value);
        ChangeColor();
    }

    public void ChangeColor()
    {
        transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material.color = _cellColor;
    }

    public void DestroyWall(WallOrientation orientation, bool isBorder, MazeChunkLabyrinth chunk)
    {
        Transform walls = transform.GetChild(2);
        int wallIndex = orientation switch
        {
            WallOrientation.Left => 0,
            WallOrientation.Right => 1,
            WallOrientation.Up => 2,
            WallOrientation.Down => 3,
            _ => -1
        };
        if (wallIndex >= 0 && wallIndex < walls.childCount)
        {
            GameObject destroyedWall = walls.GetChild(wallIndex).gameObject;
            destroyedWall.SetActive(false);
            if (!isBorder)
            {
                chunk._wallDestroyed.Add(destroyedWall);
            }
        }
    }
}

public enum WallOrientation { Right, Left, Up, Down }
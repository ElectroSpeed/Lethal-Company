using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeCell : MonoBehaviour
{
    public int _cellNumber;
    public Color _cellColor;
    [HideInInspector] public bool _visited;

    [HideInInspector] public readonly List<MazeCell> _neighbordsCells = new();

    public void Init(int id)
    {
        _cellNumber = id;
        _cellColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
        ChangeColor();
    }

    public void ChangeColor()
    {
        var rend = transform.GetChild(0).GetChild(0).GetComponent<Renderer>();
        rend.material.color = _cellColor;
    }

    public void DestroyWall(WallOrientation orientation)
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
            walls.GetChild(wallIndex).gameObject.SetActive(false);
    }
}

public enum WallOrientation { Right, Left, Up, Down }
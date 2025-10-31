using System.Collections.Generic;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    public int _cellNumber;

    [HideInInspector] public readonly List<MazeCell> _neighbordsCells = new();
    [HideInInspector] public bool _visited = false;

    public Transform _wallContainer;

    public void Init(int id)
    {
        _cellNumber = id;
    }

    public int GetWallIndex(WallOrientation orientation)
    {
        return orientation switch
        {
            WallOrientation.Left => 0,
            WallOrientation.Right => 1,
            WallOrientation.Up => 2,
            WallOrientation.Down => 3,
            _ => -1
        };
    }

    public WallOrientation GetOppositeWallOrientation(WallOrientation dir)
    {
        return dir switch
        {
            WallOrientation.Up => WallOrientation.Down,
            WallOrientation.Down => WallOrientation.Up,
            WallOrientation.Left => WallOrientation.Right,
            WallOrientation.Right => WallOrientation.Left,
            _ => dir
        };
    }


    public void DestroyWall(WallOrientation orientation, bool isBorder = false, MazeChunkLabyrinth chunk = null)
    {
        int wallIndex = GetWallIndex(orientation);

        if (wallIndex >= 0 && wallIndex < _wallContainer.childCount)
        {
            GameObject destroyedWall = _wallContainer.GetChild(wallIndex).gameObject;
            destroyedWall.SetActive(false);
            if (!isBorder && chunk != null)
            {
                chunk._wallDestroyed.Add(destroyedWall);
            }
        }
    }

    public void CloseWall(WallOrientation orientation)
    {
        //Create Invisible Wall before lunch anim 
        CloseWallAnim();

        int wallIndex = GetWallIndex(orientation);

        if (wallIndex >= 0 && wallIndex < _wallContainer.childCount)
        {
            GameObject closedWall = _wallContainer.GetChild(wallIndex).gameObject;
            if (closedWall.activeSelf)
            {
                print("T'es un looser tu t'es trompé");
            }

            closedWall.SetActive(true);
        }

    }
    public void CloseWallAnim()
    {

    }
}
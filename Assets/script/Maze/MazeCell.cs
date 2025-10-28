using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MazeCell : NetworkBehaviour
{
    public int _cellNumber;
    public Color _cellColor;

    [HideInInspector] public readonly List<MazeCell> _neighbordsCells = new();
    [HideInInspector] public bool _visited = false;

    [SerializeField] private Transform _wallContainer;

    private void Awake()
    {
        if (_wallContainer == null)
            _wallContainer = transform;
    }

    public void Init(int id)
    {
        _cellNumber = id;
    }

    private int GetWallIndex(WallOrientation orientation)
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
        if (wallIndex < 0 || wallIndex >= _wallContainer.childCount)
            return;

        GameObject destroyedWall = _wallContainer.GetChild(wallIndex).gameObject;
        if (!destroyedWall.activeSelf) return;

        destroyedWall.SetActive(false);
        if (!isBorder && chunk != null)
            chunk._wallDestroyed.Add(destroyedWall);

        UpdateWallClientRpc(wallIndex, false);
    }

    public void CloseWall(WallOrientation orientation)
    {
        int wallIndex = GetWallIndex(orientation);
        if (wallIndex < 0 || wallIndex >= _wallContainer.childCount)
            return;

        GameObject closedWall = _wallContainer.GetChild(wallIndex).gameObject;
        closedWall.SetActive(true);

        UpdateWallClientRpc(wallIndex, true);
    }

    [ClientRpc]
    private void UpdateWallClientRpc(int wallIndex, bool active)
    {
        if (wallIndex < 0 || wallIndex >= _wallContainer.childCount)
            return;

        _wallContainer.GetChild(wallIndex).gameObject.SetActive(active);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MazeCell : MonoBehaviour
{
    public int _cellNumber;
    public Color _cellColor;

    [HideInInspector] public List<MazeCell> _neighbordsCells = new List<MazeCell>();
    [HideInInspector] public bool _visited = false;

    public void ChangeColor()
    {
        transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material.color = _cellColor;
    }

    public void Init(int id)
    {
        _cellNumber = id;
        _cellColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        ChangeColor();
    }

    public void DestroyWall(WallOrientation orientation)
    {
        GameObject wallParent = this.transform.GetChild(2).gameObject;
        switch (orientation)
        {
            case WallOrientation.Left:
                wallParent.transform.GetChild(0).gameObject.SetActive(false); 
                break;
            case WallOrientation.Right:
                wallParent.transform.GetChild(1).gameObject.SetActive(false); 
                break;
            case WallOrientation.Up:
                wallParent.transform.GetChild(2).gameObject.SetActive(false); 
                break;
            case WallOrientation.Down:
                wallParent.transform.GetChild(3).gameObject.SetActive(false); 
                break;
            _ : break;
        }
    }
}
[System.Serializable]
public enum WallOrientation
{
    Right,
    Left,
    Up,
    Down
}
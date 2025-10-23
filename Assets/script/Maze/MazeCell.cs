using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeCell : MonoBehaviour
{

    public int _cellNumber;

    public Color cellColor;
    
    public List<MazeCell> _neighbordsCells = new List<MazeCell>();


    public void ChangeColor()
    {
        transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material.color = cellColor;
    }

    public void Init(int id)
    {
        _cellNumber = id;
        cellColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        ChangeColor();
    }
}

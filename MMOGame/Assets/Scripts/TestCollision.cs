using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TestCollision : MonoBehaviour
{
    public Tilemap _tileMap;
    public TileBase _tile;

    void Start()
    {
        _tileMap.SetTile(new Vector3Int(0, 0, 0), _tile);
    }

    void Update()
    {
        List<Vector3Int> blockedTiles = new List<Vector3Int>();

        foreach (Vector3Int pos in _tileMap.cellBounds.allPositionsWithin)
        {
            TileBase tile = _tileMap.GetTile(pos);
            if (tile != null)
                blockedTiles.Add(pos);
        }
    }
}

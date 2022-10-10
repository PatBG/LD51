using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Unit))]

public class EnemyAI : MonoBehaviour
{
    private Unit _unit;

    // Start is called before the first frame update
    void Start()
    {
        _unit = GetComponent<Unit>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_unit.HP <=0)
            return;

        if (_unit.CanAttackNow)
        {
            List<Vector3Int> tiles = _unit.GetAttackTiles();
            if (tiles.Count > 0)
            {
                _unit.AttackTo(ChooseFrontTile(transform.position, transform.forward, tiles));        // Attack target in front or choose the one with less rotation
            }
        }
        if (_unit.CanMoveNow)
        {
            List<Vector3Int> tiles = _unit.GetOpponentTiles(_unit.Tile);
            if (tiles.Count > 0)
            {
                // Opponent in range, stay here and wait for attack
            }
            else
            {
                tiles = _unit.GetMoveTiles();
                if (tiles.Count > 0)
                {
                    List<Vector3Int> nearOpponentTiles = new();
                    foreach(Vector3Int nearTile in tiles)
                    {
                        if (_unit.GetOpponentTiles(nearTile).Count > 0)     // Search a movable tile that has an opponent in range
                        {
                            nearOpponentTiles.Add(nearTile);
                        }
                    }
                    if (nearOpponentTiles.Count > 0)                        // Choose tiles with opponent in range if possible
                    {
                        tiles = nearOpponentTiles;
                    }
                    if (Random.Range(0, 100) < 40)
                        _unit.MoveTo(tiles[Random.Range(0, tiles.Count)]);                              // Move at random location
                    else
                        _unit.MoveTo(ChooseFrontTile(transform.position, transform.forward, tiles));    // Move in front or with less rotation
                }
            }
        }
    }

    /// <summary>
    /// Choose front tile or tile with the less rotation.
    /// </summary>
    private Vector3Int ChooseFrontTile(Vector3 origin, Vector3 direction, List<Vector3Int> tiles)
    {
        //Debug.Log("ChooseFrontTile() direction = " + direction + "\r\n");
        List<Tuple<float, Vector3Int>> tuples = new();
        foreach (Vector3Int tile in tiles)
        {
            float angle = Vector3.Angle(MapManager.GetPositionFromTile(tile) - origin, direction);
            tuples.Add(new(angle, tile));
            //Debug.Log("Add(" + tuples.Last().Item1 + ", " + tuples.Last().Item2 + ")\r\n");
        }
        tuples.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
        Vector3Int v = tuples.First().Item2;
        return v;
    }
}
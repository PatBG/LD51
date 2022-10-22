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
    bool _previousIsReady = false;
    float _timePseudoPause;

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

        if (_previousIsReady != _unit.IsReady )
        {
            _previousIsReady = _unit.IsReady;
            if (_previousIsReady)
            {
                // Simulate a random pause between each turn
                _timePseudoPause = MapManager.GameTime + Random.Range(Mathf.Min(1, MapManager.TurnDuration / 2), MapManager.TurnDuration / 2);
            }
        }
        if (_previousIsReady && _timePseudoPause > MapManager.GameTime)
        {
            return;
        }
        if (_unit.CanAttackNow)
        {
            List<Tile> tiles = _unit.GetAttackTiles();
            if (tiles.Count > 0)
            {
                _unit.AttackTo(ChooseFrontTile(transform.position, _unit.Pivot.forward, tiles));        // Attack target in front or choose the one with less rotation
            }
        }
        if (_unit.CanMoveNow)
        {
            List<Tile> tiles = _unit.GetOpponentTiles(_unit.Tile);
            if (tiles.Count > 0)
            {
                // Opponent in range, stay here and wait for attack
            }
            else
            {
                tiles = _unit.GetMoveTiles();
                if (tiles.Count > 0)
                {
                    List<Tile> nearOpponentTiles = new();
                    foreach(Tile nearTile in tiles)
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
                        _unit.MoveTo(ChooseFrontTile(transform.position, _unit.Pivot.forward, tiles));    // Move in front or with less rotation
                }
            }
        }
    }

    /// <summary>
    /// Choose front tile or tile with the less rotation.
    /// </summary>
    private Tile ChooseFrontTile(Vector3 origin, Vector3 direction, List<Tile> tiles)
    {
        //Debug.Log("ChooseFrontTile() direction = " + direction + "\r\n");
        List<Tuple<float, Tile>> tuples = new();
        foreach (Tile tile in tiles)
        {
            float angle = Vector3.Angle(tile.GetPosition() - origin, direction);
            tuples.Add(new(angle, tile));
            //Debug.Log("Add(" + tuples.Last().Item1 + ", " + tuples.Last().Item2 + ")\r\n");
        }
        tuples.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
        Tile v = tuples.First().Item2;
        return v;
    }
}

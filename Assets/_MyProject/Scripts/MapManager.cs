using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public GameObject[] TileGrass;

    public static int SizeX = 30;
    public static int SizeZ = 30;

    public static float TurnDuration = 10;      // Every 10 seconds

    public static float GameTime;
    public static float GameTimeScale;

    // Start is called before the first frame update
    void Start()
    {
        GameTime = Time.time;
        GameTimeScale = 1;

        // Generate map
        int layermask = LayerMask.NameToLayer("Tiles");
        for (int x = 0; x < SizeX; x++)
        {
            for (int z = 0; z < SizeZ; z++)
            {
                int i = (x % 2) == 0 ? (z % 3) : ((z + 2) % 3);
                GameObject go = Instantiate(TileGrass[i], GetPositionFromTile(new Vector3Int(x, 0, z)), Quaternion.identity, transform);
                go.name = "Tile_" + x + "_" + z;
                go.layer = layermask;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameTime += Time.deltaTime * GameTimeScale;
    }

    public static Vector3Int GetTileFromPosition(Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x), 
            0, 
            (Mathf.RoundToInt(position.x) % 2) == 0 ? Mathf.RoundToInt(position.z) : Mathf.RoundToInt(position.z - 0.5f));
    }

    public static Vector3 GetPositionFromTile(Vector3Int tile)
    {
        return new Vector3(
            tile.x, 
            0, 
            (tile.x % 2) == 0 ? tile.z : tile.z + 0.5f);
    }

    public static readonly Vector3Int[] Adjacent0 = new Vector3Int[6]
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, -1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, -1),
        new Vector3Int(-1, 0, 0),
    };

    public static readonly Vector3Int[] Adjacent1 = new Vector3Int[6]
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(-1, 0, 1),
    };

    public static bool IsAllowed(Vector3Int tile)
    {
        bool isAllowed = (tile.x >= 0 && tile.x < SizeX && tile.z >= 0 && tile.z < SizeZ);
        return isAllowed;
    }

    public static bool IsMoveAllowed(Vector3Int tile)
    {
        bool isAllowed = IsAllowed(tile);
        if (isAllowed)
        {
            Unit unit = Unit.GetUnit(tile);
            isAllowed = (unit == null);
        }
        return isAllowed;
    }

    public static bool IsAttackAllowed(Vector3Int tile, bool isEnemy)
    {
        bool isAllowed = IsAllowed(tile);
        if (isAllowed)
        {
            Unit unit = Unit.GetUnit(tile);
            isAllowed = (unit != null) && (unit.IsEnemy != isEnemy);
        }
        return isAllowed;
    }

    public void SetGameSpeed(float speed)
    {
        GameTimeScale = speed;
    }
}

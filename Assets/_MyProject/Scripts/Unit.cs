using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Transform Model;
    private Animator _animator;

    public bool IsEnemy;

    public float HP;
    public float MaxHP;

    public float Attack;
    public float Defense;

    public bool CanMove;
    public bool CanAttack;

    public bool CanMoveNow { get { return CanMove && TimeNextMove <= Time.time; } }
    public bool CanAttackNow { get { return CanAttack && TimeNextAttack <= Time.time; } }

    public float TimeNextMove;
    public float TimeNextAttack;

    private float _initialY;
    public Vector3Int Tile { get { return MapManager.GetTileFromPosition(transform.position); } }

    // -------------------------------------------------------------[ BEGIN of static code ]-------
    private static readonly List<Unit> _list = new();

    public static Unit GetUnit(Vector3Int tile)
    {
        foreach (Unit unit in _list)
        {
            if (unit.Tile == tile)
            {
                return unit;
            }
        }
        return null;
    }

    public static int CountUnits(bool isEnemy)
    {
        int count = 0;
        foreach (Unit unit in _list)
        {
            if (unit.IsEnemy == isEnemy)
            {
                count++;
            }
        }
        return count;
    }

    public static void HackClearList()
    {
        _list.Clear();
    }

    // -------------------------------------------------------------[ END of static code ]-------

    // Start is called before the first frame update
    void Start()
    {
        _initialY = Model.position.y;
        _list.Add(this);
        if (Model != null)
        {
            _animator = Model.GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsEnemy)
        {
            SetAnimationBool("Ready", (CanMoveNow && GetMoveTiles().Count > 0) || (CanAttackNow && GetAttackTiles().Count > 0));
        }
    }

    public List<Vector3Int> GetMoveTiles()
    {
        List<Vector3Int> moveTiles = new();
        if (CanMoveNow)
        {
            Vector3Int[] adjacent = (Tile.x % 2) == 0 ? MapManager.Adjacent0 : MapManager.Adjacent1;
            foreach (Vector3Int delta in adjacent)
            {
                Vector3Int moveTile = Tile + delta;
                if (MapManager.IsMoveAllowed(moveTile))
                {
                    moveTiles.Add(moveTile);
                }
            }
        }
        return moveTiles;
    }

    public List<Vector3Int> GetAttackTiles()
    {
        List<Vector3Int> attackTiles = new();
        if (CanAttackNow)
        {
            attackTiles.AddRange(GetOpponentTiles(Tile));
        }
        return attackTiles;
    }

    public List<Vector3Int> GetOpponentTiles(Vector3Int tile)
    {
        List<Vector3Int> opponentsTiles = new();
        Vector3Int[] adjacent = (tile.x % 2) == 0 ? MapManager.Adjacent0 : MapManager.Adjacent1;
        foreach (Vector3Int delta in adjacent)
        {
            Vector3Int attackTile = tile + delta;
            if (MapManager.IsAttackAllowed(attackTile, IsEnemy))
            {
                opponentsTiles.Add(attackTile);
            }
        }
        return opponentsTiles;
    }

    public void MoveTo(Vector3Int tile)
    {
        Vector3 previousPosition = transform.position;
        transform.position = MapManager.GetPositionFromTile(tile);                              // Move to tile
        transform.rotation = Quaternion.LookRotation(transform.position - previousPosition);    // Turn to the move direction
        TimeNextMove = Time.time + MapManager.TurnDuration;                                     // Time for next move
        Debug.Log(name + " move to: " + tile + "\r\n");

        SetAnimationTrigger("Move");
    }

    public void AttackTo(Vector3Int tile)
    {
        Unit defenderUnit = Unit.GetUnit(tile);
        Debug.Assert(defenderUnit != null, "Attack empty tile: " + tile);
        
        // Turn attacker and defender face to face
        transform.rotation = Quaternion.LookRotation(defenderUnit.transform.position - transform.position);
        defenderUnit.transform.rotation = Quaternion.LookRotation(transform.position - defenderUnit.transform.position);

        // Calculate attack damage
        int damage = Mathf.FloorToInt(Attack - defenderUnit.Defense);
        defenderUnit.Damage(damage);

        TimeNextMove = TimeNextAttack = Time.time + MapManager.TurnDuration;                // Time for next move and attack

        SetAnimationTrigger("Attack");
    }

    public string Description()
    {
        string text = name + "  ";
        if (CanAttack)
        {
            text += "  Attack " + Attack;
        }        
        text += "  Defense " + Defense;
        text += "  HP " + HP + "/" + MaxHP;
        if (CanMove && !CanMoveNow)
        {
            int sec = Mathf.RoundToInt(TimeNextMove - Time.time);
            if (sec > 0)
            {
                text += "  " + new String('⌂', Mathf.FloorToInt(sec));
            }
        }
        return text;
    }

    private void Damage(int damage)
    {
        Debug.Log(name + " with " + HP + " HP get " + damage + " damage(s).\r\n");
        HP = Mathf.Max(0, HP - damage);
        if (HP > 0)
        {
            SetAnimationTrigger("Hurt");
        }
        else
        {
            Die();
        }
    }

    //private IEnumerator Die()
    private void Die()
    {
        Debug.Log(name + " is dead.\r\n");
        _list.Remove(this);
        SetAnimationTrigger("Die");
        //Model.parent = null;                // Detach the model from its parents to let the corpse but destroy the unit
        //yield return null;
        //Destroy(gameObject);
    }

    private void SetAnimationTrigger(string name)
    {
        if (_animator != null)
        {
            _animator.SetTrigger(name);
        }
    }

    private void SetAnimationBool(string name, bool value)
    {
        if (_animator != null)
        {
            _animator.SetBool(name, value);
        }
    }


}

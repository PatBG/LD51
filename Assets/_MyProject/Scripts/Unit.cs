using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class Unit : MonoBehaviour
{
    // ------------------------------------------------------------------------
    public const float BaseAttack = 0;
    public const float BaseDefense = 10;
    public const float BaseHP = 10;
    public const float AttackCoef = 1;
    public const float AttackBonusBackstab = 2;
    public const float AttackBonusHalfBackstab = 1;
    public const float DefenseBonusFlanking = 1;
    public const float DefenseMalusLastHits = 1;
    // ------------------------------------------------------------------------

    public Transform Model;
    private Animator _animator;

    public bool IsEnemy;

    public float HP;
    public float MaxHP;

    public float Attack;
    public float Defense;

    public bool CanMove;
    public bool CanAttack;

    public bool CanMoveNow { get { return CanMove && _timeNextMove <= Time.time; } }
    public bool CanAttackNow { get { return CanAttack && _timeNextAttack <= Time.time; } }

    private float _timeNextMove;
    private float _timeNextAttack;
    private readonly List<float> _lastHits = new();
    public int LastHitsCount => _lastHits.Count;

    public Vector3Int Tile { get { return MapManager.GetTileFromPosition(transform.position); } }

    public bool IsReady { get => (CanMoveNow && GetMoveTiles().Count > 0) || (CanAttackNow && GetAttackTiles().Count > 0); }

    // -------------------------------------------------------------[ BEGIN of static code ]-------
    private static readonly List<Unit> _globalList = new();

    public static Unit GetUnit(Vector3Int tile)
    {
        foreach (Unit unit in _globalList)
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
        foreach (Unit unit in _globalList)
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
        _globalList.Clear();
    }

    // -------------------------------------------------------------[ END of static code ]-------

    // Start is called before the first frame update
    void Start()
    {
        _globalList.Add(this);
        if (Model != null)
        {
            _animator = Model.GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Purge last hits list (number of hits received this turn)
        for (int i = _lastHits.Count - 1; i >= 0; i--)
        {
            if ((_lastHits[i] + MapManager.TurnDuration) <= Time.time)
            {
                _lastHits.RemoveAt(i);
            }
        }

        SetAnimationBool("Ready", IsReady);
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

    public List<Vector3Int> GetFlankTiles(Vector3Int attackTile)
    {
        List<Vector3Int> flankTiles = new();

        Vector3Int[] adjacent = (attackTile.x % 2) == 0 ? MapManager.Adjacent0 : MapManager.Adjacent1;
        List<Vector3Int> tiles = new();
        int index = -1;
        foreach (Vector3Int delta in adjacent)
        {
            Vector3Int tile = attackTile + delta;
            tiles.Add(tile);
            if (tile == Tile)
            {
                index = tiles.Count - 1;            // Index if the current unit
            }
        }

        // Flank left
        int indexFlankedTile = (index + 5) % 6;
        Vector3Int flankedTile = tiles[indexFlankedTile];
        if (MapManager.IsAllowed(flankedTile))
        {
            flankTiles.Add(flankedTile);
        }

        // Flank right
        indexFlankedTile = (index + 1) % 6;
        flankedTile = tiles[indexFlankedTile];
        if (MapManager.IsAllowed(flankedTile))
        {
            flankTiles.Add(flankedTile);
        }

        return flankTiles;
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
        _timeNextMove = Time.time + MapManager.TurnDuration;                                     // Time for next move
        //Debug.Log(name + " move to: " + tile + "\r\n");

        SetAnimationTrigger("Move");
    }

    public void AttackTo(Vector3Int tile)
    {
        Unit defenderUnit = Unit.GetUnit(tile);
        Debug.Assert(defenderUnit != null, "Attack empty tile: " + tile);
        
        // Turn attacker to the defender
        transform.rotation = Quaternion.LookRotation(defenderUnit.transform.position - transform.position);

        // Calculate base attack
        float baseAttack = BaseAttack + Attack;
        
        // Calculate backstab bonus
        float attackDirection = transform.rotation.eulerAngles.y;
        float defenderDirection = defenderUnit.transform.rotation.eulerAngles.y;
        float deltaDirection = MathF.Abs(Mathf.DeltaAngle(attackDirection, defenderDirection));
        float backstabBonus = (deltaDirection <= 0) ? AttackBonusBackstab : (deltaDirection <= 60) ? AttackBonusHalfBackstab : 0;

        // Resulting attack
        float attack = baseAttack + backstabBonus;

        // Turn defender to the attacker 
        defenderUnit.transform.rotation = Quaternion.LookRotation(transform.position - defenderUnit.transform.position);

        // Calculate base defense
        float baseDefense = BaseDefense + defenderUnit.Defense;

        // TODO: implement flanking bonus and recent hits malus
        float flankingBonus = 0;
        List<Vector3Int> flankTiles = GetFlankTiles(defenderUnit.Tile);
        foreach(Vector3Int flankTile in flankTiles)
        {
            Unit flankUnit = GetUnit(flankTile);
            if (flankUnit != null && flankUnit.IsEnemy == defenderUnit.IsEnemy)
            {
                flankingBonus += DefenseBonusFlanking;
            }
        }

        // Calculate the defense malus based on hit during the last turn
        float recentHitsMalus = defenderUnit.LastHitsCount * DefenseMalusLastHits;

        // Resulting defense
        float defense = Mathf.Max(baseDefense + flankingBonus - recentHitsMalus, 1);

        // Calculate resulting damage
        int damage = Mathf.FloorToInt(BaseHP * AttackCoef * attack / defense);

        Debug.Log(name + " attacks " + defenderUnit.name + " :" +
            " attack=" + baseAttack + "+" + backstabBonus + 
            " defense=" + baseDefense + "+" + flankingBonus + "-" + recentHitsMalus + " -> damage=" + damage + "\r\n");
        defenderUnit.Damage(damage);

        _timeNextAttack = Time.time + MapManager.TurnDuration;              // Time for next attack
        _timeNextMove = _timeNextAttack;                                    // Time for next move is also affected because attack ends the turn

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
        if (LastHitsCount > 0)
        {
            text += "(-" + LastHitsCount + ")";
        }
        text += "  HP " + HP + "/" + MaxHP;
        if (CanMove && !CanMoveNow)
        {
            int sec = Mathf.RoundToInt(_timeNextMove - Time.time);
            if (sec > 0)
            {
                text += "  " + new String('⌂', Mathf.FloorToInt(sec));
            }
        }
        return text;
    }

    private void Damage(int damage)
    {
        _lastHits.Add(Time.time);
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
        _globalList.Remove(this);
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

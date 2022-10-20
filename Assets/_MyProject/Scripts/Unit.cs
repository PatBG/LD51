using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class Unit : MonoBehaviour
{
    // ------------------------------------------------------------------------
    public const float AttackBonusBackstab = 2;
    public const float AttackBonusHalfBackstab = 1;
    public const float DefenseBonusFlanking = 1;
    public const float DefenseMalusLastHits = 1;
    // ------------------------------------------------------------------------

    public Transform Pivot;
    public Transform Model;
    private Animator _animator;

    public AudioClip SoundMove;
    public AudioClip SoundHitShield;
    public AudioClip SoundHit;

    public bool IsEnemy;

    public float HP;
    public GameObject IconHeart;

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
        Pivot.transform.rotation = Quaternion.LookRotation(transform.position - previousPosition);    // Turn to the move direction
        _timeNextMove = Time.time + MapManager.TurnDuration;                                     // Time for next move
        //Debug.Log(name + " move to: " + tile + "\r\n");

        SetAnimationTrigger("Move");
        GetComponent<AudioSource>().PlayOneShot(SoundMove);
    }

    public struct AttackData
    {
        public Unit attacker;
        public Unit defender;
        public float backstabAttackBonus;
        public float attack;
        public float flankingDefenseBonus;
        public float recentHitsDefenseMalus;
        public float defense;
        public int damage;
    }
    public static string AttackDataToString(AttackData attackData)
    {
        string txt = "\r\n";
        if (attackData.attacker == null)
        {
            txt += "no attacker\r\n";
        }
        else
        {
            txt += "attacker:" + attackData.attacker.name + "\r\n";
            txt += "baseAttack:" + attackData.attacker.Attack + "\r\n";
            txt += "backstabAttackBonus:" + attackData.backstabAttackBonus + "\r\n";
            txt += "attack:" + attackData.attack + "\r\n";
        }
        if (attackData.defender == null)
        {
            txt += "no defender\r\n";
        }
        else
        {
            txt += "defender:" + attackData.defender.name + "\r\n";
            txt += "baseDefense:" + attackData.defender.Defense + "\r\n";
            txt += "flankingDefenseBonus:" + attackData.flankingDefenseBonus + "\r\n";
            txt += "recentHitsDefenseMalus:" + attackData.recentHitsDefenseMalus + "\r\n";
            txt += "defense:" + attackData.defense + "\r\n";
            txt += "damage:" + attackData.damage + "\r\n";
        }
        return txt;
    }

    public AttackData CalculateAttack(Vector3Int tile)
    {
        AttackData data = new();

        data.attacker = this;
        data.defender = Unit.GetUnit(tile);
        Debug.Assert(data.defender != null, "Attack empty tile: " + tile);

        // Turn attacker to the defender
        Quaternion attackerRotation = Quaternion.LookRotation(data.defender.transform.position - transform.position);

        // Calculate backstab bonus
        float attackDirection = attackerRotation.eulerAngles.y;
        float defenderDirection = data.defender.Pivot.transform.rotation.eulerAngles.y;
        float deltaDirection = MathF.Abs(Mathf.DeltaAngle(attackDirection, defenderDirection));
        // Backstab is 0° and half backstab is 60° (possible direction are 0, 60, 120, 180)
        data.backstabAttackBonus = (deltaDirection < 30) ? AttackBonusBackstab : (deltaDirection < 90) ? AttackBonusHalfBackstab : 0;

        // Resulting attack
        data.attack = Attack + data.backstabAttackBonus;

        // Calculate flanking bonus
        data.flankingDefenseBonus = 0;
        List<Vector3Int> flankTiles = GetFlankTiles(data.defender.Tile);
        foreach (Vector3Int flankTile in flankTiles)
        {
            Unit flankUnit = GetUnit(flankTile);
            if (flankUnit != null && flankUnit.IsEnemy == data.defender.IsEnemy)
            {
                data.flankingDefenseBonus += DefenseBonusFlanking;
            }
        }

        // Calculate the defense malus based on hit during the last turn
        data.recentHitsDefenseMalus = data.defender.LastHitsCount * DefenseMalusLastHits;

        // Resulting defense
        data.defense = data.defender.Defense + data.flankingDefenseBonus - data.recentHitsDefenseMalus;

        // Calculate resulting damage (cannot be less than zero)
        data.damage = Mathf.Max(0, Mathf.RoundToInt(data.attack - data.defense));

        return data;
    }

    public void AttackTo(Vector3Int tile)
    {
        AttackData attackData = CalculateAttack(tile);

        // Turn attacker to the defender
        Pivot.transform.rotation = Quaternion.LookRotation(attackData.defender.transform.position - transform.position);

        // Turn defender to the attacker 
        attackData.defender.Pivot.transform.rotation = Quaternion.LookRotation(transform.position - attackData.defender.transform.position);

        Debug.Log(name + " attacks " + attackData.defender.name + " :" +
            " attack=" + Attack + "+" + attackData.backstabAttackBonus + 
            " defense=" + attackData.defender.Defense + "+" + attackData.flankingDefenseBonus + "-" + attackData.recentHitsDefenseMalus + " -> damage=" + attackData.damage + "\r\n");
        attackData.defender.Damage(attackData);

        _timeNextAttack = Time.time + MapManager.TurnDuration;              // Time for next attack
        _timeNextMove = _timeNextAttack;                                    // Time for next move is also affected because attack ends the turn

        SetAnimationTrigger("Attack");

        List<AudioClip> audioList = new();
        audioList.Add((attackData.attack > attackData.damage) ? SoundHitShield : SoundHit);
        audioList.Add((attackData.damage <= 0) ? SoundHitShield : SoundHit);
        StartCoroutine(playEngineSound(audioList));
    }

    IEnumerator playEngineSound(List<AudioClip> audioList)
    {
        foreach(AudioClip audioClip in audioList)
        {
            GetComponent<AudioSource>().PlayOneShot(audioClip);
            yield return new WaitForSeconds(audioClip.length + 0.1f);
        }
    }

    private void Damage(AttackData attackData)
    {
        _lastHits.Add(Time.time);
        StartDamagePopup(attackData);
        HP = Mathf.Max(0, HP - attackData.damage);
        if (HP > 0)
        {
            SetAnimationTrigger("Hurt");
        }
        else
        {
            Die();
        }
    }

    private void StartDamagePopup(AttackData attackData)
    {
        GameObject damagePopup = new("DamagePopup", typeof(DamagePopup));
        damagePopup.transform.parent = transform;

        int index = Mathf.RoundToInt(attackData.defense * AttackUI.CoefIconPosition);
        for (int i = 0; i < attackData.damage; i++)
        {
            Instantiate(IconHeart, new Vector3(((float)index++) * AttackUI.CoefIconPosition, 0, 0), Quaternion.identity, damagePopup.transform);
        }

        damagePopup.transform.localPosition = Vector3.up;
        damagePopup.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        damagePopup.transform.localScale = AttackUI.ScaleFactor;
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

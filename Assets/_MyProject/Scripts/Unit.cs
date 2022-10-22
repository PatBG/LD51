using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class Unit : MonoBehaviour
{
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

    public bool CanMoveNow { get { return CanMove && _timeNextMove <= MapManager.GameTime; } }
    public bool CanAttackNow { get { return CanAttack && _timeNextAttack <= MapManager.GameTime; } }

    private float _timeNextMove;
    private float _timeNextAttack;
    private readonly List<float> _lastHits = new();
    public int LastHitsCount => _lastHits.Count;

    public Tile Tile { get { return Tile.GetTile(transform.position); } }

    public bool IsReady { get => (CanMoveNow && GetMoveTiles().Count > 0) || (CanAttackNow && GetAttackTiles().Count > 0); }

    // -------------------------------------------------------------[ BEGIN of static code ]-------
    private static readonly List<Unit> _globalList = new();

    public static Unit GetUnit(Tile tile)
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
            if ((_lastHits[i] + MapManager.TurnDuration) <= MapManager.GameTime)
            {
                _lastHits.RemoveAt(i);
            }
        }

        SetAnimationBool("Ready", IsReady);
    }

    public List<Tile> GetMoveTiles()
    {
        List<Tile> moveTiles = new();
        if (CanMoveNow)
        {
            foreach (Tile moveTile in Tile.GetAdjacentTiles())
            {
                if (MapManager.IsMoveAllowed(moveTile))
                {
                    moveTiles.Add(moveTile);
                }
            }
        }
        return moveTiles;
    }


    public List<Tile> GetAttackTiles()
    {
        List<Tile> attackTiles = new();
        if (CanAttackNow)
        {
            attackTiles.AddRange(GetOpponentTiles(Tile));
        }
        return attackTiles;
    }

    public List<Tile> GetOpponentTiles(Tile tile)
    {
        List<Tile> opponentsTiles = new();
        foreach (Tile attackTile in tile.GetAdjacentTiles())
        {
            if (MapManager.IsAttackAllowed(attackTile, IsEnemy))
            {
                opponentsTiles.Add(attackTile);
            }
        }
        return opponentsTiles;
    }

    public void MoveTo(Tile tile)
    {
        Vector3 previousPosition = transform.position;
        transform.position = tile.GetPosition();                              // Move to tile
        Pivot.transform.rotation = Quaternion.LookRotation(transform.position - previousPosition);    // Turn to the move direction
        _timeNextMove = MapManager.GameTime + MapManager.TurnDuration;                                     // Time for next move
        //Debug.Log(name + " move to: " + tile + "\r\n");

        SetAnimationTrigger("Move");
        GetComponent<AudioSource>().PlayOneShot(SoundMove);
    }

    public void AttackTo(Tile tile)
    {
        AttackData attackData = new();
        Unit defender = Unit.GetUnit(tile);
        Debug.Assert(defender != null, "Attack empty tile " + tile);
        attackData.CalculateAttack(this, defender);

        // Turn attacker to the defender
        Pivot.transform.rotation = Quaternion.LookRotation(attackData.Defender.transform.position - transform.position);

        // Turn defender to the attacker 
        attackData.Defender.Pivot.transform.rotation = Quaternion.LookRotation(transform.position - attackData.Defender.transform.position);

        Debug.Log(name + " attacks " + attackData.Defender.name + " :" +
            " attack=" + Attack + "+" + attackData.BackstabAttackBonus + 
            " defense=" + attackData.Defender.Defense + "+" + attackData.FlankingDefenseBonus + "-" + attackData.RecentHitsDefenseMalus + " -> damage=" + attackData.Damage + "\r\n");
        attackData.Defender.Damage(attackData);

        _timeNextAttack = MapManager.GameTime + MapManager.TurnDuration;              // Time for next attack
        _timeNextMove = _timeNextAttack;                                    // Time for next move is also affected because attack ends the turn

        SetAnimationTrigger("Attack");

        List<AudioClip> audioList = new();
        audioList.Add((attackData.Attack > attackData.Damage) ? SoundHitShield : SoundHit);
        audioList.Add((attackData.Damage <= 0) ? SoundHitShield : SoundHit);
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
        _lastHits.Add(MapManager.GameTime);
        StartDamagePopup(attackData);
        HP = Mathf.Max(0, HP - attackData.Damage);
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

        int index = Mathf.RoundToInt(attackData.Defense * AttackUI.CoefIconPosition);
        for (int i = 0; i < attackData.Damage; i++)
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

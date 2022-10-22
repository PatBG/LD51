using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackData
{
    public const float AttackBonusBackstab = 2;
    public const float AttackBonusHalfBackstab = 1;
    public const float DefenseBonusFlanking = 1;
    public const float DefenseMalusLastHits = 1;

    public Unit Attacker;
    public Unit Defender;
    public float BackstabAttackBonus;
    public float Attack;
    public float FlankingDefenseBonus;
    public float RecentHitsDefenseMalus;
    public float Defense;
    public int Damage;

    public void CalculateAttack(Unit attacker, Unit defender)
    {
        Attacker = attacker;
        Defender = defender;

        // Turn attacker to the defender
        Quaternion attackerRotation = Quaternion.LookRotation(defender.transform.position - attacker.transform.position);

        // Calculate backstab bonus
        float attackDirection = attackerRotation.eulerAngles.y;
        float defenderDirection = defender.Pivot.transform.rotation.eulerAngles.y;
        float deltaDirection = MathF.Abs(Mathf.DeltaAngle(attackDirection, defenderDirection));
        // Backstab is 0° and half backstab is 60° (possible direction are 0, 60, 120, 180)
        BackstabAttackBonus = (deltaDirection < 30) ? AttackBonusBackstab : (deltaDirection < 90) ? AttackBonusHalfBackstab : 0;

        // Resulting attack
        Attack = attacker.Attack + BackstabAttackBonus;

        // Calculate flanking bonus
        FlankingDefenseBonus = 0;
        List<Tile> flankTiles = GetFlankTiles(defender.Tile, attacker.Tile);
        foreach (Tile flankTile in flankTiles)
        {
            Unit flankUnit = Unit.GetUnit(flankTile);
            if (flankUnit != null && flankUnit.IsEnemy == defender.IsEnemy)
            {
                FlankingDefenseBonus += DefenseBonusFlanking;
            }
        }

        // Calculate the defense malus based on hit during the last turn
        RecentHitsDefenseMalus = defender.LastHitsCount * DefenseMalusLastHits;

        // Resulting defense
        Defense = defender.Defense + FlankingDefenseBonus - RecentHitsDefenseMalus;

        // Calculate resulting damage (cannot be less than zero)
        Damage = Mathf.Max(0, Mathf.RoundToInt(Attack - Defense));
    }

    public List<Tile> GetFlankTiles(Tile defenderTile, Tile attackerTile)
    {
        List<Tile> flankTiles = new();

        List<Tile> tiles = defenderTile.GetAdjacentTiles();
        int index = tiles.FindIndex(a => a == attackerTile);        // Index of the tile of the defender

        if (index >= 0)
        {
            // Flank left
            int indexFlankedTile = (index + 5) % 6;
            Tile flankedTile = tiles[indexFlankedTile];
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
        }

        return flankTiles;
    }

    public override string ToString()
    {
        string txt = "\r\n";
        if (Attacker == null)
        {
            txt += "no attacker\r\n";
        }
        else
        {
            txt += "attacker:" + Attacker.name + "\r\n";
            txt += "baseAttack:" + Attacker.Attack + "\r\n";
            txt += "backstabAttackBonus:" + BackstabAttackBonus + "\r\n";
            txt += "attack:" + Attack + "\r\n";
        }
        if (Defender == null)
        {
            txt += "no defender\r\n";
        }
        else
        {
            txt += "defender:" + Defender.name + "\r\n";
            txt += "baseDefense:" + Defender.Defense + "\r\n";
            txt += "flankingDefenseBonus:" + FlankingDefenseBonus + "\r\n";
            txt += "recentHitsDefenseMalus:" + RecentHitsDefenseMalus + "\r\n";
            txt += "defense:" + Defense + "\r\n";
            txt += "damage:" + Damage + "\r\n";
        }
        return txt;
    }
}

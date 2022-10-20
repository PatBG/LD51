using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;
using UnityEngine.UIElements;
using static Unit;

public class AttackUI : MonoBehaviour
{
    public GameObject IconHeart;
    public GameObject IconArmor;
    public GameObject IconShield;
    public GameObject IconFlanked;
    public GameObject IconSword;
    public GameObject IconBackstab;
    public GameObject IconHit;

    public bool IsRefreshed;
    public Vector3Int AttackerTile;
    public Vector3Int DefenderTile;

    private GameObject _attackerHUD;
    private GameObject _defenderHUD;

    public const float CoefIconPosition = 1;
    public static readonly Vector3 ScaleFactor = Vector3.one * 0.1f;

    // Update is called once per frame
    void Update()
    {
        ClearAll();
        if (IsRefreshed)
        {
            InitHUD();
        }
    }

    public void ClearAll()
    {
        if (_attackerHUD != null)
        {
            Destroy(_attackerHUD);
            _attackerHUD = null;
        }
        if (_defenderHUD != null)
        {
            Destroy(_defenderHUD);
            _defenderHUD = null;
        }
    }

    private void InitHUD()
    {
        Unit attacker = Unit.GetUnit(AttackerTile);
        Unit defender = Unit.GetUnit(DefenderTile);
        if (attacker != null)
        {
            _attackerHUD = new GameObject("AttackerHUD");
            _attackerHUD.transform.parent = attacker.transform;
            if (defender == null)
            {
                AttackData attackData = new()
                {
                    attacker = attacker,
                    backstabAttackBonus = 0
                };
                InitAttacker(attackData, _attackerHUD.transform);
            }
            else
            {
                _defenderHUD = new GameObject("DefenderHUD");
                _defenderHUD.transform.parent = defender.transform;

                AttackData attackData = attacker.CalculateAttack(defender.Tile);
                InitAttacker(attackData, _attackerHUD.transform);
                InitDefender(attackData, _defenderHUD.transform);

                _defenderHUD.transform.localPosition = Vector3.up;
                _defenderHUD.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                _defenderHUD.transform.localScale = ScaleFactor;
            }
            _attackerHUD.transform.localPosition = Vector3.up;
            _attackerHUD.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            _attackerHUD.transform.localScale = ScaleFactor;
        }
    }

    private void InitAttacker(Unit.AttackData attackData, Transform hud)
    {
        int index = 0;
        // Attack data
        for (int i = 0; i < attackData.attacker.Attack; i++)
        {
            Instantiate(IconSword, new Vector3(((float)index++) * CoefIconPosition, 1 * CoefIconPosition, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.backstabAttackBonus; i++)
        {
            Instantiate(IconBackstab, new Vector3(((float)index++) * CoefIconPosition, 1 * CoefIconPosition, 0), Quaternion.identity, hud);
        }
        // Defense data
        index = 0;
        if (attackData.attacker.Defense > 0)
        {
            Instantiate(IconArmor, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.attacker.Defense > 1)
        {
            Instantiate(IconShield, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.attacker.HP; i++)
        {
            Instantiate(IconHeart, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        index = 0;
        float recentHitsDefenseMalus = attackData.attacker.LastHitsCount * DefenseMalusLastHits;
        for (int i = 0; i < recentHitsDefenseMalus; i++)
        {
            Instantiate(IconHit, new Vector3(((float)index++) * CoefIconPosition, 0, -0.1f), Quaternion.identity, hud);
        }
    }

    private void InitDefender(Unit.AttackData attackData, Transform hud)
    {
        int index = 0;
        for (int i = 0; i < attackData.flankingDefenseBonus; i++)
        {
            Instantiate(IconFlanked, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.defender.Defense > 0)
        {
            Instantiate(IconArmor, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.defender.Defense > 1)
        {
            Instantiate(IconShield, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.defender.HP; i++)
        {
            Instantiate(IconHeart, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        index = 0;
        for (int i = 0; i < attackData.recentHitsDefenseMalus; i++)
        {
            Instantiate(IconHit, new Vector3(((float)index++) * CoefIconPosition, 0, -0.1f), Quaternion.identity, hud);
        }
    }
}

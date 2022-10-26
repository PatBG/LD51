using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Tile SelectedTile;
    public Tile HoveredTile;

    private GameObject _selectedHUD;
    private GameObject _hoveredHUD;

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
        if (_selectedHUD != null)
        {
            Destroy(_selectedHUD);
            _selectedHUD = null;
        }
        if (_hoveredHUD != null)
        {
            Destroy(_hoveredHUD);
            _hoveredHUD = null;
        }
    }

    private void InitHUD()
    {
        Unit selectedUnit = Unit.GetUnit(SelectedTile);
        Unit hoveredUnit = Unit.GetUnit(HoveredTile);

        if (selectedUnit != null)
        {
            _selectedHUD = new GameObject("AttackerHUD");
            _selectedHUD.transform.parent = selectedUnit.transform;
        }
        if (hoveredUnit != null)
        {
            _hoveredHUD = new GameObject("DefenderHUD");
            _hoveredHUD.transform.parent = hoveredUnit.transform;
        }

        if (selectedUnit != null && hoveredUnit != null && selectedUnit.GetAttackTiles().Contains(hoveredUnit.Tile))
        {
            AttackData attackData = new();
            attackData.CalculateAttack(selectedUnit, hoveredUnit);
            InitAttacker(attackData, _selectedHUD.transform);
            InitDefender(attackData, _hoveredHUD.transform);
        }
        else
        {
            if (selectedUnit != null)
            {
                AttackData attackData = new()
                {
                    Attacker = selectedUnit,
                    BackstabAttackBonus = 0
                };
                InitAttacker(attackData, _selectedHUD.transform);
            }
            if (hoveredUnit != null)
            {
                AttackData attackData = new()
                {
                    Attacker = hoveredUnit,
                    BackstabAttackBonus = 0
                };
                InitAttacker(attackData, _hoveredHUD.transform);
            }
        }

        if (selectedUnit != null)
        {
            _selectedHUD.transform.localPosition = Vector3.up;
            _selectedHUD.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            _selectedHUD.transform.localScale = ScaleFactor;
        }
        if (hoveredUnit != null)
        {
            _hoveredHUD.transform.localPosition = Vector3.up;
            _hoveredHUD.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            _hoveredHUD.transform.localScale = ScaleFactor;
        }
    }

    private void InitAttacker(AttackData attackData, Transform hud)
    {
        int index = 0;
        // Attack data
        for (int i = 0; i < attackData.Attacker.Attack; i++)
        {
            Instantiate(IconSword, new Vector3(((float)index++) * CoefIconPosition, 1 * CoefIconPosition, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.BackstabAttackBonus; i++)
        {
            Instantiate(IconBackstab, new Vector3(((float)index++) * CoefIconPosition, 1 * CoefIconPosition, 0), Quaternion.identity, hud);
        }
        // Defense data
        index = 0;
        if (attackData.Attacker.Defense > 0)
        {
            Instantiate(IconArmor, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.Attacker.Defense > 1)
        {
            Instantiate(IconShield, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.Attacker.HP; i++)
        {
            Instantiate(IconHeart, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        index = 0;
        float recentHitsDefenseMalus = attackData.Attacker.LastHitsCount;
        for (int i = 0; i < recentHitsDefenseMalus; i++)
        {
            Instantiate(IconHit, new Vector3(((float)index++) * CoefIconPosition, 0, -0.1f), Quaternion.identity, hud);
        }
    }

    private void InitDefender(AttackData attackData, Transform hud)
    {
        int index = 0;
        for (int i = 0; i < attackData.FlankingDefenseBonus; i++)
        {
            Instantiate(IconFlanked, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.Defender.Defense > 0)
        {
            Instantiate(IconArmor, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        if (attackData.Defender.Defense > 1)
        {
            Instantiate(IconShield, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        for (int i = 0; i < attackData.Defender.HP; i++)
        {
            Instantiate(IconHeart, new Vector3(((float)index++) * CoefIconPosition, 0, 0), Quaternion.identity, hud);
        }
        index = 0;
        for (int i = 0; i < attackData.RecentHitsDefenseMalus; i++)
        {
            Instantiate(IconHit, new Vector3(((float)index++) * CoefIconPosition, 0, -0.1f), Quaternion.identity, hud);
        }
    }
}

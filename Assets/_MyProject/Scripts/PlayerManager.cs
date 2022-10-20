using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.CanvasScaler;

[RequireComponent(typeof(AttackUI))]

public class PlayerManager : MonoBehaviour
{
    public GameObject HighlightAlly;
    public GameObject HighlightEnemy;
    public GameObject HighlightMove;
    public GameObject HighlightAttack;

    private readonly List<GameObject> _highlights = new();

    public enum SelectionType { None, Ally }

    private SelectionType _selection;
    private Unit _selectionUnit;

    public GameObject PanelEndGame;
    public TextMeshProUGUI TextEndGame;

    public GameObject PanelHelp;

    private AttackUI _attackUI;

    // Start is called before the first frame update
    void Start()
    {
        _attackUI = GetComponent<AttackUI>();
        Debug.Assert(_attackUI != null);

        EnterPanelHelp();
    }

    private bool TileFromMousePosition(out Vector3Int tile)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layermask = 1 << LayerMask.NameToLayer("Tiles");
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, layermask))
        {
            tile = MapManager.GetTileFromPosition(raycastHit.transform.position);
            return true;
        }
        else
        {
            tile = new Vector3Int(-1, -1, -1);
            return false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PanelHelp.activeInHierarchy)
        {
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            EnterPanelHelp();
            return;
        }

        bool isHoveredTile = TileFromMousePosition(out Vector3Int hoveredTile);
        if (Input.GetButtonDown("Fire1"))
        {
            if (isHoveredTile)
            {
                SelectTile(hoveredTile);
            }
            else
            {
                ClearSelection();
            }
        }
        else if (_selection == SelectionType.Ally)
        {
            // Refresh Selected unit (reselect it)
            Vector3Int tile = _selectionUnit.Tile;
            ClearSelection();
            SelectTile(tile, false);

            // If he can attack now, check if we hover an enemy
            if (_selectionUnit.CanAttackNow)
            {
                if (isHoveredTile)
                {
                    bool isEnemy = false;
                    foreach (GameObject go in _highlights)
                    {
                        if (MapManager.GetTileFromPosition(go.transform.position) == hoveredTile)
                        {
                            if (go.name.StartsWith(HighlightAttack.name))
                            {
                                isEnemy = true;
                                break;
                            }
                        }
                    }
                    // An attackable enemy is hovered, update the attack HUD
                    _attackUI.DefenderTile = isEnemy ? hoveredTile : new Vector3Int(-1, -1, -1);
                }
            }
        }
        else
        {
            Unit unit = null;
            if (isHoveredTile)
            {
                unit = Unit.GetUnit(hoveredTile);
                if (unit != null)
                {
                    // Display the HUD for the hovered unit
                    _attackUI.AttackerTile = hoveredTile;
                    _attackUI.DefenderTile = new Vector3Int(-1, -1, -1);
                }
            }
            _attackUI.IsRefreshed = (unit != null);
        }

        CheckEndGame();
    }

    private void CheckEndGame()
    {
        if (!PanelEndGame.activeInHierarchy)
        {
            if (Unit.CountUnits(true) == 0)
            {
                // Victory
                int nb = Unit.CountUnits(false);
                TextEndGame.text =
                    "You win!!!\r\n" +
                    "with " + nb + " guard" + (nb > 0 ? "s" : "") + " left.";
                PanelEndGame.SetActive(true);
            }
            else if (Unit.CountUnits(false) == 0)
            {
                // Defeat
                int nb = Unit.CountUnits(true);
                TextEndGame.text =
                    "You loose!!!\r\n" +
                    "with " + nb + " foe" + (nb > 0 ? "s" : "") + " left.";
                PanelEndGame.SetActive(true);
            }
        }
    }

    private void ClearSelection()
    {
        _selection = SelectionType.None;
        _selectionUnit = null;
        foreach (GameObject go in _highlights)
        {
            Destroy(go);
        }
        _highlights.Clear();
        _attackUI.IsRefreshed = false;
    }

    private void SelectTile(Vector3Int tile, bool isCameraFollow = true)
    {
        //Debug.Log("Select tile: " + tile + "\r\n");
        if (isCameraFollow)
        {
            Vector3 tilePosition = MapManager.GetPositionFromTile(tile);
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(tilePosition);
            float minX = Camera.main.pixelWidth * 0.3f;
            float maxX = Camera.main.pixelWidth * 0.7f;
            float minY = Camera.main.pixelHeight * 0.3f;
            float maxY = Camera.main.pixelHeight * 0.7f;
            // Camera follow is applied only on the screen border
            if (screenPoint.x <= minX || screenPoint.x >= maxX || screenPoint.y <= minY || screenPoint.y >= maxY)
            {
                CameraManager.SetTileTarget(tile);
            }
        }
        if (_selection == SelectionType.None)
        {
            Unit unit = Unit.GetUnit(tile);
            if (unit != null && unit.IsEnemy == false)
            {
                _selectionUnit = unit;
                _selection = SelectionType.Ally;

                // Activate the attackHUD
                _attackUI.AttackerTile = tile;
                _attackUI.DefenderTile = new Vector3Int(-1, -1, -1);
                _attackUI.IsRefreshed = true;

                // Highlight the unit
                _highlights.Add(Instantiate(HighlightAlly, MapManager.GetPositionFromTile(tile), Quaternion.identity, unit.transform));
                // Highlight the movements
                foreach (Vector3Int moveTile in unit.GetMoveTiles())
                {
                    _highlights.Add(Instantiate(HighlightMove, MapManager.GetPositionFromTile(moveTile), Quaternion.identity, unit.transform));
                }
                // Highlight the attacks
                foreach (Vector3Int attackTile in unit.GetAttackTiles())
                {
                    _highlights.Add(Instantiate(HighlightAttack, MapManager.GetPositionFromTile(attackTile), Quaternion.identity, unit.transform));
                }
                return;
            }
        }
        else if (_selection == SelectionType.Ally)
        {
            foreach (GameObject go in _highlights)
            {
                if (MapManager.GetTileFromPosition(go.transform.position) == tile)
                {
                    if (go.name.StartsWith(HighlightMove.name))
                    {
                        _selectionUnit.MoveTo(tile);
                    }
                    else if (go.name.StartsWith(HighlightAttack.name))
                    {
                        _selectionUnit.AttackTo(tile);
                        return;                     // After an attack, the attacker stay selected
                    }
                    else if (go.name.StartsWith(HighlightAlly.name))
                    {
                        ClearSelection();           // If the same unit is reselected: cancel the selection
                        return;
                    }
                }
            }
            ClearSelection();
            SelectTile(tile);                // Clear the previous selection and select the new tile
            return;
        }
        ClearSelection();
    }

    public void RestartGame()
    {
        Unit.HackClearList();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void EnterPanelHelp()
    {
        Time.timeScale = 0;
        PanelHelp.SetActive(true);
    }

    public void QuitPanelHelp()
    {
        PanelHelp.SetActive(false);
        Time.timeScale = 1;
    }
}

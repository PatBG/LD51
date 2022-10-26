using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public enum SelectionType { None, Ally, Enemy }

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

    private bool TileFromMousePosition(out Tile tile)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layermask = 1 << LayerMask.NameToLayer("Tiles");
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, layermask) && !EventSystem.current.IsPointerOverGameObject())
        {
            tile = Tile.GetTile(raycastHit.transform.position);
            return true;
        }
        else
        {
            tile = null;
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

        bool isHoveredTile = TileFromMousePosition(out Tile hoveredTile);
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
            _attackUI.HoveredTile = null;
        }
        else
        {
            if (_selection != SelectionType.None)
            {
                // Refresh Selected unit (reselect it)
                Tile tile = _selectionUnit.Tile;
                ClearSelection();
                SelectTile(tile, false);
            }
            _attackUI.HoveredTile = hoveredTile;
        }
        _attackUI.IsRefreshed = (_attackUI.SelectedTile != null || _attackUI.HoveredTile != null);

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

    private void SelectTile(Tile tile, bool isCameraFollow = true)
    {
        //Debug.Log("Select tile: " + tile + "\r\n");
        if (isCameraFollow)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(tile.GetPosition());
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

        if (_selection == SelectionType.Ally)
        {
            foreach (GameObject go in _highlights)
            {
                if (Tile.GetTile(go.transform.position) == tile)
                {
                    if (go.name.StartsWith(HighlightMove.name))
                    {
                        _selectionUnit.MoveTo(tile);
                        ClearSelection();
                        SelectTile(tile);           // After a move, the destination is selected
                        return;
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
        }
        
        Unit unit = Unit.GetUnit(tile);
        if (unit != null)
        {
            _selectionUnit = unit;
            _selection = unit.IsEnemy ? SelectionType.Enemy : SelectionType.Ally;

            // Activate the attackHUD
            _attackUI.SelectedTile = tile;
            _attackUI.IsRefreshed = true;

            // Highlight the unit
            _highlights.Add(Instantiate(unit.IsEnemy ? HighlightEnemy : HighlightAlly, tile.GetPosition(), Quaternion.identity, unit.transform));

            if (!unit.IsEnemy)
            {
                // Highlight the movements
                foreach (Tile moveTile in unit.GetMoveTiles())
                {
                    _highlights.Add(Instantiate(HighlightMove, moveTile.GetPosition(), Quaternion.identity, unit.transform));
                }
                // Highlight the attacks
                foreach (Tile attackTile in unit.GetAttackTiles())
                {
                    _highlights.Add(Instantiate(HighlightAttack, attackTile.GetPosition(), Quaternion.identity, unit.transform));
                }
            }
        }
        else
        {
            ClearSelection();
        }
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

    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
}

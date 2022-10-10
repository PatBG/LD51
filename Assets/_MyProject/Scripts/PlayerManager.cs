using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public GameObject PanelDescription;
    public TextMeshProUGUI TextDescription;

    public GameObject PanelEndGame;
    public TextMeshProUGUI TextEndGame;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layermask = 1 << LayerMask.NameToLayer("Tiles");
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, layermask))
            {
                SelectTile(MapManager.GetTileFromPosition(raycastHit.transform.position));
            }
            else
            {
                ClearSelection();
            }
        }
        else if (_selection == SelectionType.Ally || _selection == SelectionType.Enemy)
        {
            // Refresh Selected unit (reselect it)
            Vector3Int tile = _selectionUnit.Tile;
            ClearSelection();
            SelectTile(tile, false);
        }

        // Check endgame
        if (!PanelEndGame.activeInHierarchy)
        { 
            if (Unit.CountUnits(true) == 0)
            {
                // Victory
                int nb = Unit.CountUnits(false);
                TextEndGame.text =
                    "You win!!!\r\n" +
                    "with " + nb + " guard" + (nb > 0 ? "s" : "")+ " left.";
                PanelEndGame.SetActive(true);
            }
            else if (Unit.CountUnits(false) == 0)
            {
                // Defeat
                int nb = Unit.CountUnits(true);
                TextEndGame.text =
                    "You loose!!!\r\n" +
                    "with " + nb + " foe" + (nb > 0 ? "s" : "")+ " left.";
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
        PanelDescription.SetActive(false);
    }

    private void SelectTile(Vector3Int tile, bool isCameraFollow = true)
    {
        //Debug.Log("Select tile: " + tile + "\r\n");
        if (isCameraFollow)
        {
            CameraManager.SetTileTarget(tile);
        }
        if (_selection == SelectionType.None)
        {
            Unit unit = Unit.GetUnit(tile);
            if (unit != null)
            {
                _selectionUnit = unit;
                // Show unit description
                PanelDescription.SetActive(true);
                TextDescription.text = unit.Description();
                if (unit.IsEnemy)
                {
                    _selection = SelectionType.Enemy;
                    // Highlight the unit
                    _highlights.Add(Instantiate(HighlightEnemy, MapManager.GetPositionFromTile(tile), Quaternion.identity, unit.transform));
                }
                else
                {
                    _selection = SelectionType.Ally;

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
        else if (_selection == SelectionType.Enemy)
        {
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
}

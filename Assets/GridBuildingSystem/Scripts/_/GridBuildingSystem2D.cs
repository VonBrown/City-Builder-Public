using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CodeMonkey.Utils;

public class GridBuildingSystem2D : MonoBehaviour {

    public static GridBuildingSystem2D Instance { get; private set; }

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;


    private Grid<GridObject> grid;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList = null;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir dir;

    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 10;

    private void Awake() {
        Instance = this;

        // int gridWidth = 10;
        // int gridHeight = 10;
        // float cellSize = 10f;
        grid = new Grid<GridObject>(gridWidth, gridHeight, cellSize, new Vector3(0, 0, 0), (Grid<GridObject> g, int x, int y) => new GridObject(g, x, y));

        placedObjectTypeSO = null;
    }

    public class GridObject {

        private Grid<GridObject> grid;
        private int x;
        private int y;
        public PlacedObject_Done placedObject;

        public GridObject(Grid<GridObject> grid, int x, int y) {
            this.grid = grid;
            this.x = x;
            this.y = y;
            placedObject = null;
        }

        public override string ToString() {
            return x + ", " + y + "\n" + placedObject;
        }

        public void SetPlacedObject(PlacedObject_Done placedObject) {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, y);
        }

        public void ClearPlacedObject() {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y);
        }

        public PlacedObject_Done GetPlacedObject() {
            return placedObject;
        }

        public bool CanBuild() {
            return placedObject == null;
        }

    }

    private void Update() {
        if (Input.GetMouseButtonDown(0) && placedObjectTypeSO != null) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
            grid.GetXY(mousePosition, out int x, out int z);

            Vector2Int placedObjectOrigin = new Vector2Int(x, z);

            // Test Can Build
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(placedObjectOrigin, dir);
            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList) {
                if (!grid.GetGridObject(gridPosition.x, gridPosition.y).CanBuild()) {
                    canBuild = false;
                    break;
                }
            }

            if (canBuild) {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + new Vector3(rotationOffset.x, rotationOffset.y) * grid.GetCellSize();

                CheckAdjacentBuildings(placedObjectWorldPosition, out List<string> ab_list, out int adjRoads, out int roadCalc,
                out Vector3 buildingNorthPos, out Vector3 buildingEastPos, out Vector3 buildingSouthPos, out Vector3 buildingWestPos);

                PlacedObject_Done placedObject = PlacedObject_Done.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO, placedObjectTypeSO.isRoad);
                placedObject.transform.rotation = Quaternion.Euler(0, 0, -placedObjectTypeSO.GetRotationAngle(dir));

                foreach (Vector2Int gridPosition in gridPositionList) {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }

                OnObjectPlaced?.Invoke(this, EventArgs.Empty);

                //DeselectObjectType();
            } else {
                // Cannot build here
                UtilsClass.CreateWorldTextPopup("Cannot Build Here!", mousePosition);
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            dir = PlacedObjectTypeSO.GetNextDir(dir);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { placedObjectTypeSO = placedObjectTypeSOList[0]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { placedObjectTypeSO = placedObjectTypeSOList[1]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { placedObjectTypeSO = placedObjectTypeSOList[2]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { placedObjectTypeSO = placedObjectTypeSOList[3]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { placedObjectTypeSO = placedObjectTypeSOList[4]; RefreshSelectedObjectType(); }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { placedObjectTypeSO = placedObjectTypeSOList[5]; RefreshSelectedObjectType(); }

        if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectObjectType(); }


        if (Input.GetMouseButtonDown(1)) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
            PlacedObject_Done placedObject = grid.GetGridObject(mousePosition).GetPlacedObject();
            if (placedObject != null) {
                // Demolish
                placedObject.DestroySelf();

                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                foreach (Vector2Int gridPosition in gridPositionList) {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                }
            }
        }
    }

    private void DeselectObjectType() {
        placedObjectTypeSO = null; RefreshSelectedObjectType();
    }

    private void RefreshSelectedObjectType() {
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }


    public Vector2Int GetGridPosition(Vector3 worldPosition) {
        grid.GetXY(worldPosition, out int x, out int z);
        return new Vector2Int(x, z);
    }

    public Vector3 GetMouseWorldSnappedPosition() {
        Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
        grid.GetXY(mousePosition, out int x, out int y);

        if (placedObjectTypeSO != null) {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y) + new Vector3(rotationOffset.x, rotationOffset.y) * grid.GetCellSize();
            return placedObjectWorldPosition;
        } else {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation() {
        if (placedObjectTypeSO != null) {
            return Quaternion.Euler(0, 0, -placedObjectTypeSO.GetRotationAngle(dir));
        } else {
            return Quaternion.identity;
        }
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO() {
        return placedObjectTypeSO;
    }

    public void CheckAdjacentBuildings(Vector3 position, out List<string> ab_list, out int adjRoads, out int roadCalc,
        out Vector3 buildingNorthPos, out Vector3 buildingEastPos, out Vector3 buildingSouthPos, out Vector3 buildingWestPos)
    {

        List<string> AdjacentBuildingsList = new List<string>();

        // Reset values if existing
        AdjacentBuildingsList.Clear();
        int adjacentRoads = 0;
        int roadCalculator = 0;

        foreach (string AdjacentBuilding in AdjacentBuildingsList)
        {

        }

        // North cell
        Vector3 _buildingNorthPos = new Vector3(position.x + 1, position.y, position.z);
        if (_buildingNorthPos.x >= 0 && _buildingNorthPos.x <= gridWidth - 1 && _buildingNorthPos.y >= 0 && _buildingNorthPos.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject((int)position.x, (int)position.y).CanBuild())
            {
                // Calculator to determine road connections & rotations
                if (placedObjectTypeSO.isRoad)
                {
                    roadCalculator = 0;
                    AdjacentBuildingsList.Add("Road");
                }
            }
            else
            {
                AdjacentBuildingsList.Add("None");
            }
        }
        else
        {
            AdjacentBuildingsList.Add("None");
        }


        // East cell
        Vector3 _buildingEastPos = new Vector3(position.x, position.y - 1, position.z);
        if (_buildingEastPos.x >= 0 && _buildingEastPos.x <= gridWidth - 1 && _buildingEastPos.y >= 0 && _buildingEastPos.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject((int)position.x, (int)position.y).CanBuild())
            {
                // Calculator to determine road connections & rotations
                if (placedObjectTypeSO.isRoad)
                {
                    roadCalculator += 1;
                    AdjacentBuildingsList.Add("Road");
                }
            }
            else
            {
                AdjacentBuildingsList.Add("None");
            }
        }
        else
        {
            AdjacentBuildingsList.Add("None");
        }


        // South cell
        Vector3 _buildingSouthPos = new Vector3(position.x - 1, position.y, position.z);
        if (_buildingSouthPos.x >= 0 && _buildingSouthPos.x <= gridWidth - 1 && _buildingSouthPos.y >= 0 && _buildingSouthPos.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject((int)position.x, (int)position.y).CanBuild())
            {
                // Calculator to determine road connections & rotations
                if (placedObjectTypeSO.isRoad)
                {
                    roadCalculator += 2;
                    AdjacentBuildingsList.Add("Road");
                }
            }
            else
            {
                AdjacentBuildingsList.Add("None");
            }
        }
        else
        {
            AdjacentBuildingsList.Add("None");
        }


        // West cell
        Vector3 _buildingWestPos = new Vector3(position.x, position.y + 1, position.z);
        if (_buildingWestPos.x >= 0 && _buildingWestPos.x <= gridWidth - 1 && _buildingWestPos.y >= 0 && _buildingWestPos.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject((int)position.x, (int)position.y).CanBuild())
            {
                // Calculator to determine road connections
                if (placedObjectTypeSO.isRoad)
                {
                    roadCalculator += 3;
                    AdjacentBuildingsList.Add("Road");
                }
            }
            else
            {
                AdjacentBuildingsList.Add("None");
            }
        }
        else
        {
            AdjacentBuildingsList.Add("None");
        }


        // Debug log adjacent buildings
        foreach (var group in AdjacentBuildingsList.GroupBy(i => i))
        {
            Debug.Log(string.Format("Item {0}: {1} times", group.Key, group.Count()));
            if (group.Key == "Road")
            {
                adjacentRoads = group.Count();
            }

        }

        // convert to out variables
        ab_list = AdjacentBuildingsList;
        adjRoads = adjacentRoads;
        roadCalc = roadCalculator;
        buildingNorthPos = _buildingNorthPos;
        buildingEastPos = _buildingEastPos;
        buildingSouthPos = _buildingSouthPos;
        buildingWestPos = _buildingWestPos;

    }

}

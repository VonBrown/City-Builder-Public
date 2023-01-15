using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CodeMonkey.Utils;

public class GridBuildingSystem3D : MonoBehaviour {

    public static GridBuildingSystem3D Instance { get; private set; }

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;


    private GridXZ<GridObject> grid;
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
        grid = new GridXZ<GridObject>(gridWidth, gridHeight, cellSize, new Vector3(0, 0, 0), (GridXZ<GridObject> g, int x, int y) => new GridObject(g, x, y));

        placedObjectTypeSO = null;// placedObjectTypeSOList[0];
    }

    public class GridObject {

        private GridXZ<GridObject> grid;
        private int x;
        private int y;
        public PlacedObject_Done placedObject;

        public GridObject(GridXZ<GridObject> grid, int x, int y) {
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
            Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
            grid.GetXZ(mousePosition, out int x, out int z);

            Vector2Int placedObjectOrigin = new Vector2Int(x, z);
            placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);

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
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

                // Check adjacent cells for buildings and roads
                CheckAdjacentBuildings(rotationOffset, placedObjectOrigin, placedObjectWorldPosition, out List<string> ab_list, out int adjRoads, out int roadCalc,
                out Vector2Int buildingNorthOrigin, out Vector2Int buildingEastOrigin, out Vector2Int buildingSouthOrigin, out Vector2Int buildingWestOrigin);

                // If PlacedObject is a road, choose proper prefab and rotation
                Debug.Log("placedObjectTypeSO.isRoad: "+ placedObjectTypeSO.isRoad);
                if (placedObjectTypeSO.isRoad)
                {
                    AddRoad(adjRoads, roadCalc, ab_list, placedObjectTypeSO, placedObjectWorldPosition, out PlacedObjectTypeSO placedObjectTypeSORoad, out int setRotateRoad);
                    placedObjectTypeSO = placedObjectTypeSORoad;
                    // Debug.Log("placedObjectTypeSORoad: " + placedObjectTypeSORoad);
                    // Debug.Log("placedObjectTypeSO: " + placedObjectTypeSO);
                
                    if (setRotateRoad == 0)
                    {
                        dir = PlacedObjectTypeSO.Dir.Down;
                    }
                    else if(setRotateRoad == 1)
                    {
                        dir = PlacedObjectTypeSO.Dir.Left;
                    }
                    else if (setRotateRoad == 2)
                    {
                        dir = PlacedObjectTypeSO.Dir.Up;
                    }
                    else if (setRotateRoad == 3)
                    {
                        dir = PlacedObjectTypeSO.Dir.Right;
                    }
                
                    rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                    placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
                }

                PlacedObject_Done placedObject = PlacedObject_Done.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO, placedObjectTypeSO.isRoad);

                foreach (Vector2Int gridPosition in gridPositionList) {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }

                // Update adjacent cells if roads
                UpdateRoad(buildingNorthOrigin);
                UpdateRoad(buildingEastOrigin);
                UpdateRoad(buildingSouthOrigin);
                UpdateRoad(buildingWestOrigin);

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
            Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
            if (grid.GetGridObject(mousePosition) != null) {
                // Valid Grid Position
                PlacedObject_Done placedObject = grid.GetGridObject(mousePosition).GetPlacedObject();
                if (placedObject != null) {
                    // Demolish
                    placedObject.DestroySelf();

                    List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                    foreach (Vector2Int gridPosition in gridPositionList) {
                        grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();

                        if (placedObject.isRoad)
                        {
                            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(gridPosition.x, gridPosition.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

                            // Check adjacent cells for buildings and roads
                            CheckAdjacentBuildings(rotationOffset, gridPosition, placedObjectWorldPosition, out List<string> ab_list, out int adjRoads, out int roadCalc,
                            out Vector2Int buildingNorthOrigin, out Vector2Int buildingEastOrigin, out Vector2Int buildingSouthOrigin, out Vector2Int buildingWestOrigin);

                            UpdateRoad(buildingNorthOrigin);
                            UpdateRoad(buildingEastOrigin);
                            UpdateRoad(buildingSouthOrigin);
                            UpdateRoad(buildingWestOrigin);
                        }
                    }
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
        grid.GetXZ(worldPosition, out int x, out int z);
        return new Vector2Int(x, z);
    }

    public Vector3 GetMouseWorldSnappedPosition() {
        Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
        grid.GetXZ(mousePosition, out int x, out int z);

        if (placedObjectTypeSO != null) {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
            return placedObjectWorldPosition;
        } else {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation() {
        if (placedObjectTypeSO != null) {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        } else {
            return Quaternion.identity;
        }
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO() {
        return placedObjectTypeSO;
    }

    public void CheckAdjacentBuildings(Vector2Int rotationOffset, Vector2Int placedObjectOrigin, Vector3 placedObjectWorldPosition, out List<string> ab_list, out int adjRoads, out int roadCalc,
        out Vector2Int buildingNorthOrigin, out Vector2Int buildingEastOrigin, out Vector2Int buildingSouthOrigin, out Vector2Int buildingWestOrigin)
    {
        List<string> AdjacentBuildingsList = new List<string>();

        // Reset values if existing
        AdjacentBuildingsList.Clear();
        int adjacentRoads = 0;
        int roadCalculator = 0;

        // Debug.Log("placedObjectWorldPosition: " + placedObjectWorldPosition);
        // Debug.Log("placedObjectOrigin: " + placedObjectOrigin);

        // North cell
        Vector2Int _buildingNorthOrigin = placedObjectOrigin + new Vector2Int(1,0);

        // Debug.Log("_buildingNorthOrigin: " + _buildingNorthOrigin);
        // Debug.Log("grid.GetGridObject(_buildingNorthOrigin): " + grid.GetGridObject(_buildingNorthOrigin.x, _buildingNorthOrigin.y));

        if (_buildingNorthOrigin.x >= 0 && _buildingNorthOrigin.x <= gridWidth - 1 && _buildingNorthOrigin.y >= 0 && _buildingNorthOrigin.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject(_buildingNorthOrigin.x, _buildingNorthOrigin.y).CanBuild())
            {
                // Calculator to determine road connections & rotations
                
                GridObject gridObjectNorth = grid.GetGridObject(_buildingNorthOrigin.x, _buildingNorthOrigin.y);
                // Debug.Log("gridObjectNorth: " + gridObjectNorth);
                PlacedObject_Done placedObjectNorth = gridObjectNorth.GetPlacedObject();
                // Debug.Log("placedObjectNorth: " + placedObjectNorth);


                if (placedObjectNorth.isRoad)
                {
                    Debug.Log("North isRoad");
                    roadCalculator = 0;
                    AdjacentBuildingsList.Add("Road");
                }
                else
                {
                    Debug.Log("Not a Road");
                    AdjacentBuildingsList.Add("None");
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
        Vector2Int _buildingEastOrigin = placedObjectOrigin + new Vector2Int(0, -1);

        // Debug.Log("_buildingEastOrigin: " + _buildingEastOrigin);
        // Debug.Log("grid.GetGridObject(_buildingEastOrigin): " + grid.GetGridObject(_buildingEastOrigin.x, _buildingEastOrigin.y));

        if (_buildingEastOrigin.x >= 0 && _buildingEastOrigin.x <= gridWidth - 1 && _buildingEastOrigin.y >= 0 && _buildingEastOrigin.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject(_buildingEastOrigin.x, _buildingEastOrigin.y).CanBuild())
            {
                // Calculator to determine road connections & rotations

                GridObject gridObjectEast = grid.GetGridObject(_buildingEastOrigin.x, _buildingEastOrigin.y);
                // Debug.Log("gridObjectEast: " + gridObjectEast);
                PlacedObject_Done placedObjectEast = gridObjectEast.GetPlacedObject();
                // Debug.Log("placedObjectEast: " + placedObjectEast);

                if (placedObjectEast.isRoad)
                {
                    Debug.Log("East isRoad");
                    roadCalculator += 1;
                    AdjacentBuildingsList.Add("Road");
                }
                else
                {
                    Debug.Log("Not a Road");
                    AdjacentBuildingsList.Add("None");
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
        Vector2Int _buildingSouthOrigin = placedObjectOrigin + new Vector2Int(-1, 0);

        // Debug.Log("_buildingSouthOrigin: " + _buildingSouthOrigin);
        // Debug.Log("grid.GetGridObject(_buildingSouthOrigin): " + grid.GetGridObject(_buildingSouthOrigin.x, _buildingSouthOrigin.y));

        if (_buildingSouthOrigin.x >= 0 && _buildingSouthOrigin.x <= gridWidth - 1 && _buildingSouthOrigin.y >= 0 && _buildingSouthOrigin.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject(_buildingSouthOrigin.x, _buildingSouthOrigin.y).CanBuild())
            {
                // Calculator to determine road connections & rotations
                GridObject gridObjectSouth = grid.GetGridObject(_buildingSouthOrigin.x, _buildingSouthOrigin.y);
                // Debug.Log("gridObjectSouth: " + gridObjectSouth);
                PlacedObject_Done placedObjectSouth = gridObjectSouth.GetPlacedObject();
                // Debug.Log("placedObjectSouth: " + placedObjectSouth);

                if (placedObjectSouth.isRoad)
                {
                    Debug.Log("South isRoad");
                    roadCalculator += 2;
                    AdjacentBuildingsList.Add("Road");
                }
                else
                {
                    Debug.Log("Not a Road");
                    AdjacentBuildingsList.Add("None");
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
        Vector2Int _buildingWestOrigin = placedObjectOrigin + new Vector2Int(0, +1);

        // Debug.Log("_buildingWestOrigin: " + _buildingWestOrigin);
        // Debug.Log("grid.GetGridObject(_buildingWestOrigin): " + grid.GetGridObject(_buildingWestOrigin.x, _buildingWestOrigin.y));

        if (_buildingWestOrigin.x >= 0 && _buildingWestOrigin.x <= gridWidth - 1 && _buildingWestOrigin.y >= 0 && _buildingWestOrigin.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject(_buildingWestOrigin.x, _buildingWestOrigin.y).CanBuild())
            {
                // Calculator to determine road connections
                GridObject gridObjectWest = grid.GetGridObject(_buildingWestOrigin.x, _buildingWestOrigin.y);
                // Debug.Log("gridObjectWest: " + gridObjectWest);
                PlacedObject_Done placedObjectWest = gridObjectWest.GetPlacedObject();
                // Debug.Log("placedObjectWest: " + placedObjectWest);

                if (placedObjectWest.isRoad)
                {
                    Debug.Log("West isRoad");
                    roadCalculator += 3;
                    AdjacentBuildingsList.Add("Road");
                }
                else
                {
                    Debug.Log("Not a Road");
                    AdjacentBuildingsList.Add("None");
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
        buildingNorthOrigin = _buildingNorthOrigin;
        buildingEastOrigin = _buildingEastOrigin;
        buildingSouthOrigin = _buildingSouthOrigin;
        buildingWestOrigin = _buildingWestOrigin;

    }

    public void AddRoad(int adjRoads, int roadCalc, List<string> ab_list, PlacedObjectTypeSO placedObjectTypeSO, Vector3 position, out PlacedObjectTypeSO placedObjectTypeSORoad, out int setRotateRoad)
    {
        int rotateRoad = ab_list.IndexOf("Road");
        setRotateRoad = rotateRoad;

        if (adjRoads == 1)
        {
            placedObjectTypeSORoad = placedObjectTypeSOList[0];
        }
        else if (adjRoads == 2)
        {
            // Across, or adjacent? (straight or corner piece)
            // if even, road is across
            if (roadCalc % 2 == 0)
            {
                placedObjectTypeSORoad = placedObjectTypeSOList[0];
            }
            // else if odd, road is corner
            else
            {
                int lastRoadInList = ab_list.LastIndexOf("Road");

                // rotate road to connect two corner roads
                if (ab_list.IndexOf("Road") == 0 && lastRoadInList == 3)
                {
                    rotateRoad = 3;
                }
                else
                {
                    rotateRoad = lastRoadInList - 1;
                    // Debug.Log("rotateRoad: " + rotateRoad);
                }

                placedObjectTypeSORoad = placedObjectTypeSOList[7];
            }
        }
        else if (adjRoads == 3)
        {
            int lastRoadInList = ab_list.LastIndexOf("Road");

            // rotate road to connect two corner roads
            if (ab_list.IndexOf("Road") == 0 && lastRoadInList == 2)
            {
                rotateRoad = 0;
            }
            else if (ab_list.IndexOf("Road") == 0 && lastRoadInList == 3 && roadCalc == 4)
            {
                rotateRoad = 3;
            }
            else if (ab_list.IndexOf("Road") == 0 && lastRoadInList == 3 && roadCalc == 5)
            {
                rotateRoad = 2;
            }
            else
            {
                rotateRoad = lastRoadInList - 2;
            }

            placedObjectTypeSORoad = placedObjectTypeSOList[8];
        }
        else if (adjRoads == 4)
        {
            placedObjectTypeSORoad = placedObjectTypeSOList[9];
        }
        else // no adjacent roads
        {
            placedObjectTypeSORoad = placedObjectTypeSOList[0];
        }

        setRotateRoad = rotateRoad;

    }
    public void UpdateRoad(Vector2Int placedObjectOrigin)
    {
        if (placedObjectOrigin.x >= 0 && placedObjectOrigin.x <= gridWidth - 1 && placedObjectOrigin.y >= 0 && placedObjectOrigin.y <= gridHeight - 1)
        {
            if (!grid.GetGridObject(placedObjectOrigin.x, placedObjectOrigin.y).CanBuild())
            {
                if (placedObjectTypeSO.isRoad)
                {
                    GridObject updateGridObject = grid.GetGridObject(placedObjectOrigin.x, placedObjectOrigin.y);
                    Debug.Log("gridObject: " + updateGridObject);
                    PlacedObject_Done updatePlacedObject = updateGridObject.GetPlacedObject();
                    Debug.Log("placedObject: " + updatePlacedObject);

                    if (updatePlacedObject.isRoad)
                    {
                        PlacedObjectTypeSO updatePlacedObjectTypeSO = updatePlacedObject.GetPlacedObjectTypeSO();
                        Debug.Log("updatePlacedObjectTypeSO: " + updatePlacedObjectTypeSO);

                        Vector2Int rotationOffset = updatePlacedObjectTypeSO.GetRotationOffset(dir);
                        Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

                        CheckAdjacentBuildings(rotationOffset, placedObjectOrigin, placedObjectWorldPosition, out List<string> ab_list, out int adjRoads, out int roadCalc,
                        out Vector2Int buildingNorthOrigin, out Vector2Int buildingEastOrigin, out Vector2Int buildingSouthOrigin, out Vector2Int buildingWestOrigin);

                        // Demolish
                        updatePlacedObject.DestroySelf();

                        List<Vector2Int> gridPositionList = updatePlacedObject.GetGridPositionList();
                        foreach (Vector2Int gridPosition in gridPositionList)
                        {
                            grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                        }

                        AddRoad(adjRoads, roadCalc, ab_list, updatePlacedObjectTypeSO, placedObjectWorldPosition, out PlacedObjectTypeSO placedObjectTypeSORoad, out int setRotateRoad);
                        updatePlacedObjectTypeSO = placedObjectTypeSORoad;
                        if (setRotateRoad == 0)
                        {
                            dir = PlacedObjectTypeSO.Dir.Down;
                        }
                        else if (setRotateRoad == 1)
                        {
                            dir = PlacedObjectTypeSO.Dir.Left;
                        }
                        else if (setRotateRoad == 2)
                        {
                            dir = PlacedObjectTypeSO.Dir.Up;
                        }
                        else if (setRotateRoad == 3)
                        {
                            dir = PlacedObjectTypeSO.Dir.Right;
                        }

                        rotationOffset = updatePlacedObjectTypeSO.GetRotationOffset(dir);
                        placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

                        PlacedObject_Done updatePlacedObject_Done = PlacedObject_Done.Create(placedObjectWorldPosition, placedObjectOrigin, dir, updatePlacedObjectTypeSO, updatePlacedObjectTypeSO.isRoad);

                        foreach (Vector2Int gridPosition in gridPositionList)
                        {
                            grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(updatePlacedObject_Done);
                        }
                    }
                }
            }
        }
    }

}

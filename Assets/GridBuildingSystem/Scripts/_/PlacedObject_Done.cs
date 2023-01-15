using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject_Done : MonoBehaviour {

    public static PlacedObject_Done Create(Vector3 worldPosition, Vector2Int origin, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO, bool isRoad)
    {

        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0));

        PlacedObject_Done placedObject = placedObjectTransform.GetComponent<PlacedObject_Done>();
        placedObject.Setup(placedObjectTypeSO, origin, dir, isRoad);

        return placedObject;
    }




    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private PlacedObjectTypeSO.Dir dir;
    public bool isRoad;

    private void Setup(PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, PlacedObjectTypeSO.Dir dir, bool isRoad) {
        this.placedObjectTypeSO = placedObjectTypeSO;
        this.origin = origin;
        this.dir = dir;
        this.isRoad = isRoad;

    }

    public List<Vector2Int> GetGridPositionList() {
        return placedObjectTypeSO.GetGridPositionList(origin, dir);
    }

    public void DestroySelf() {
        Destroy(gameObject);
    }

    public override string ToString() {
        return placedObjectTypeSO.nameString;
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

}

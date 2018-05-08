
using UnityEngine;

public class WaypointMarkerDragHandle : DragObject
{

    private WaypointMarker _waypointMarker;

    void Awake()
    {
        _waypointMarker = transform.parent.GetComponent<WaypointMarker>();
        _dragReferencePoint = transform.parent.position;
    }

    public override void OnDrag(float difference, Vector3 direction)
    {
        _waypointMarker.UpdateCustomScale(difference, direction);
    }

    public override Vector3 StartDrag()
    {
        return _dragReferencePoint;
    }

    public override void StopDrag()
    {
        _waypointMarker.EndUpdateCustomScale();
    }
}

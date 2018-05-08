public class WaypointMarkerDragHandle : DragObject
{

    private WaypointMarker _waypointMarker;

    void Awake()
    {
        _waypointMarker = transform.parent.GetComponent<WaypointMarker>();
    }

    public override void OnDrag(float difference)
    {
        _waypointMarker.UpdateCustomScale(difference);
    }

    public override void StartDrag()
    {
       
    }

    public override void StopDrag()
    {
        
    }
}

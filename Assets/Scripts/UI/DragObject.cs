using UnityEngine;

public abstract class DragObject : MonoBehaviour
{
    protected Vector3 _dragReferencePoint;

    public abstract void OnDrag(float difference, Vector3 direction);

    public abstract Vector3 StartDrag();

    public abstract void StopDrag();
}

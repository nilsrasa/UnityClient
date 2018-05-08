using UnityEngine;

public abstract class DragObject : MonoBehaviour
{

    public abstract void OnDrag(float difference);

    public abstract void StartDrag();

    public abstract void StopDrag();
}

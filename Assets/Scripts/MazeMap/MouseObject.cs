using UnityEngine;

public class MouseObject : MonoBehaviour
{

    public delegate void MouseHovered();
    public event MouseHovered OnMouseHover;

    public delegate void MouseStayed();
    public event MouseStayed OnMouseStay;

    public delegate void MouseExited();
    public event MouseExited OnMouseExit;

    public delegate void MouseClicked();
    public event MouseClicked OnMouseClick;

    protected bool _isHovered;

    public virtual void Hovered()
    {
        if (_isHovered) Stayed();
        else
        {
            _isHovered = true;
            OnMouseHover?.Invoke();
        }
    }

    public virtual void Stayed()
    {
        OnMouseStay?.Invoke();
    }

    public virtual void Exited()
    {
        if (_isHovered)
            OnMouseExit?.Invoke();
        _isHovered = false;
    }

    public virtual void Clicked()
    {
        OnMouseClick?.Invoke();
    }
}

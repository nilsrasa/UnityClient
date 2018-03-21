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
            if (OnMouseHover != null)
                OnMouseHover();
        }
    }

    public virtual void Stayed()
    {
        if (OnMouseStay != null)
            OnMouseStay();
    }

    public virtual void Exited()
    {
        if (_isHovered)
        {
            if (OnMouseExit != null)
                OnMouseExit();
        }
        _isHovered = false;
    }

    public virtual void Clicked()
    {
        if (OnMouseClick != null)
            OnMouseClick();
    }
}

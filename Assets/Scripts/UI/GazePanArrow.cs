using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GazePanArrow : GazeObject
{
    [SerializeField] private new UnityEvent _onActivateRepeat;
    [SerializeField] private Color _activatedColor;
    [SerializeField] private Image _image;

    private Color _orgColor;

    protected override void Awake()
    {
        base.Awake();
        _orgColor = _image.color;
    }

    protected override void Update()
    {
        base.Update();
        if (IsActivated)
        {
            _image.color = _activatedColor;
            if (_onActivateRepeat != null)
                _onActivateRepeat.Invoke();
        }
        else
        {
            _image.color = _orgColor;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class SorroundPhotoLocation : MouseObject
{
    [SerializeField] private float _scaleFactor = 2;

    public delegate void WasClicked(SorroundPhotoLocation sender);

    public event WasClicked OnClick;

    public int PictureId;
    public List<DateTime> Timestamps;

    private Vector3 _orgScale;

    void Awake()
    {
        _orgScale = transform.localScale;
        Timestamps = new List<DateTime>();
    }

    void Update()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
    }

    public override void Hovered()
    {
        base.Hovered();
        transform.localScale *= _scaleFactor;
    }

    public override void Exited()
    {
        base.Exited();
        transform.localScale = _orgScale;
    }

    public override void Clicked()
    {
        base.Clicked();
        if (OnClick != null)
            OnClick(this);
    }
}
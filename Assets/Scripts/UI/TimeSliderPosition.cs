using System;
using UnityEngine;
using UnityEngine.UI;

public class TimeSliderPosition : MonoBehaviour
{
    [SerializeField] private Color _highlightColor;

    [HideInInspector] public int PictureId;
    [HideInInspector] public DateTime Timestamp;

    private Image _image;
    private RectTransform _rectTransform;
    private Color _orgColor;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _orgColor = _image.color;
    }

    public void SetHighlight(bool isHighlighted)
    {
        _image.color = isHighlighted ? _highlightColor : _orgColor;
    }
}

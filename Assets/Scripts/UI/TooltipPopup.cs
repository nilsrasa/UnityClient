using UnityEngine;

//A tooltip that pops up when responsible GazeObject is hovered
public class TooltipPopup : MonoBehaviour
{
    [SerializeField] private GazeObject _gazeObject;

    private Transform _container;

    void Awake()
    {
        _container = transform.GetChild(0);
    }

    void Start()
    {
        _gazeObject.Hovered += ShowTooltip;
        _gazeObject.Unhovered += HideTooltip;
    }

    private void ShowTooltip(GazeObject button)
    {
        _container.gameObject.SetActive(true);
    }

    private void HideTooltip(GazeObject button)
    {
        _container.gameObject.SetActive(false);
    }
}
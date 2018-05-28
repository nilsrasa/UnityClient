using System.Collections;
using UnityEngine;

public class AnimationDelay : MonoBehaviour
{

    [SerializeField] private float _animationDelay = 0;
    [SerializeField] private string _animationStartTrigger = "Start";

    private Animator _animator;
    private Renderer[] _renderers;

	// Use this for initialization
	void Start ()
	{
	    _renderers = GetComponentsInChildren<Renderer>();
	    _animator = GetComponent<Animator>();
	    StartCoroutine(StartAnimation(_animationDelay));
	    foreach (Renderer meshRenderer in _renderers)
	    {
	        meshRenderer.enabled = false;
	    }
	}

    private IEnumerator StartAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (Renderer meshRenderer in _renderers)
        {
            meshRenderer.enabled = true;
        }
        _animator.SetTrigger(_animationStartTrigger);
    }
}

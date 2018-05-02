using UnityEngine;
using UrdfToUnity.Urdf.Models;

/// <summary>
/// This class holds a UrdfUnity Link model to be attached
/// to the corresponding GameObject. 
/// </summary>
public class ROSLinkModel : MonoBehaviour
{
    public Link link = null;
    public bool hasMesh = false;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorroundPhotoController : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] _photoPlanes;
    private string[] _cameraFolderPaths = new string[]
    {
        "/Cam 0/", "/Cam 1/", "/Cam 2/", "/Cam 3/", "/Cam 4/", "/Cam 5/", "/Cam 6/", "/Cam 7/", "/Cam 8/", "/Cam 9/", "/Cam 10/", "/Cam 11/"
    };

    private const string _cameraFolderPath = "/AbsoluteZero";
    private Dictionary<int, string> _locations;



}

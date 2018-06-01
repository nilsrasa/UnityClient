using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLoader : MonoBehaviour
{
    [SerializeField] private List<ObjectSpawn> _objectsToLoad;

    private List<GameObject> _objectsLoaded;

	void Start ()
	{
	    MazeMapController.Instance.OnFinishedGeneratingCampus += LoadObjects;
	    MazeMapController.Instance.OnStartedGeneratingCampus += ClearObjects;
	}

    private void ClearObjects()
    {
        if (_objectsLoaded != null)
        {
            foreach (GameObject go in _objectsLoaded)
            {
                Destroy(go);
            }
        }
        _objectsLoaded = new List<GameObject>();
    }

    private void LoadObjects(int campusId)
    {
        _objectsLoaded = new List<GameObject>();

        foreach (ObjectSpawn spawn in _objectsToLoad)
        {
            if (spawn.CampusId != campusId) continue;

            GameObject loadedObject = Instantiate(spawn.Prefab, spawn.Position.ToUTM().ToUnity(), Quaternion.Euler(spawn.Rotation), transform);
            _objectsLoaded.Add(loadedObject);
        }

    }

    [Serializable]
    public struct ObjectSpawn
    {
        public int CampusId;
        public GameObject Prefab;
        public GeoPointWGS84 Position;
        public Vector3 Rotation;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UrdfToUnity.IO;
using UrdfToUnity.Parse.Xml;
using UrdfToUnity.Urdf.Models;
using UrdfToUnity.Urdf.Models.Links;

public class RobotUrdfUtility {

    private static string assetFolderPath = "Assets/Materials/Robots";
    private static string assetPathFormat = assetFolderPath + "/{0}/{1}.mat";
    private static string prefabFolderPath = "Assets/Resources";
    private static string prefabResourcesFolderPath = "Prefabs/Robots";
    private static string prefabPathFormat = prefabFolderPath + "/" + prefabResourcesFolderPath + "/{0}.prefab";

    /// <summary>
    /// Handles putting together a robot GameObject.
    /// </summary>
    /// <param name="robotDescription">Contents of URDF robot description from robot or file</param>    
    /// /// <returns>Gameobject representation of robot generated from description</returns>
    public static GameObject GenerateRobotGameObjectFromDescription(string robotDescription)
    {
        XmlDocument xmlDoc = new XmlDocument();
        RobotParser parser = new RobotParser();

        xmlDoc.Load(XmlReader.Create(new StringReader(robotDescription)));
        Robot robot = parser.Parse(xmlDoc.DocumentElement);

        if (robot != null)
            return GenerateRobotGameObject(robot);
        else return null;
    }

    /// <summary>
    /// Handles putting together a robot GameObject or loads if already created
    /// </summary>
    /// <param name="robot">Robot XML description to generate robot with</param>
    /// <returns>Gameobject representation of robot generated from description</returns>
    /// Code from https://github.com/MangoMangoDevelopment/neptune
    public static GameObject GenerateRobotGameObject(Robot robot) {

        Directory.CreateDirectory(assetFolderPath);
        Directory.CreateDirectory(prefabFolderPath);
        string roboName = robot.Name;
        Debug.Log(string.Format(prefabResourcesFolderPath + "/{0}.prefab", roboName));
        GameObject robotGo = Resources.Load<GameObject>(string.Format(prefabResourcesFolderPath + "/{0}", roboName));
        if (robotGo != null) {
            return GameObject.Instantiate(robotGo, Vector3.zero, Quaternion.identity);
        }

        GameObject parent = new GameObject(robot.Name);
        ROSRobotModel rosRobotModel = parent.AddComponent<ROSRobotModel>();
        rosRobotModel.robot = robot;

        Robot rob2 = rosRobotModel.robot;
        Debug.Log(rob2.Name);
        Dictionary<string, GameObject> linkAsGos = new Dictionary<string, GameObject>();

        // creating a list of gameobjects
        foreach (KeyValuePair<string, Link> link in robot.Links) {
            GameObject linkGo = new GameObject(link.Key);

            ROSLinkModel linkModel = linkGo.AddComponent<ROSLinkModel>();
            linkModel.link = link.Value;
            linkModel.name = link.Key;

            if (link.Value.Visual.Count > 0) {
                GameObject linkVisualGo = null;
                Vector3 linkScale = new Vector3();
                Vector3 linkPosition = new Vector3();
                Vector3 linkRotation = new Vector3();
                float radiusScale = 0.0f;

                foreach (Visual obj in link.Value.Visual) {
                    if ((Geometry.Shapes.Mesh == obj.Geometry.Shape) && (obj.Geometry.Mesh != null)) {
                        linkModel.hasMesh = true;
                        string fileName = FileManagerImpl.GetFileName(obj.Geometry.Mesh.FileName, false);
                        string[] guids = AssetDatabase.FindAssets(string.Format("{0} t:GameObject", fileName));
                        foreach (string guid in guids) {
                            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                            if (asset.name == fileName) {
                                linkVisualGo = GameObject.Instantiate<GameObject>((GameObject)asset);
                                break;
                            }
                        }
                        if (obj.Geometry.Mesh.Size != null) {
                            linkScale = new Vector3((float)obj.Geometry.Mesh.Size.Length, (float)obj.Geometry.Mesh.Size.Height, (float)obj.Geometry.Mesh.Size.Width);
                        }
                        else {
                            linkScale = new Vector3(1f, 1f, 1f);
                        }
                        linkPosition = new Vector3((float)obj.Origin.Xyz.X, (float)obj.Origin.Xyz.Y, (float)obj.Origin.Xyz.Z);
                        linkRotation = new Vector3(Mathf.Rad2Deg * (float)obj.Origin.Rpy.R, Mathf.Rad2Deg * (float)obj.Origin.Rpy.P, Mathf.Rad2Deg * (float)obj.Origin.Rpy.Y);
                    }
                    else {
                        PrimitiveType type = PrimitiveType.Cube;

                        switch (obj.Geometry.Shape) {
                            case Geometry.Shapes.Cylinder:
                                type = PrimitiveType.Cylinder;
                                radiusScale = (float)obj.Geometry.Cylinder.Radius * 2;
                                linkScale = new Vector3(radiusScale, (float)obj.Geometry.Cylinder.Length / 2, radiusScale);
                                break;
                            case Geometry.Shapes.Sphere:
                                type = PrimitiveType.Sphere;
                                radiusScale = (float)obj.Geometry.Sphere.Radius * 2;
                                linkScale = new Vector3(radiusScale, radiusScale, radiusScale);
                                break;
                            case Geometry.Shapes.Box:
                                type = PrimitiveType.Cube;
                                linkScale = new Vector3((float)obj.Geometry.Box.Size.Length, (float)obj.Geometry.Box.Size.Height, (float)obj.Geometry.Box.Size.Width);
                                break;
                        }
                        linkVisualGo = GameObject.CreatePrimitive(type);
                        linkPosition = new Vector3((float)obj.Origin.Xyz.X, (float)obj.Origin.Xyz.Z, (float)obj.Origin.Xyz.Y);
                        linkRotation = new Vector3(Mathf.Rad2Deg * (float)obj.Origin.Rpy.R, Mathf.Rad2Deg * (float)obj.Origin.Rpy.Y, Mathf.Rad2Deg * (float)obj.Origin.Rpy.P);
                    }

                    if (linkVisualGo != null) {
                        linkVisualGo.transform.localEulerAngles = linkRotation;
                        linkVisualGo.transform.localScale = linkScale;
                        linkVisualGo.transform.localPosition = linkPosition;

                        if (obj.Material != null && obj.Material.Color != null) {
                            Material mat = new Material(Shader.Find("Standard"));
                            mat.color = new Color((float)obj.Material.Color.Rgb.R / 255,
                                (float)obj.Material.Color.Rgb.G / 255,
                                (float)obj.Material.Color.Rgb.B / 255);
                            if (!Directory.Exists(assetFolderPath + "/" + parent.name)) {
                                AssetDatabase.CreateFolder(assetFolderPath, parent.name);
                            }
                            AssetDatabase.CreateAsset(mat, string.Format(assetPathFormat, parent.name, link.Key));
                            if (linkVisualGo.GetComponentInChildren<MeshRenderer>() != null)
                                linkVisualGo.GetComponentInChildren<MeshRenderer>().sharedMaterial = mat;
                        }
                        linkVisualGo.transform.SetParent(linkGo.transform);
                    }
                }

            }
            linkGo.transform.SetParent(parent.transform);
            linkAsGos.Add(link.Key, linkGo);
        }

        foreach (KeyValuePair<string, UrdfToUnity.Urdf.Models.Joint> joint in robot.Joints) {
            GameObject child = linkAsGos[joint.Value.Child.Name];
            child.transform.SetParent(linkAsGos[joint.Value.Parent.Name].transform);
            // Very strange, imported object will have the proper associated XYZ coordinates where as
            // a generated primitive types have the YZ coordinates are swapped.
            if (child.GetComponent<ROSLinkModel>().hasMesh) {
                child.transform.localPosition = new Vector3((float)joint.Value.Origin.Xyz.X, (float)joint.Value.Origin.Xyz.Y, (float)joint.Value.Origin.Xyz.Z);
            }
            else {
                // URDF considers going up as the Z-axis where as unity consider going up is Y-axis
                child.transform.localPosition = new Vector3((float)joint.Value.Origin.Xyz.X, (float)joint.Value.Origin.Xyz.Z, (float)joint.Value.Origin.Xyz.Y);
            }

            child.transform.localEulerAngles = new Vector3(Mathf.Rad2Deg * (float)joint.Value.Origin.Rpy.R, Mathf.Rad2Deg * (float)joint.Value.Origin.Rpy.Y, Mathf.Rad2Deg * (float)joint.Value.Origin.Rpy.P);
        }

        MeshRenderer[] children = parent.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer child in children) {
            if (child.transform == parent.transform)
                continue;
            if (child.gameObject.GetComponent<Renderer>()) {
                child.gameObject.AddComponent<MeshCollider>();
                MeshCollider mc = child.gameObject.GetComponent<MeshCollider>();
                mc.sharedMesh = child.gameObject.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        PrefabUtility.CreatePrefab(string.Format(prefabPathFormat, roboName), parent);

        return parent;
    }
}

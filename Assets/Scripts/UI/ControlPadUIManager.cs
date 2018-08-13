using System.Collections;
using System.Collections.Generic;
using ROSBridgeLib.std_msgs;
using UnityEngine;



//Sizes should work dynamically but the position needs to be hardcoded unfortunately, no time to fix this now 
//TODO : Try to make a dynamic dimension system with all the rects
public class ControlPadUIManager : MonoBehaviour
{


    [SerializeField]
    private SpriteRenderer BordersSprite;
    [SerializeField]
    private SpriteRenderer DeadZoneSprite;
    [SerializeField]
    private SpriteRenderer ROLeftSprite;
    [SerializeField]
    private SpriteRenderer RORightSprite;


    private float BorderWidth;
    private float BorderHeight;

    
    // Use this for initialization
    void Start ()
    {
        BorderWidth = 6.1f;//BordersSprite.size.x;
        BorderHeight = 3.4f; //BordersSprite.size.y;
       // Debug.Log(BorderWidth +"  "+ BorderHeight);
        //DeadZone dimensions
        float zoneWidth = RobotInterface.Instance.RightDeadZoneLimit - RobotInterface.Instance.LeftDeadZoneLimit;
        float zoneHeight = RobotInterface.Instance.UpperDeadZoneLimit - RobotInterface.Instance.LowerDeadZoneLimit;
        float zoneCenterX = (RobotInterface.Instance.LeftDeadZoneLimit + RobotInterface.Instance.RightDeadZoneLimit) / 2.0f;
        float zoneCenterY = (RobotInterface.Instance.UpperDeadZoneLimit + RobotInterface.Instance.LowerDeadZoneLimit) / 2.0f;

        // RO Left
        float ROLWidth = (2 - zoneWidth) / 2;
        float ROLHeight = zoneHeight;
        float ROLCenterX = (-1 + RobotInterface.Instance.LeftDeadZoneLimit) / 2;
        float ROLCenterY = zoneCenterY;

        // RO Right
        float RORWidth = (2 - zoneWidth) / 2;
        float RORHeight = zoneHeight;
        float RORCenterX = (1 + RobotInterface.Instance.RightDeadZoneLimit) / 2;
        float RORCenterY = zoneCenterY;

        //Set UI elements size
        DeadZoneSprite.size =new Vector2(CalculateActualSize(BorderWidth,zoneWidth,2), CalculateActualSize(BorderHeight, zoneHeight, 2)); 
        
        ROLeftSprite.size = new Vector2(CalculateActualSize(BorderWidth,ROLWidth,2), CalculateActualSize(BorderHeight, ROLHeight, 2));
        RORightSprite.size = new Vector2(CalculateActualSize(BorderWidth, RORWidth, 2), CalculateActualSize(BorderHeight, RORHeight, 2));

    }


    private float CalculateActualSize(float sprDimension, float elemNormDimension, float NormalizedDimension)
    {


        return (sprDimension * elemNormDimension) / NormalizedDimension;
    }


}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigManager
{
    private static readonly string ConfigPath = Application.streamingAssetsPath + "/Config/";
    private static readonly string ConfigName = "Config.json";
    private static readonly string UserConfigName = "UserConfig.json";

    public static ConfigFile ConfigFile { get; private set; }

    static ConfigManager()
    {
        if (!File.Exists(ConfigPath + ConfigName))
        {
            Debug.LogError("Config file missing - Exiting Application");
            Application.Quit();
            return;
        }

        if (!File.Exists(ConfigPath + UserConfigName))
        {
            File.CreateText(ConfigPath + UserConfigName);
        }
        ReadConfigFile();
    }

    private static void ReadConfigFile()
    {
        string json = File.ReadAllText(ConfigPath + ConfigName);
        ConfigFile = JsonUtility.FromJson<ConfigFile>(json);

        string userFileJson = File.ReadAllText(ConfigPath + UserConfigName);
        ConfigFile userConfigFile = JsonUtility.FromJson<ConfigFile>(userFileJson);
        ConfigFile def = new ConfigFile();

        if (def.WaypointDistanceThreshold != userConfigFile.WaypointDistanceThreshold) ConfigFile.WaypointDistanceThreshold = userConfigFile.WaypointDistanceThreshold;
        if (def.MaxLinearSpeed != userConfigFile.MaxLinearSpeed) ConfigFile.MaxLinearSpeed = userConfigFile.MaxLinearSpeed;
        if (def.MaxAngularSpeed != userConfigFile.MaxAngularSpeed) ConfigFile.MaxAngularSpeed = userConfigFile.MaxAngularSpeed;
        if (def.ControlParameterRho != userConfigFile.ControlParameterRho) ConfigFile.ControlParameterRho = userConfigFile.ControlParameterRho;
        if (def.ControlParameterPitch != userConfigFile.ControlParameterPitch) ConfigFile.ControlParameterPitch = userConfigFile.ControlParameterPitch;
        if (def.ControlParameterRoll != userConfigFile.ControlParameterRoll) ConfigFile.ControlParameterRoll = userConfigFile.ControlParameterRoll;
        if (def.ControlParameterYaw != userConfigFile.ControlParameterYaw) ConfigFile.ControlParameterYaw = userConfigFile.ControlParameterYaw;
        if (def.FloorHeightAboveGround != userConfigFile.FloorHeightAboveGround) ConfigFile.FloorHeightAboveGround = userConfigFile.FloorHeightAboveGround;
        if (def.FloorLineWidth != userConfigFile.FloorLineWidth) ConfigFile.FloorLineWidth = userConfigFile.FloorLineWidth;
        if (def.UtmZone != userConfigFile.UtmZone) ConfigFile.UtmZone = userConfigFile.UtmZone;
        if (def.IsUtmNorth != userConfigFile.IsUtmNorth) ConfigFile.IsUtmNorth = userConfigFile.IsUtmNorth;
        if (def.ZeroFiducial != userConfigFile.ZeroFiducial) ConfigFile.ZeroFiducial = userConfigFile.ZeroFiducial;
        if (def.Routes != userConfigFile.Routes) ConfigFile.Routes = userConfigFile.Routes;
    }

    private static void SaveUserConfig()
    {
        File.WriteAllText(ConfigPath + UserConfigName, JsonUtility.ToJson(ConfigFile));
    }

    public static void SaveRoute(string name, List<GeoPointWGS84> route)
    {
        ConfigFile.WaypointRoute wpRoute = new ConfigFile.WaypointRoute
        {
            Name = name,
            Points = route
        };

        bool saved = false;
        for (int i = 0; i < ConfigFile.Routes.Count; i++)
        {
            if (ConfigFile.Routes[i].Name == name)
            {
                ConfigFile.Routes[i] = wpRoute;
                saved = true;
                break;
            }
        }
        if (!saved)
            ConfigFile.Routes.Add(wpRoute);

        SaveUserConfig();
    }
}

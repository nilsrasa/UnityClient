

# Unity Client
## Features
* Management of ROS controlled robots.
* Simulation of virtual robots and sensors.
* Uses Mazemap data to generate 3D floorplans for waypoint navigation and simulation.
* Can use Virtual Reality for remote control of telerobots.

## Prerequisites
Unity version 2017.x

## Running Unity
For tutorials and documentation on installing and running Unity see [Unity Manual](https://docs.unity3d.com/Manual/UnityBasics.html)

## Project contents
The Unity projects contain two projects regarding ROS controlled robots, one being a client for a remote presence robots, one being a client and/or simulator.
Unity is scene based, which means each project is placed in a separate scene.
* **MazeMapModel:** Client and simulator using mazemap for generating floor plan of robot's environment
* **Telerobot_ThetaS_Normal:** Direct control of robot through VR interface using Theta S 360 camera for navigation. (Requires camera)
* **Telerobot_ThetaS_Limited:** Same as above but with a limited interface for use with only eyes. (Either on a screen or with a VR HMD)

### Guang Tao experiments July 2018

The goal was to test the differences between two different input methods by the user, direct gazecontrol and joystick.
For both types the user was connected to the Virtual environment through the FOVE  headset and was asked to drive a physical robot through different sets of mazes in the physical world,
answering pop up questions in the process. The environment could be perceived through direct feedback (image and sound) from the Ricoh Theta 360 camera attached to the robot. 

In Unity, the scene that contains this specific simulation is the "Telerobot_ThetaS_QTest" located in the scenes folder of the project directory. 

The simulation assumes that setup for to the physical robot and the 360 camera has been established beforehand, to enable the Unity interface to connect to both and receive/send information correctly.

The test conductor may change the input method for the test through a drop down menu, located in the CockpitStructure gameobject and on the Stream Controller script component attached to it.
The name of the menu in the inspector is  called "selected control type".

The QueryManager gameobject (and script) contains all the settings for the pop up question interface , which the test conductor may change according to their needs.
It also contains settings for parameterising the user trial (Test user ID , maze ID etc) which will be serialised to a JSON file once the experiment is done.

## Builds
A list of available builds can be found [here](https://drive.google.com/drive/folders/1ta0PJkqub2rFYe6Vo0hOmulnSYLumLxf?usp=sharing).
This google drive folder contains the following builds:

 - **MazemapRobotInterface:** Mazemap interface where you can give robot movement commands through waypoints and use VR telepresence robots. Possibilities of using virtual robots as well as physical robots.
 - **VRTelerobot_Head/_Gaze:** Remotely control a Telepresence robot through a different interface with either head or gaze control. (Requires [FOVE VR](https://www.getfove.com/))

### User manual for builds:

#### Common
 - Download and extract build into a folder. (Build contains .exe file, UnityPlayer.dll and a Data folder)
 - Setup ROS Bridge through Docker-ROS. (See [Docker-ROS Readme](https://github.com/DTU-R3/Docker-ROS))
- Add hostname of both ROS Bridge and Unity client to their respective host files.

#### VRTelerobot
- Setup config file in `Data folder/StreamingAssets/Config/Telerobot_ThetaS.json` (The data folder is created when building and is named `#buildname#_Data`)
- Make sure FOVE VR HMD is connected and set up.
- Run build
- Press 'C' on the keyboard to center head

#### MazemapRobotInterface
- Setup config file in `Data folder/StreamingAssets/Config/` (See [Config File Section](#robot-config))
- Load campus from MazeMap by inputting campus id and pressing the Generate Campus button. (Easiest way to find campus id is by going to [use.mazemap.com](https://use.mazemap.com), selecting the wanted campus, and looking for the "campusid=" variable)
- Open Robot List and connect to the robot. 
- Select robot from dropdown list.
- Click on the model to set waypoints and use commands to move robot.

## Charging Ricoh Theta S camera before use

The camera should be charged before use. When charging, the switch of the camera will be solid red light as shown in the picture.
![Charge camera](/Screenshots/charge.jpg "Charge camera")

## Seting up Ricoh Theta S camera

The “DevicePath” is needed to be added to the system registry in order to make Unity work with Theta S blending software.
* Press Win + R and type in 'regedit'.
* Find registry key that is located under '\HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}\Instance\' (note it’s SOFTWARE\Classes\Wow6432Node\CLSID... in 64-bit OS for 32-bit app space), where every subkey corresponds to a registered device.
* Adding new property with the name DevicePath, the value could be random. Note that the value will be destroyed with re-registration of the device filter, and will have to be created once again.

![Add registry key](/Screenshots/registry.PNG "Add registry key")

## Development

### Robots
A robot representation consists of these elements:

* A robot control script that  Which contains:
	* Robot behaviour
	* ROS nodes and modules necessary for robot
	* Robot modules attached to robot
* A prefab that contains the 3D model and the scripts.
* A robot config file with the same name as the prefab, that contains information such as hostname and port for connecting to the robot.  

A robot can only be connected to when it has all these 3 elements. Unity checks if there's a config file and prefab with the same name, and then tries to initialise the robot control script attached to the gameobject when connected.
The prefab and robot control scripts can only be made in editor, so before a build is made, but the robot config files can still be changed after a build is made.

### Robot Control Script
The robot control script defines the robot behaviour as the name implies. This script is attached to the robot gameobject and is responsible for subscribing/advertising to the correct topics. In the current version, all robots have the same basic behaviours, such as waypoint navigation.  
The script extends a `ROSController.cs` (See `ArlobotROSController.cs` or `VirtualRobot.cs`).  

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RosControllerFileLocation.PNG?raw=true)

### Robot prefab
The prefab is the gameobject instantiated by Unity when you connect to a robot. This gameobject should have the robot control script attached, and can also contain a 3D representation of the robot.  

![alt text](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Screenshots/ROSPrefabLocation.PNG?raw=true)


#### Robot Models
Robot models can be created in external 3D editing software and imported into Unity (see [Unity documentation](https://docs.unity3d.com/Manual/HOWTO-importObject.html)), but we also support creating 3D models semi-dynamically from [URDF files](https://github.com/DTU-R3/URDF-ROS).  
This can however only be done in the Unity Editor - not in builds.  
To generate models from URDF, put any URDF files into `Assets/StreamingAssets/URDF` and run the scene `URDF_Model_Generation`.
The project comes with a few models already created as prefabs, that can simply be dragged into the robot prefab (make sure it's centered and facing forward).  
Models can be found in `Assets/Prefabs/RobotModels`.

### Robot config

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RobotFileLocation.PNG?raw=true)
Robot config files consists of 3 important fields that needs to be correct to start the robot:
"Campuses", "RosMasterUri", and "RosMasterPort".
**Campuses:** An int array of the mazemap campus Ids where the robot is available. Make sure your campus id is included here.
**RosBridgeUri:** ROS Bridge Uri (IP or Hostname) that was added to the host file. (Example: "Raspi-ROS-02")
**RosBridgePort:** Port to ROS bridge.
The config files can be found in `Assets/StreamingAssets/Config(/Robots)` before the project is built, and `Data folder/StreamingAssets/Config(/Robots)` after the project is built.

### User Config

Two general config files exist that control the MazeMap integration, but that will have a big impact on navigation, as this also affects things such as coordinate transformation.  
Config location: #buildname#_Data/StreamingAssets/Config  
Two files exist; UserConfig and Config. **Only ever edit the 'UserConfig' file.**  
The UserConfig file contains parameters for generating campus 3D models, position of the positional reference Fiducial, if they are used, and any saved routes there might be.  
The most important fields are:  
 - **UtmZone:**  The UTM coordinate system divides the world into zones. Transformation between coordinate systems is only accurate if the UtmZone is. To find out which zone you reside in, look [here](https://mangomap.com/robertyoung/maps/69585/what-utm-zone-am-i-in-#).  
 - **IsUtmNorth:** Each UTM zone has a north and south equivalent. Please look at above link to find out if you are in a north or south zone. (Look at the "Hemisphere" value)  

### Communication
ROS functions by having nodes that are either subscribing or advitising to topics. Communication to the ROS Master functions through a ROSBridge. ([ROSBridgeWebSocketConnection.cs](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/ROSBridgeLib/ROSBridgeWebSocketConnection.cs))
Communication on the bridge functions through ROSBridgeMessages.

![alt text](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Screenshots/ROSMessageFileLocation.PNG?raw=true)

To create new message types, simply create new classes and follow the structure of the already existing ones. The most important functions to get right is `ToYAMLString()`, as this needs to be parsed correctly on the bridge-side, and `GetMessageType()`, as the name of the message type needs to be identical on the bridge-side.

#### Nodes
Subscribers and Publishers uses the ROSBridge connection to transmit or receive ROSMessages through a websocket connection.
A [generic publisher](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/Publishers/ROSGenericPublisher.cs) and [subscriber](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/Subscribers/ROSGenericSubscriber.cs) exists, that can be used for all messages, but I have created wrappers for a few message types. (Example: [ROSLocomotionWaypoint](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/ROSLocomotionWaypoint.cs))

## License
DTU-R3/VRClient is licensed under the **BSD 3-clause "New" or "Revised"** License - see the [LICENSE.md](LICENSE.ds) file for details

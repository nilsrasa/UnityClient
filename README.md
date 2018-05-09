

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
- Setup config file in Data folder/StreamingAssets/Config/Telerobot_ThetaS.json
- Make sure FOVE VR HMD is connected and set up.
- Run build
- Press 'C' on the keyboard to center head

#### MazemapRobotInterface
- Setup config file in Data folder/StreamingAssets/Config/ (See [Config File Section](#robot-config))
- Load campus from MazeMap by inputting campus id and pressing the Generate Campus button. (Easiest way to find campus id is by going to [use.mazemap.com](https://use.mazemap.com), selecting the wanted campus, and looking for the "campusid=" variable)
- Open Robot List and connect to the robot. 
- Select robot from dropdown list.
- Click on the model to set waypoints and use commands to move robot.

## Development

### Robots
A robot representation consists of these elements:

* A robot control script that extends a `ROSController` (See `ArlobotROSController.cs` or `VirtualRobot.cs`) Which contains:
	* Robot behaviour
	* ROS nodes and modules necessary for robot
	* Robot modules attached to robot

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RosControllerFileLocation.PNG?raw=true)
* A prefab that contains the 3D model and the scripts.

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RobotPrefabFileLocation.PNG?raw=true)
* A robot config file with the same name as the prefab, that contains information such as hostname and port for connecting to the robot.

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RobotFileLocation.PNG?raw=true)

The prefab and robot control scripts can only be made in editor, so before a build is made, but the robot config files can still be changed after a build is made.
They will be located in: #buildname#_Data/StreamingAssets/Config/Robots.

### Robot config
Robot config files consists of 3 important fields that needs to be correct to start the robot:
"Campuses", "RosMasterUri", and "RosMasterPort".
**Campuses:** An int array of the mazemap campus Ids where the robot is available. Make sure your campus id is included here.
**RosBridgeUri:** ROS Bridge Uri (IP or Hostname) that was added to the host file. (Example: "Raspi-ROS-02")
**RosBridgePort:** Port to ROS bridge.

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

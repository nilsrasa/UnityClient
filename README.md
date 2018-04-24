
# Unity Client
### Features
* Management of ROS controlled robots.
* Simulation of virtual robots and sensors.
* Uses Mazemap data to generate 3D floorplans for waypoint navigation and simulation.
* Can use Virtual Reality for remote control of telerobots.

### Prerequisites
Unity version 2017.x

### Running Unity
For tutorials and documentation on installing and running Unity see [Unity Manual](https://docs.unity3d.com/Manual/UnityBasics.html)

### Project contents
The Unity projects contain two projects regarding ROS controlled robots, one being a client for a remote presence robots, one being a client and/or simulator.
Unity is scene based, which means each project is placed in a separate scene.
* MazeMapModel: Client and simulator using mazemap for generating floor plan of robot's environment
* Telerobot_ThetaS_Normal: Direct control of robot through VR interface using Theta S 360 camera for navigation. (Requires camera)
* Telerobot_ThetaS_Limited: Same as above but with a limited interface for use with only eyes. (Either on a screen or with a VR HMD)

### Setup
* Setup ROS Bridge (See [Docker-ROS/r3-ws-bridge](https://github.com/DTU-R3/Docker-ROS/tree/master/r3-ws-bridge))
* Add hostname of both ROS Bridge and Unity client to their respective host files
* Create prefab of robot configuration (3D model and necessary scripts (Look below))
* Create config file with same name (Look below) and set the right URI/Port to connect to the ROS Bridge.
* Open "Scenes/MazeMap.scene" and click Play in Unity

### Development

#### Robots
A robot consists of X elements. 

* A robot control script that extends a `ROSController` (See `ArlobotROSController.cs` or `VirtualRobot.cs`) Which contains:
	* Robot behaviour
	* ROS nodes and modules necessary for robot

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RosControllerFileLocation.PNG?raw=true)
* A prefab that contains the 3D model and the scripts

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RobotPrefabFileLocation.PNG?raw=true)
* A config file with the same name as the prefab, that contains information such as hostname and port for connecting to the robot.

![alt text](https://github.com/DTU-R3/UnityClient/blob/master/Screenshots/RobotFileLocation.PNG?raw=true)

Config files consists of 3 important fields that needs to be correct to start the robot:
"Campuses", "RosMasterUri", and "RosMasterPort".
**Campuses:** An int array of the mazemap campus Ids where the robot is available.
**RosBridgeUri:** ROS Bridge Uri (IP or Hostname) that was added to the host file. (Example: "Raspi-ROS-02")
**RosBridgePort:** Port to ROS bridge.
#### Communication
ROS functions by having nodes that are either subscribing or advitising to topics. Communication to the ROS Master functions through a ROSBridge. ([ROSBridgeWebSocketConnection.cs](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/ROSBridgeLib/ROSBridgeWebSocketConnection.cs))
Communication on the bridge functions through ROSBridgeMessages.

![alt text](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Screenshots/ROSMessageFileLocation.PNG?raw=true)

To create new message types, simply create new classes and follow the structure of the already existing ones. The most important functions to get right is `ToYAMLString()`, as this needs to be parsed correctly on the bridge-side, and `GetMessageType()`, as the name of the message type needs to be identical on the bridge-side.

##### Nodes
Subscribers and Publishers uses the ROSBridge connection to transmit or receive ROSMessages through a websocket connection.
A [generic publisher](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/Publishers/ROSGenericPublisher.cs) and [subscriber](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/Subscribers/ROSGenericSubscriber.cs) exists, that can be used for all messages, but I have created wrappers for a few message types. (Example: [ROSLocomotionWaypoint](https://github.com/DTU-R3/UnityClient/blob/rosbridge/Assets/Scripts/ROS/Nodes/ROSLocomotionWaypoint.cs))

### License
DTU-R3/VRClient is licensed under the **BSD 3-clause "New" or "Revised"** License - see the [LICENSE.md](LICENSE.ds) file for details

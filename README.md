
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
* ROS Master and Unity client needs to be on same network
* Add hostname of both ROS Master and Unity client to their respective host files
* Open "Scenes/MazeMap.scene" and click Play in Unity

### Development
A robot consists of X elements. 

* A robot control script that extends a `ROSController` (See `ArlobotROSController.cs` or `VirtualRobot.cs`) Which contains:
	* Robot behaviour
	* ROS nodes and modules necessary for robot
* A prefab that contains the 3D model and the scripts
* A config file with the same name as the prefab, that contains information such as hostname and port for connecting to the robot.


### License
DTU-R3/VRClient is licensed under the **BSD 3-clause "New" or "Revised"** License - see the [LICENSE.md](LICENSE.ds) file for details

# VRClient
VR Unity client for ROS controlled robots, also functions as 
Allows user to control a ROS based robot from virtual reality with head and eye tracking.

### Prerequisites
Unity version 2017.x

### Running Unity
For tutorials and documentation on installing and running Unity see [Unity Manual](https://docs.unity3d.com/Manual/UnityBasics.html)

### Project contents
The Unity projects contain two projects regarding ROS controlled robots, one being a client for a remote presence robots, one being a client and/or simulator.
Unity is scene based, which means each project is placed in a separate scene.
* MazeMapModel: Client and simulator using mazemap for generating floor plan of robot's environment
* RobotControlTest: To test direct locomotion control with a ROS controlled robot.
* Telerobot_ThetaS_Normal: Direct control of robot through VR interface using Theta S 360 camera for navigation.
* Telerobot_ThetaS_Limited: Same as above but with a limited interface for use with only eyes. (Either on a screen or with a VR HMD)

### Setup Guide
* Connect Unity and ROS machine through LAN/Wifi
* Start up ROS on other machine and note IP and port of master (usually :11311)
* Add IP and Hostname of ROS machine to hosts file on Unity PC
* Run Unity software
* Open Unity ROSSimulator or VideoProjectionTheta_USB scene
* Select "CockpitStructure" gameobject in scene hierarchy and change "ROS_MASTER_URI" value in Robot Interface script to match IP and port of ROS server
* Change "Controlled Robot Type" in same script to "Arlobot"
* Click play


### License
DTU-R3/VRClient is licensed under the **BSD 3-clause "New" or "Revised"** License - see the [LICENSE.md](LICENSE.ds) file for details

## Acknowledgements
* [ROS.NET](https://github.com/uml-robotics/ROS.NET)
* [ProjNet4GeoAPI](https://github.com/NetTopologySuite/ProjNet4GeoAPI)

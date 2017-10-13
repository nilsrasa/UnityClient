# VRClient
VR Unity client for ROS controlled remote presence robots. 
Allows user to control a ROS based robot from virtual reality with head and eye tracking.

### Prerequisites
Unity version 2017.x

### Setup Guide
* Connect Unity and ROS machine through LAN/Wifi
* Start up ROS on other machine and note IP and port of master (usually :11311)
* Add IP of ROS machine to enviromental variables with name "ROS_MASTER_URI" on Unity PC
* Add IP and Hostname of ROS machine to hosts file on Unity PC
* Run Unity software

### Running Unity
For tutorials and documentation on Unity, see [Unity Manual](https://docs.unity3d.com/Manual/UnityBasics.html)

### License
DTU-R3/VRClient is licensed under the **BSD 3-clause "New" or "Revised"** License - see the [LICENSE.md](LICENSE.ds) file for details

## Acknowledgements
* [ROS.NET](https://github.com/uml-robotics/ROS.NET)
* [ROS.NET_Unity](https://github.com/uml-robotics/ROS.NET_Unity)

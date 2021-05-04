# Distributed Execution of *OPTANO Algorithm Tuner*
To lessen the time you need to tune your algorithm, or to improve the tuning, *OPTANO Algorithm Tuner* can be executed across multiple computing nodes.

One of these nodes needs to be defined as being the *Master* that coordinates the tuning, manages results and acts as the connection point for the other nodes, the *Workers*. *Workers* may be added or removed dynamically during the tuning. They are additional nodes to execute your algorithm using different parameter configurations.

Master and Workers communicate via TCP and send serialized messages to each other. They also distribute heartbeats to determine if a node has failed. Disconnected Workers exit their program and are gracefully handled by the still connected nodes. If the Master terminates, fails or cannot be found at the start of the program, applications will also exit.

Workers get informed about all [tuning parameters](parameters.md) by the Master, but they need to know how to connect to the Master when starting. Therefore, you need to provide them with the Master node's public hostname when starting the program. You may additionally set how much information they should print to console.

Troubleshooting: If the worker does not connect to the master, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.
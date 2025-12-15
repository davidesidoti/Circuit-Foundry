### High-level modules

* WorldGrid: occupancy, placement rules, tile types
* BeltSim: deterministic movement, segment queues
* ItemSystem: item definitions, stacks, tags
* MachineSystem: recipes, input/output ports, tick update
* RobotSystem: pathfinding, task execution, inventory
* Scripting: flow graph runtime + text script interpreter
* UI: build menu, inspector panels, debugger

### Simulation tick

Fixed update loop:

* BeltSim tick
* Machine tick
* Robot tick
* Events dispatch

No Unity physics for items on belts.

## Circuit Foundry

Circuit Foundry is a 2.5D automation game where you build a factory and program robots to do the work. Start manual, then automate everything with conveyors, machines, power networks, and a swarm of bots you can literally script.

Core fantasy: you are not placing “magic inserters”. You’re building a production system and teaching it how to think.

### Key features

* Automation backbone: conveyors, splitters, machines, buffering, bottlenecks
* Programmable robots: move, pick, place, sense, communicate, coordinate
* Two programming modes:

  * Visual flow editor for quick logic
  * Text scripting for full control (safe, sandboxed)
* Debug tools: logs, step/run, breakpoints, live variable inspection
* Cozy industrial sci-fi look: readable diorama, soft lighting, satisfying motion

### Gameplay loop

Gather → craft → build → automate → build robots → program robots → scale → unlock smarter hardware → solve new constraints.

### MVP scope (first playable)

* Small grid map
* 6 resources
* Conveyor + splitter
* 3 machines: Smelter, Assembler, Charger
* 1 robot type with basic sensors
* Flow editor + minimal script commands
* Goal: automate “CPU Core” production to unlock higher tier bots

### Tech stack

* Unity (URP)
* C#
* Deterministic belt/item simulation (no physics for items)
* Grid-based world + A* pathfinding
* Custom lightweight scripting runtime

### Development principles

* Keep simulation deterministic and fast
* Favor clarity over realism
* Build vertical slices over giant systems
* Every new feature must create a new player decision, not just more stuff

### Roadmap snapshot

* Phase 1: belts + item simulation + manual crafting
* Phase 2: robots with state machine + tasks
* Phase 3: flow editor + basic scripting + debugging
* Phase 4: power + progression + content expansion
* Phase 5: polish, UX, performance, save/load, mod hooks

### Contributing

PRs welcome once core architecture stabilizes. See `CONTRIBUTING.md`.

---

# Milestones

* M0: Belt playground
* M1: First automated product
* M2: First robot that does useful work
* M3: First programmable loop
* M4: First “player-made” automation strategy
* M5: MVP release steam.com

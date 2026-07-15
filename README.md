# DartsAR

An XR darts prototype built with Unity for Meta/Oculus headsets. The project combines hand-based dart grabbing and throwing, dartboard scoring, and session logging for later analysis.

## Overview

DartsAR is a small VR/AR interaction project centered on a dartboard training loop:

- spawn a fixed set of darts
- grab a dart with hand tracking pinch input
- throw toward the board with smoothed fingertip velocity
- score hits using ring and slice detection
- record headset, hand, and dart data to log files

## Highlights

| Area | What it does |
| --- | --- |
| Hand interaction | Uses Meta hand tracking pinch gestures to grab and release darts |
| Throwing | Estimates throw speed from recent fingertip motion samples |
| Scoring | Detects bullseye, outer bull, single, double, triple, and miss zones |
| Session flow | Spawns up to 15 darts and ends the session after the last hit |
| Research logging | Writes TSV logs with head, hand, object, and event data |

## Tech Stack

- Unity 2022.3.62f3
- Meta XR SDK (`com.meta.xr.sdk.all`)
- XR Interaction Toolkit (`com.unity.xr.interaction.toolkit`)
- Oculus XR Plugin (`com.unity.xr.oculus`)
- TextMesh Pro

## Getting Started

### Requirements

- Unity Hub
- Unity Editor 2022.3.62f3
- A Meta/Oculus-capable XR setup if you want to run the project on device

### Open The Project

1. Clone or download this repository.
2. Open Unity Hub.
3. Add this folder as an existing project.
4. Open it with Unity 2022.3.62f3.
5. Let Unity import packages and regenerate project files.

## Gameplay Flow

1. The dartboard logic spawns a grid of darts at the configured spawn point.
2. The player pinches near a dart to grab it.
3. Releasing the pinch throws the dart using smoothed hand velocity.
4. A hit on the dartboard is converted into a score and shown in the UI.
5. Each hit is logged as an event, and logging stops after the configured number of throws.

## Logging

Session logs are written as `.tsv` files under Unity's persistent data path in:

`DataLogs/`

The logger records:

- frame and time data
- event names such as `SessionStarted` or `Hit_Triple_60`
- headset transform
- left and right hand transforms
- tracked object transforms for tagged darts

## Main Scripts

- `Assets/Scripts/DartboardLogic.cs`: scoring, total score tracking, dart spawning, and session end handling
- `Assets/Scripts/HandThrower.cs`: pinch-based grabbing and velocity-based dart throwing
- `Assets/Scripts/DartProjectile.cs`: dart release and projectile behavior
- `Assets/Scripts/LoggerScript.cs`: TSV session logging for XR transforms and events
- `Assets/Scripts/SimpleThrower.cs`: alternate throwing interaction path

## Repository Notes

- Unity-generated folders such as `Library/`, `Logs/`, and `UserSettings/` are ignored through the root `.gitignore`.
- Asset `.meta` files should remain committed because Unity depends on them.
- Large generated files should not be added manually before publishing to GitHub.

## Suggested GitHub Description

`Unity XR darts prototype with Meta hand tracking, dartboard scoring, and session logging.`
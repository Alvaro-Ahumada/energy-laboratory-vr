# Energy Laboratory VR

## Project title and brief description
Energy Laboratory VR is a seated or standing controller-based Unity experience for Meta Quest. Start at the world-space menu. You have 90 seconds to move three energy cells from their pedestals to the station. Point at a menu button and press the trigger. Grip a cell, transport it to a free receiver and release it nearby. The HUD counts accepted cells. Releasing a cell on the floor does not score. Connect the third cell before time expires to win. Use the teleport ray for short moves and the step turn to reorient yourself.

## Technologies used
Unity 6000.3.6f1, Universal Render Pipeline 17.3.0, XR Interaction Toolkit 3.4.1, Input System 1.19.0, OpenXR Plugin 1.17.1 and Unity OpenXR: Meta 2.5.1. The Meta integration is supplied by the OpenXR packages and controller profile configured in the project. The original Starter Assets scene is located in a sample directory named 3.5.1. Package resolution is recorded in Packages/packages-lock.json.

## How to build and install the APK
Use Unity Hub to install the exact Editor version with Android Build Support, the bundled SDK, NDK and OpenJDK. Open the folder containing Assets, Packages and ProjectSettings. Allow the package manager and asset import to finish. Open Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoScene.unity. In File > Build Profiles select Android. Keep this scene enabled, use IL2CPP, ARM64, minimum API 29 and target API 34. Build an APK. For the academic release use the version recorded in ProjectSettings. A local build uses the developer's local signing identity. Private keys are excluded. For the distributed file compare its SHA-256 with the release manifest.

Enable developer mode on a compatible Quest, connect by USB and authorize the computer in the headset. With Android platform tools run `adb devices`, then `adb install -r EnergyLab.apk` using the downloaded filename. Open the app through the headset's Unknown Sources library. Updating an installed copy requires a matching certificate. Inspect installation errors before changing any installed data.

## Known issues and future improvements
The tutor's physical test identified abrupt movement, an off-axis results panel and an opening at the edge of the room. The Phase 4 revision reduced teleport reach, used a 22.5-degree step turn, recentered the results menu beyond its viewing cone and enclosed the room with six repeated modules. Compare headset results only when the version is identified. Sustained frame rate, tolerance of turning and panel readability need measurements or feedback from the person wearing the device. Flat Android phones without a compatible OpenXR runtime cannot substitute for that test.

## AI tools used
Alvaro Daniel Ahumada Navarro reviewed the course deliverables, personally edited the cell recovery message, recorded the previous narration and reported the texture stretching on the fourth wall. Codex assisted with procedural asset production, implementation, revision tracking, automated Editor checks and documentation. The tutor provided independent observations during the earlier headset trial. Automated input is identified separately from human controller use.

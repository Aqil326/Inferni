# Inferni: Hope & Fear

Codebase for Game `Inferni: Hope & Fear`

## Prerequisites
- Download and install [Unity Hub](https://unity3d.com/get-unity/download) for your machine
- Inside Unity Hub download `Unity Editor 2022.3.25f1` and add Windows modules.
- Install [Git CLI](https://git-scm.com/download/wingithub)
- Download and install [Steam](https://store.steampowered.com/about/)
- [Optional] You can download [Source Tree](https://www.sourcetreeapp.com/) or [Github Desktop](https://desktop.github.com/) or other VCS tools for better version control management.

## How to run
1. Clone current repository to your local folder
2. Open Unity Hub, click `Add` button to add the project from in step 1
3. Click on the project `Well` to open it in Unity Editor (You'll be asked to install the corresponding Unity Editor if you don't have)
4. To run as Client in Editor:  File -> Build Settings -> Windows, Mac, Linux
5. To run as Server in Editor:  File -> Build Settings -> Dedicated Server
6. Set server settings for client
      1. Connect local server
          1. The client will bypass Matchmaking and connect to your local game server directly, which is only for debugging and testing
          2. In EnvironmentSettings, make sure to set the correct `IP` and `port` of your local server
      2. Connect remote server
          1. The client will call Matchmaking server first then connect to the remote game server, which is the real-life workflow
          2. In EnvironmentSettings, make sure to set the correct IP and port of your local server: in EnvironmentSettings, in the OnlineGameConfigs, make sure to toggle  `IsActive` for your expected remote server stack.

7. Setup Steam environtment
      1. Open Steam App on your machine
      2. Ask Cyril to give you the access to download Inferni (or InferniDemo)
      3. Set correct `application_id` in `steam_appid.txt` file
          1. For development in Unity Editor, `steam_appid.txt` is already in the root folder of the codebase, it is used for Steam integration, make sure it has correct `app_id`. (Full game -Inferni: `2731170`, Demo game - Inferni Demo: `3003980`)
          2. For testing client build, put `steam_appid.txt` to the same folder of the build
      4. Set correct `application_id` in `SteamworksSettings`
          1. For development in Unity Editor, make sure to set the correct `application_id` in SteamworksSettings (Full game -Inferni: `2731170`, Demo game - Inferni Demo: `3003980`)
          2. For testing client build, before you make the client build, make sure to set the correct `application_id` in SteamworksSettings (Full game -Inferni: `2731170`, Demo game - Inferni Demo: `3003980`)

## Environment Diagram

![Environment Diagram](https://drive.google.com/uc?export=view&id=1Owh5CWteMJdHZJjasy_I9xlWgvOjWq-m)

## How to package build

1. Make sure Unity Logo has been removed
2. Update build version: File -> Build Settings -> Players Settings -> Version (Bump the version for each new build)
3. Update Playtest Window settings
      1. Update the `ProjectSettings/ClientSettingUpdate.json` file with the Playtest windows for that new build (should be synced for both Mac and Windows clients).
      2. Upload the JSON file to [S3 bucket](https://eu-west-2.console.aws.amazon.com/s3/buckets/village-inferni?region=eu-west-2&bucketType=general&tab=objects)
      3. On the JSON upload page, click Permission -> Access control list (ACL) -> Grant public-read access before clicking on the `upload` button.
      4. Verify uploaded JSON by opening [link](https://village-inferni.s3.eu-west-2.amazonaws.com/ClientSettingUpdate.json) in an incognito window of your browser.
4. Update GameAnalytics Build version: Window -> GameAnalytics -> Select Settings -> Setup -> OSXPlayer -> Build (Update the build version to the same version you set at step2)
5. Server settings and toggles
      1. To build for Steam Demo game
          1. Change Steam Game ID in steam_appid.txt to 3003980
          2. Change the Application ID in Assets -> SteamworksSettings.asset to 3003980
          3. Steam Support: ON
          4. Is Demo: ON
          4. Replace executable / Product name with `InferniDemo`
          5. Activate remote server stack: EnvironmentSettings -> OnlineGameConfigs -> Find your target remote server stack and then tick `IsActive`. (Will read the first active OnelineGmeConfigs)
      2. To build for Steam Full/Main Game
          1. Change Steam Game ID in steam_appid.txt to 2731170
          2. Change the Application ID in Assets -> SteamworksSettings.asset to 2731170
          3. Steam Support: ON
          4. Is Demo: OFF
          4. Replace executable / Product name with `Inferni`
          5. Activate remote server stack: EnvironmentSettings -> OnlineGameConfigs -> Find your target remote server stack and then tick `IsActive`. (Will read the first active OnelineGmeConfigs)

6. [Optional] Debug Menu: to enable Debug Menu in the build, toggle on `Build Settings -> Development Build`
7. [Optional] Minimum player amount: to change minimum player amount for quick testing, go to `GlobalGameSettings.asset -> Min Player Amount`, change it to a even number you expect.

8. Make Build
      1. To build for Windows platform
          1. Target Platform: Windows
          2. Architecture: Intel 64-bit
          3. Click Build button, then chose a folder to save the build
      2. To build for MacOS platform
          1. Target Platform: macOS
          2. Architecture: Intel 64-bit + Apple Silcon
          3. Click Build button, then chose a folder to save the build
9. Ship the build
      1. Ship the executable file
          1. Find the folder that you save the compiled build
          2. Copy the `steam_appid.txt` file to the same folder where the Inferni.app or Inferni.exe is
          3. Select the folder that contains both Inferni.app/Inferni.exe and the `steam_appid.txt` file, then compress the folder
          4. Send the compressed file to testers or upload to somewhere
      2. Upload to Steam
          1. Find the folder that you save the compiled build
          2. Delete the folder with name `_BurstDebugInformation_DoNotShip` or contains `DoNotShip`
          3. Use the [SteamDeploymentTool](https://github.com/Village-Studio/SteamDeploymentTool) and follow the READ guide there to upload the build to Steam
10. Commit the build version related changes
      1. Commit changes of file `Assets/Resources/GameAnalytics/Settings.asset`
      2. Commit changes of file `ProjectSettings/ProjectSettings.asset`


## Deployment
- [Deployment Guide for Steam and UGS Server](https://docs.google.com/document/d/1co3jSDyFvVjwkp1-XJUlYZO51LfY7UdvNeezXRKoHfk/edit?usp=sharing)
- [Video - Build and Upload Linux Server to UGS](https://www.loom.com/share/b051fa323b2146cfb30babef39903506)
- With the Steam components in the code, we now also need to include `steam_appid.txt` in the server upload, putting it in the same folder as the executable `Inferni_Linux_server.x86_64`

## Advanced Debugging Support
1. Specifying the compiled client (e.g. Inferni.app) to connect to a specific Server address: `./Inferni.app/Contents/MacOS/Inferni -port 9000 -ip 127.0.0.1 -isLocDev true`
2. Specifying the compiled dedicated server (e.g. Inferni_Mac_Server.app) to listen to a specific port: `./Inferni_Mac_Server/Inferni -port 9000`

## More Steam Integration Knowledge
The file `steam_appid.txt` contains the Steam App ID for the game. It has to be put into the Project Root directory when running in Unity Editor for the Steamwork.Net SDK to pick it up. For compiled executables, it has to be in the same folder.  The implication is that if you are running multiple executables, they would all be logged in as the same Steam users.  To avoid that, please put the instances of the executables in separate directory and make sure only one of them have the correct `steam_appid.txt` (located in the Root of the Well Project)(it seems that the game would auto-generate a default incorrect `steam_appid.txt` when none is present).  This will allow you to launch the game and `Start Online Game`.  However, to use the Steam Lobby feature, the main Steam app has to be running and your Steam account need to be "associated" with the game you are developing.

## UGS server troubleshooting
### Find service on Unity Cloud dashboard
1. Open [Unity Cloud Daashboard -> Dashboard -> Game Service Hosting -> Servers](https://cloud.unity.com/home/organizations/5773192982720/projects/c4a98cc9-ca1e-44b8-b71f-5046af404dfd/environments/beb493db-d5bb-4d84-97c1-52a2b4c57d4e/multiplay/servers)
2. Scroll down / next page to find the corresponding stack that you are working on

### Context and Knowledge
When no one is playing, it should be at Status Available. When a game is ongoing, it should be in Allocated.

Problems are normally due to the server got stuck and not going back to Available state after a game, click on the Blue number that represent the server, and then click `Stop Server` (not `Restart`!)
The problematic server should go back to Available

If there is issue for all 4 clients connected to the same game, then it is likely that there are some ghost tickets in the UGS matchmaker.  If restarting the clients doesn’t help. It is best to wait for 5 minutes.  Then all ghost tickets should have timed out automatically.

## Team Conventions

### Code Style
We built consistent code style in the codebase, the basic alignments are:
- File name
  - Game Object in the Hierarchy window: Upper Camel Case, e.g. `GameManager`
  - Folder in the Project Window: Upper Camel Case, e.g. `Animations`
  - Prefab: Upper Camel Case, e.g. `Card3D`
- Variable name
  - Class public variable: Upper Camel Case, e.g.
  ```csharp
  public static GameManager Instance;
  ```
  - Class private variable: Lower Camel Case, e.g.
  ```csharp
  private static Dictionary<Type, BaseManager> subManagersDictionary;
  ```
  - Method input variable: Lower Camel Case, e.g. the `character` in:
  ```csharp
  private void CheckForWinCondition(Character character)
  ```
- Method name: we name all methods following Upper Camel Case, no matter if it's public/protected/private.

Tips: if you don't know how to do the naming, please try to find something similar in the codebase and then follow that pattern.

### Branches
- dev: branch for development, all PRs base branch
- main: branch for release, every time after the play test, we'll merge code from `dev` to `main`
- other branches: we use PRs to cooperate, every time we start to work on a task from Jira, we'll name our branch with Jira task number for better tracking, for example: `feature/VILLAGE-155`, `fix/VILLAGE-162`.


### Tags

We'll create a tag from `main` branch after each release.

### PRs

We use PRs to cooperate with each other. Every time when we finish a Jira task, we'll create a github PR, the format is:
- Base branch: `dev`, `target branch`: your current branch name
- Name: your Jira task’s title, e.g. `[UX] Improved Projectile Design`
- Descriptions: Write necessary descriptions for the PR for clarification/communication purpose, if everything can be checked in the code, you can skip it.

We don't like unused zombie branches in the repository, so every time when we merge the PR, we'll also delete the PR branch.

To keep dev branch clean, we'll choose `Squash and Merge` option when we merge the PR.
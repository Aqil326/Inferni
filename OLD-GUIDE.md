# Inferni: Hope & Fear

Outdated guide for Game `Inferni: Hope & Fear`

### How to Package and upload WASD demo build

#### Codebase checklist
1. GlobalGameSettings.asset -> Max / Min Player Amount = 8/4
2. GameManager Prefab -> Is Staging -> Tick
3. GameManager Prefab -> Is WASD -> Tick
4. GameManager Prefab -> WASD Position Id -> use 0,1,2 and 3 , need 4 clients with different Pos ID
5. File -> Build Settings -> Players Settings -> Product Name -> InferniX where X corresponding to 0,1,2 and 3 above
6. File -> Build Settings -> Players Settings -> Version -> WASD_1.0.X  <-- replace the version number according.  The clients and server version string must match

#### Distribution Tips
1. Build the files with suffix 0,1,2 and 3 to identify their position, e.g. Inferni_Mac_client0
2. For PC build, remember to remove the sub-folder `XXX_BurstDebugInformation_DoNotShip`
3. On Mac, right click on the build (a file/app Mac build, a folder for PC build), select "Compress XXX" where XXX is the name for the build.  MacOS will then compress the build to a zip file.  Upload that file to Google Drive for distribution
4. For WASD, the Linux server files need to be uploaded to WASD-Staging Build

### How to enable Gametester.gg PIN UI before Gametester playtest

#### Step1. Get `Developer Token` and `Testing PIN` from gametester.gg
- After we create a new Test on Gametester.gg, get the `Developer Token` on gametester.gg dashboard from: Tests -> Create -> API Information -> `TEST AUTHENTICATION TOKEN`

![GametesterSetup1.1](https://drive.google.com/uc?export=view&id=1noFvB0HNd6Wu2IvciqrvJRgLD9L1YG8x)

- Get the `Testing PIN` from: Tests -> Create -> API Validation -> Your Testing PIN

![GametesterSetup1.2](https://drive.google.com/uc?export=view&id=1Ki9NF5q-fPVEsNqALWVx17s0B2Sy6zDg)

#### Step2. Update code to enable Gametester.gg PIN UI
- Enable the `GameTesterManager` from `MainScene`, then update the Developer Token with the token you get from step 1.

![GametesterSetup2.1](https://drive.google.com/uc?export=view&id=1_nQ_KNZgDQSLX_uDekLMkTayX8QDcuE_)

- Open `GameTesterManager.cs` from your IDE, update the `TEST_NO` from the code, now you're ready to run the game, and the gametester.gg PIN UI will be shown when you launch the game for the first time.

![GametesterSetup2.2](https://drive.google.com/uc?export=view&id=1L_2ZK1R3ik-WZGXINvUUWpvjOC_3B9L5)

#### Step3. Unlock the testing to pass Gametester.gg `API Validation`
- To finish the testing setup, we need to call the gametester.gg `/unlock` API at least for once. Our current unlock testing logic is finishing the gameplay for 3 times.
- When run the game for the first time, the gametester.gg PIN UI will be shown, fill the `Testing PIN` you get from step 1.

![GametesterSetup3.1](https://drive.google.com/uc?export=view&id=1AGHkNppTCshS9iYV2HRALab2GbqSDSRw)

- Finish three gameplay (win and lose both work) to get the Unlock testing UI shown

![GametesterSetup3.2](https://drive.google.com/uc?export=view&id=1v9rn5ZOd4r46f3hoBWKXgcQj00M3jjZK)

- Go back to the gametester.gg dashboard, now you should be able to see the `API Validation` is passed

You're good to submit the code changes now! Can check this commit example for some reference: https://github.com/Village-Studio/Well/commit/8e5a260b
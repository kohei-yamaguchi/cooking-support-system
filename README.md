# Cooking Support System

This is a Unity project of the Cooking Support System.

## Prerequisites

- OS: Windows 10
- Unity version: 2017.3.1f1
- MySQL version: 8.0.15
- PC: VR Ready (https://ocul.us/compat-tool)
- Device: Oculus Rift CV1 and Touch

## How to Set Up

### Install Unity
1. Download Unity 2017.3.1 installer from the following link.
https://unity3d.com/jp/get-unity/download/archive
2. Install Unity 2017.3.1f1.

### Install Oculus Software
1. Download Oculus Rift software from the following link.
https://www.oculus.com/setup/
2. Install Oculus software.

### Import Oculus Utilities for Unity

1. Download "Oculus Utilities for Unity ver.1.24.0" from the following link.  
https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/1.24.0/
2. Unzip the downloaded file.
3. Open this project with Unity.
4. Click [Assets]-[Import Package]-[Custom Package...].
5. Select and open "OculusUtilities.unitypackage".
6. Click [Import] button.
7. Click [Yes] on "Update Oculus Utilities Plugin" window.
8. Click [Restart] on "Restart Unity" window.

### Import Final IK

1. Purchase "Final IK" from the following link.  
https://assetstore.unity.com/packages/tools/animation/final-ik-142902. 
2. Import Final IK into this project with Unity.

### Import executable file and dll for TTS
1. Prepare "ConsoleSimpleTTS.exe" and "Interop.SpeechLib.dll", following the repository: https://github.com/PartnerRobotChallengeVirtual/console-simple-tts
2. Copy those files to the [TTS] folder in the same directory as README.md.

### Set Up MySQL Database

1. Download MySQL installer (8.0.15 is available).
2. Install MySQL.
	- Use Legacy Authentication Method.
	- Root password is "**********".
	- Other settings are default.
3. Open MySQL Workbench, and click a connection.
4. Click "Data Import" in "Server" tab.
5. Check "Import from Dump Project Folder", refer "CookingSupportSystem/dumps/DumpCooking", and start import.
6. Open this project with Unity.
7. Click "DatabaseManager" in Hierarchy window.
8. Input root password into "My Sql Password" in Inspector window.


## How to Build

1. Create a "Build" folder in this project folder.
2. Open this project with Unity.
3. Click [File]-[Build Settings].
4. Click [Build].
5. Select the "Build" folder.
6. Type a file name and save the file.  


## How to Execute

### Execute On Unity Editor
1. Double click "Assets/CookingSupport/Scenes/CookingGuidance(.unity)" in Project window.
2. Click the Play button at the top of the Unity editor.  

### Execute the Executable file
1. Copy the "SIGVerseConfig" folder into the "Build" folder.
2. Double Click the "*.exe" in the "Build" folder.


## How to Do SSH Connection
1. Install Cygwin.
2. Open Cygwin, and create OpenSSH key.
3. Set up for becoming a client of NII server using a public key.
4. Install Teraterm.
5. Open Teraterm, and connect to server (socio3.iir.nii.ac.jp) using a secret key.
6. Click 'SSH Transfer' in the Settings, and add 'local 53306, remote 3306'.


## Reference

https://github.com/shiori-yokota

## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
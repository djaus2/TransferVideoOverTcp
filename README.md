# TransferVideoOverTcp

> Yet another repository created with the help of GitHub Copilot!

## About
Transfer a video locally from an Android phone to Windows Desktop over TCP.

## Projects
- GetVideo
  - Console app that receives the video
-  DownloadVideoOverTcpLib
    - Library used by Console app to implement reception of video file over TCP.
- SendVideo
  - MAUI Android phone app to send video from /Movies folder
  - UI includes selection of file from the folder
- SendVideoOverTcpLib
  - MAUI Library used by SendVideo too implement sending of video file over TCP
 
## Notes
- THe filename of the source file is sent and used when saving the the received file.
- A checksum is determined and sent as part of the transmission and checked upon reception.
- The file is sent as 1MB packets.
- GetVideo inludes option paameter to set the target folder _(default c:\temp\AAA)_ and TCP port _(default 5000)_.

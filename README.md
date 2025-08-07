# TransferVideoOverTcp

> Yet another repository created with the help of GitHub Copilot!

## About
Transfer a video locally from an Android phone to Windows Desktop over TCP.

## Projects
- :movie_camera: **GetVideo**
  - Console app that receives the video
    - Uses NuGet Packae  [Sportronics.ConfigurationManager](https://www.nuget.org/packages/Sportronics.ConfigurationManager) to handle command line options
  - **DownloadVideoOverTcpLib**
    - Library used by Console app to implement reception of the video file over TCP.
- :video_camera: **SendVideo**
  - MAUI Android phone app to send video from /Movies folder
    - UI includes selection of file from the folder
  - **SendVideoOverTcpLib**
    - MAUI Library used by SendVideo to implement sending of video file over TCP
- :new: :running: **GetVideoWPF**  A WPF desktop app to manage reception of video files like the Console app. (Some rough edges)
  - :new: **GetVideoService** A Windowws service that is used by GetVideo to manage the reception of files.
 
## Notes
- The filename of the source file is sent and used when saving the the received file.
- A checksum is determined and sent as part of the transmission and checked upon reception.
- The file is sent as 1MB packets.
- GetVideo includes optional paameter to set the target folder _(default c:\temp\AAA)_ and TCP port _(default 5000)_.

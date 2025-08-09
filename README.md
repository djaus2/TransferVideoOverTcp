# TransferVideoOverTcp

> Yet another repository created with the help of GitHub Copilot!

## About
Transfer a video locally from an Android phone to Windows Desktop over TCP.

----
> Better WPF app coming tomorrow.  Doesn't use Windows Service but uses in-app only Tcp service.
----

## Projects
- :video_camera:  **GetVideoConsole**
  - Console app that receives the video
    - Uses NuGet Packae  [Sportronics.ConfigurationManager](https://www.nuget.org/packages/Sportronics.ConfigurationManager) to handle command line options
  - :small_airplane: **DownloadVideoOverTcpLib**
    - Library used by Console app to implement reception of the video file over TCP.
- :telephone: **SendVideo**
  - MAUI Android phone app to send video from /Movies folder
    - UI includes selection of file from the folder
  - :postbox: **SendVideoOverTcpLib**
    - MAUI Library used by SendVideo to implement sending of video file over TCP
- :new: :satellite: **GetVideoViaSvcWPF**  A WPF desktop app to manage reception of video files like the Console app. _(Some rough edges)_
  - :new:   :post_office: **GetVideoService** A Windowws service that is used by GetVideo to manage the reception of files.
  - **NOTE: MAY NEED TO RUN THIS WITH ELEVATED PRIVLEDGES IS SERVICE ISN't RUNNING** *See next section.*
  -  **GetVideoService** is a Windows Service that runs in the background to receive video files over TCP.
  -  Uses ***DownloadVideoOverTcpLib***
    - It can be installed/uninstalled via the WPF app or manually using the provided batch script.
    - It listens for incoming video transfers and saves them to a specified directory.
    - Uses `appsettings.json` for configuration.
- :new: :telephone_receiver: **GetVideoInAppWPF** A WPF desktop app to manage reception of video files like the Console app. _(No rough edges)_
  - This app runs a Tcp service in-app and does not require a separate Windows Service.
  - Uses ***DownloadVideoOverTcpLib***
  - It has similar UI and functionality to GetVideoViaSvcWPF but without the service management features.
  - Also similar to the Console app but does not stop Tcp service nor exit the app after a download is complete.

## Running GetVideoViasSvcWPF in Elevated Mode.
Install/Uninstall does not require elevated mode. To Start the sevice though, you need to be in elevated mode.
In The apps AppManifest there are 2 entries. One commented out:
```xaml
        <!-- requestedExecutionLevel level="asInvoker" uiAccess="false" /-->
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```
If you run the app from Visual Studio as is, it will request to run in Elevated mode and if accepted will restart Visual studio in that mode and hence you can run it from there.
If you wish to not use the app to start the service, use the commented out Manifest entry.  You can manually start the service or even set it to Automatic.

You can run the app from the comamnd line thus in a Powershell:
```ps
cd the built directory eg.....\DownloadVideoOverTcp\GetVideoWPF\bin\Debug\net9.0-windows
Start-Process ".\GetVideoWPF.exe" -Verb  RunAs
```

## Notes
- The filename of the source file is sent and used when saving the the received file.
- A checksum is determined and sent as part of the transmission and checked upon reception.
- The file is sent as 1MB packets.
- GetVideoConsole includes optional paameter to set the target folder _(default c:\temp\AAA)_ and TCP port _(default 5000)_.
  - In the WPF apps the folder can be set usingthe UI.
- The Port used for the TCP connection can be set in the appsettings.json file for the Windows Service and WPF apps.
  - Also can be set in the UI of the WPF apps.
  - It is a command line option for the Console app.
- SEndVideo uses a configurable TimeOut for sends _(default 15 seconds)_.

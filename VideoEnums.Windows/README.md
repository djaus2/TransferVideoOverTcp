# VideoEnums.Windows package

> Note same as VideoEnums but for Windows OS

Used by WPF Apps at [GitHub djaus2/TransferVideoOverTcp](https://github.com/djaus2/TransferVideoOverTcp)

```cs
    public enum TimeFromMode
    {
        FromVideoStart, //From start of video capture
        FromGunSound, //From gun sound
        FromGunFlash,  //From observed flash of gun on video
        ManuallySelect, //Manually selected start time
        WallClockSelect
    }

    public enum VideoDetectMode
    {
        FromFlash, //Detect flash in video
        FromFrameChange, //Detect motion in video
        FromMotionDetector //Detect frame change in video.
    }
```

```
    public VideoInfo
    {
        // Properties
        byte[] Checksum { get; set; }
        VideoDetectMode DetectMode { get; set; }
        DateTime GunTime { get; set; }
        string RecordedFilename { get; set; }
        string RecordedVideoPath { get; set; }
        string SelectedFilename { get; set; }
        string SelectedVideoPath { get; set; }
        TimeFromMode TimeFrom { get; set; }
        DateTime VideoStart { get; set; }

        // Static Mthods
        static abstract string EncodeDateTime(DateTime dat);
        static abstract DateTime ParseCustomDateTime(string input);

        // Methods
        void AppendTimesToVideoFilename();
        string ConcatenateTimeFromandVideoTimes();
        void GetCheckSum();
        bool GetMetaInfoandFilename();
        string ToJson();
        string ToString();
    }
```
# TabloRecordingExtractor
A C# .Net WPF application that can find all recordings on a Tablo OTA DVR, read their meta data, extract the TS files, and combine them into an MP4 using FFMPEG.

Please note, FFMPEG is required for this application to convert the numerous TS files stored by the Tablo per recording into a single MP4 video file. FFPMEG can be downloaded here: https://ffmpeg.org/

This application uses the Newtonsoft.Json library to read the Tablo json metadata into a class.

There's some commented out code for using the Handbrake CLI to convert the final MP4 into an MKV but I haven't made that as configurable as it should be.

using Android.Content;
using Android.Database;
using Android.Provider;
using Java.IO;
using SendVideoOverTCPLib.Services;
using System;
using System.Threading.Tasks;
using AndroidUri = Android.Net.Uri;

namespace SendVideoOverTCPLib.Platforms.Android
{
    public class VideoMetadataService : IVideoMetadataService
    {
        public async Task<DateTime> GetVideoCreationDateAsync(string filePath)
        {
            // Default to current time in case we can't get the actual creation time
            DateTime creationTime = DateTime.Now;

            try
            {
                var context = Platform.CurrentActivity?.ApplicationContext;
                if (context == null)
                    return creationTime;

                // If it's a content URI (starts with content://)
                if (filePath.StartsWith("content://"))
                {
                    AndroidUri contentUri = AndroidUri.Parse(filePath);
                    creationTime = GetCreationTimeFromContentUri(contentUri, context);
                }
                // If it's a file URI or regular file path
                else
                {
                    return creationTime;
                    // Try to get creation date from MediaStore
                    //var file = new System.IO.File(filePath);
                    //if (file.Exists())
                    //{
                    //    // Try to find in MediaStore by display name
                    //    string fileName = file.Name;
                    //    creationTime = GetCreationTimeByFileName(fileName, context) ?? creationTime;
                    //}
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting video creation time: {ex.Message}");
            }

            return creationTime;
        }

        public async Task<VideoFileInfo> PickVideoAsync()
        {
            VideoFileInfo videoFileInfo = null;
            
            try
            {
                // Use FilePicker to get the file
                var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "video/*" } }
                });

                var options = new PickOptions
                {
                    PickerTitle = "Select a Video File",
                    FileTypes = customFileTypes
                };

                var fileResult = await FilePicker.PickAsync(options);
                if (fileResult == null)
                    return null;
                    
                videoFileInfo = new VideoFileInfo
                {
                    FilePath = fileResult.FullPath,
                    FileName = fileResult.FileName,
                    CreationTime = DateTime.Now // Default value
                };
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Selected file: {fileResult.FileName}, Path: {fileResult.FullPath}");
                
                // Get the original creation time
                var context = Platform.CurrentActivity?.ApplicationContext;
                if (context != null)
                {
                    try
                    {
                        // Try to get from content URI if available
                        if (fileResult.FullPath.StartsWith("content://"))
                        {
                            AndroidUri contentUri = AndroidUri.Parse(fileResult.FullPath);
                            videoFileInfo.CreationTime = GetCreationTimeFromContentUri(contentUri, context);
                            System.Diagnostics.Debug.WriteLine($"Got creation time from content URI: {videoFileInfo.CreationTime}");
                        }
                        else
                        {
                            // Try to get by file name
                            DateTime? mediaStoreTime = GetCreationTimeByFileName(fileResult.FileName, context);
                            if (mediaStoreTime.HasValue)
                            {
                                videoFileInfo.CreationTime = mediaStoreTime.Value;
                                System.Diagnostics.Debug.WriteLine($"Got creation time from MediaStore: {videoFileInfo.CreationTime}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Could not find creation time in MediaStore");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting creation time: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Context is null, using default creation time");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking video: {ex.Message}");
                return null;
            }

            return videoFileInfo;
        }

        private DateTime? GetCreationTimeByFileName(string fileName, Context context)
        {
            try
            {
                string[] projection = { 
                    MediaStore.Video.Media.InterfaceConsts.DateAdded,
                    MediaStore.Video.Media.InterfaceConsts.DateModified,
                    MediaStore.Video.Media.InterfaceConsts.DateTaken
                };

                string selection = $"{MediaStore.Video.Media.InterfaceConsts.DisplayName} = ?";
                string[] selectionArgs = { fileName };

                using var cursor = context.ContentResolver.Query(
                    MediaStore.Video.Media.ExternalContentUri,
                    projection,
                    selection,
                    selectionArgs,
                    null);

                if (cursor != null && cursor.MoveToFirst())
                {
                    // Try to get the date taken first (most accurate for videos)
                    int dateTakenColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken);
                    if (dateTakenColumnIndex != -1 && !cursor.IsNull(dateTakenColumnIndex))
                    {
                        long dateTakenMs = cursor.GetLong(dateTakenColumnIndex);
                        return DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs).LocalDateTime;
                    }

                    // Fall back to date added
                    int dateAddedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
                    if (dateAddedColumnIndex != -1 && !cursor.IsNull(dateAddedColumnIndex))
                    {
                        long dateAddedSeconds = cursor.GetLong(dateAddedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateAddedSeconds).LocalDateTime;
                    }

                    // Last resort: date modified
                    int dateModifiedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateModified);
                    if (dateModifiedColumnIndex != -1 && !cursor.IsNull(dateModifiedColumnIndex))
                    {
                        long dateModifiedSeconds = cursor.GetLong(dateModifiedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateModifiedSeconds).LocalDateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creation time by file name: {ex.Message}");
            }

            return null;
        }

        private DateTime GetCreationTimeFromContentUri(AndroidUri contentUri, Context context)
        {
            DateTime creationTime = DateTime.Now;

            try
            {
                string[] projection = { 
                    MediaStore.Video.Media.InterfaceConsts.DateAdded,
                    MediaStore.Video.Media.InterfaceConsts.DateModified,
                    MediaStore.Video.Media.InterfaceConsts.DateTaken
                };

                using var cursor = context.ContentResolver.Query(contentUri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    // Try to get the date taken first (most accurate for videos)
                    int dateTakenColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken);
                    if (dateTakenColumnIndex != -1 && !cursor.IsNull(dateTakenColumnIndex))
                    {
                        long dateTakenMs = cursor.GetLong(dateTakenColumnIndex);
                        return DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs).LocalDateTime;
                    }

                    // Fall back to date added
                    int dateAddedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
                    if (dateAddedColumnIndex != -1 && !cursor.IsNull(dateAddedColumnIndex))
                    {
                        long dateAddedSeconds = cursor.GetLong(dateAddedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateAddedSeconds).LocalDateTime;
                    }

                    // Last resort: date modified
                    int dateModifiedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateModified);
                    if (dateModifiedColumnIndex != -1 && !cursor.IsNull(dateModifiedColumnIndex))
                    {
                        long dateModifiedSeconds = cursor.GetLong(dateModifiedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateModifiedSeconds).LocalDateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creation time from content URI: {ex.Message}");
            }

            return creationTime;
        }
    }
}

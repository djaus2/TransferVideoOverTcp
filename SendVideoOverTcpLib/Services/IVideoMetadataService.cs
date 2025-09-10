using System;
using System.Threading.Tasks;

namespace SendVideoOverTCPLib.Services
{
    public interface IVideoMetadataService
    {
        /// <summary>
        /// Gets the original creation date/time of a video file
        /// </summary>
        /// <param name="filePath">The path to the video file</param>
        /// <returns>The creation date/time of the video file</returns>
        Task<DateTime> GetVideoCreationDateAsync(string filePath);
        
        /// <summary>
        /// Picks a video file and returns its path and metadata
        /// </summary>
        /// <returns>Video file information including path and creation date</returns>
        Task<VideoFileInfo> PickVideoAsync();
    }

    public class VideoFileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreationTime { get; set; }
    }
}

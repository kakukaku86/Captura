﻿using Screna.Audio;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Screna
{
    /// <summary>
    /// Encode Video using FFMpeg.exe
    /// </summary>
    public class FFMpegVideoWriter : IVideoFileWriter
    {
        readonly string _path;
        static readonly Random Random = new Random();
        static readonly string BaseDir = Path.Combine(Path.GetTempPath(), "Screna.FFMpeg");
        int _fileIndex;
        readonly string _fileNameFormat;
        AudioFileWriter _audioWriter;
        string _ffmpegArgs;

        static FFMpegVideoWriter()
        {
            if (!Directory.Exists(BaseDir))
                Directory.CreateDirectory(BaseDir);
        }

        /// <summary>
        /// Creates a new instance of <see cref="FFMpegVideoWriter"/>.
        /// </summary>
        /// <param name="FileName">Path for the output file... Output video type is determined by the file extension (e.g. ".avi", ".mp4", ".mov").</param>
        /// <param name="FrameRate">Video Frame Rate.</param>
        public FFMpegVideoWriter(string FileName, int FrameRate, IAudioProvider AudioProvider = null)
        {
            int val;

            do val = Random.Next();
            while (Directory.Exists(Path.Combine(BaseDir, val.ToString())));

            _path = Path.Combine(BaseDir, val.ToString());
            Directory.CreateDirectory(_path);

            _fileNameFormat = Path.Combine(_path, "img-{0:D7}.png");

            if (AudioProvider != null)
                _audioWriter = new AudioFileWriter(Path.Combine(_path, "audio.wav"), AudioProvider.WaveFormat);
         
            // FFMpeg Command-line args
            _ffmpegArgs = $"-r {FrameRate}";

            _ffmpegArgs += $" -i \"{Path.Combine(_path, "img-%07d.png")}\"";

            if (_audioWriter != null)
                _ffmpegArgs += $" -i \"{Path.Combine(_path, "audio.wav")}\"";

            _ffmpegArgs += " -vcodec libx264 -pix_fmt yuv420p";

            if (_audioWriter != null)
                _ffmpegArgs += " -acodec aac -b:a 192k";

            _ffmpegArgs += $" \"{FileName}\"";
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Task.Run(() =>
            {
                using (var p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = "ffmpeg.exe";
                    p.StartInfo.Arguments = _ffmpegArgs;
                    p.Start();
                    p.WaitForExit();
                    
                    Debug.WriteLine(p.StandardOutput.ReadToEnd());
                }

                _audioWriter?.Dispose();

                Directory.Delete(_path, true);
            });
        }

        /// <summary>
        /// Gets whether audio is supported.
        /// </summary>
        public bool SupportsAudio { get; } = true;
        
        /// <summary>
        /// Write audio block to Audio Stream.
        /// </summary>
        /// <param name="Buffer">Buffer containing audio data.</param>
        /// <param name="Length">Length of audio data in bytes.</param>
        public void WriteAudio(byte[] Buffer, int Length)
        {
            _audioWriter?.Write(Buffer, 0, Length);
        }

        /// <summary>
        /// Writes an Image frame.
        /// </summary>
        /// <param name="Image">The Image frame to write.</param>
        public void WriteFrame(Bitmap Image)
        {
            Image.Save(string.Format(_fileNameFormat, ++_fileIndex), ImageFormat.Png);
            Image.Dispose();
        }
    }
}

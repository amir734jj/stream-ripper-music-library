using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using StreamRipperMusicLibrary.Models;
using Newtonsoft.Json;

namespace StreamRipperMusicLibrary.Services
{
    internal class StreamRadioModel
    {
        public string Url { get; set; }
        public string FolderName { get; set; }
        public CancellationTokenSource SongChangeThreadToken { get; set; }
        public CancellationTokenSource SongDownloaderThreadToken { get; set; }
        public Task SongChangeThread { get; set; }
        public Task SongDownloaderThread { get; set; }
    }
    
    public class StreamRadioRipper
    {
        private bool Complete { get; set; } = false;
        private string CurrentSongName { get; set; } = string.Empty;
        private const string FolderPrefix = "wwwroot/music/";
        private const string Extension = ".mp3";
        
        private StreamRadioModel CurrentStreamModel { get; set; }
        
        private static readonly Dictionary<string, StreamRadioModel> StreamRadioModels = new Dictionary<string, StreamRadioModel>(); 
        public static readonly List<string> Streams = new List<string>();
        
        /// <summary>
        /// Constructor that starts two child threads
        /// </summary>
        /// <param name="url"></param>
        public StreamRadioRipper(string url)
        {
            if (StreamRadioModels.ContainsKey(url)) return;

            // set Url
            var folderName = new Uri(url).Host;

            CreateDirectoryIfNotExist(folderName);

            // start a new thread that calls two other child threads
            var songChangeThreadToken = new CancellationTokenSource();
            var songChangeThread = Task.Factory.StartNew(() => SongChange(url), songChangeThreadToken.Token);

            var songDownloaderThreadToken = new CancellationTokenSource();
            var songDownloaderThread = Task.Factory.StartNew(() => ReadStreamAndDumpToFile(url), songDownloaderThreadToken.Token);

            CurrentStreamModel = new StreamRadioModel()
            {
                Url = url,
                FolderName = folderName,
                SongChangeThread = songChangeThread,
                SongDownloaderThread = songDownloaderThread,
                SongChangeThreadToken = songChangeThreadToken,
                SongDownloaderThreadToken = songDownloaderThreadToken
            };
            
            // add thread to the pool
            StreamRadioModels.Add(url, CurrentStreamModel);
            Streams.Add(url);
        }

        /// <summary>
        /// Creates a directory given folder name
        /// </summary>
        /// <param name="folderName"></param>
        private void CreateDirectoryIfNotExist(string folderName)
        {
            var directoryPath = Path.Combine(FolderPrefix, folderName);

            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Formats song name to be written to the file
        /// </summary>
        /// <param name="artistName"></param>
        /// <param name="songTitle"></param>
        /// <returns></returns>
        private string FromatSongName(string artistName, string songTitle)
        {
            return $"{artistName}-{songTitle}{Extension}";
        }

        /// <summary>
        /// Formats file name and merges folder into file
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string FormatFileName (string folder, string fileName)
        {
            return Path.Combine(FolderPrefix, folder, fileName);
        }

        /// <summary>
        /// Event listener on sound change, sets complete boolean value
        /// to true to trigger save event
        /// </summary>
        /// <param name="url"></param>
        private void SongChange(string url)
        {
            var radio = new Radio(url);
            radio.OnCurrentSongChanged += (sender, eventArgs) =>
            {
                CurrentSongName = FromatSongName(eventArgs.NewSong.Artist, eventArgs.NewSong.Title);
                Complete = true;
                Console.WriteLine(eventArgs.NewSong.Artist + " - " + eventArgs.NewSong.Title);

                if (CurrentStreamModel.SongChangeThreadToken.IsCancellationRequested) radio.Stop();
            };

            radio.Start();
            GC.KeepAlive(radio);
        }

        /// <summary>
        /// Read and download stream to file
        /// </summary>
        /// <param name="Url"></param>
        private void ReadStreamAndDumpToFile(string Url)
        {
            // time to get the name
            // Thread.Sleep(10000);

            while (string.IsNullOrEmpty(CurrentSongName));

            if (!string.IsNullOrEmpty(CurrentSongName))
            {
                var webClient = new WebClient();
                var downloadStream = webClient.OpenRead(Url);

                Stream file = File.Create(FormatFileName(CurrentStreamModel.FolderName, CurrentSongName));
                CopyStream(downloadStream, file);
            }
            else
            {
                throw new Exception("Name is empty string, maybe Url is not reachabel!");
            }
        }

        /// <summary>
        /// Copies stream to file
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        private void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[64 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);

                if (Complete && !File.Exists(CurrentSongName))
                {
                    input.Flush();

                    // save stream to file
                    output.Flush();
                    output.Close();
                    output = File.Create(FormatFileName(CurrentStreamModel.FolderName, CurrentSongName));

                    Complete = false;
                }
                
                if (CurrentStreamModel.SongDownloaderThreadToken.IsCancellationRequested) break;
            }
        }

        /// <summary>
        /// Stops a stream
        /// </summary>
        /// <param name="url"></param>
        public static void StopStream(string url)
        {
            if (!Streams.Contains(url)) return;
            
            StreamRadioModels[url].SongChangeThreadToken.Cancel();
            StreamRadioModels[url].SongDownloaderThreadToken.Cancel();
                
            StreamRadioModels.Remove(url);
            Streams.Remove(url);
        }

        /// <summary>
        /// Given stream url and filename, it deletes the file in stream folder
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        public static void DeleteSong(string url, string fileName)
        {
            if (!Streams.Contains(url)) return;

            var directoryName = StreamRadioModels[url].FolderName;

            var path = Path.Combine(FolderPrefix, directoryName, fileName);

            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                throw new Exception("Stream is still writing to the file, wait until stream finishes and then delete!");
            }
        }

        /// <summary>
        /// Given file name, it returns artist and song name as string tuple
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static (string, string) FileNameToArtistAndSongName (string fileName)
        {
            fileName = fileName.Substring(fileName.LastIndexOf("/", StringComparison.Ordinal) + 1);
            var artistName = fileName.Split("-")[0];
            var songName = fileName.Split("-")[1].Replace(Extension, string.Empty);

            return (artistName, songName);
        }

        /// <summary>
        /// Returns list of songs that have been downloaded
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<SongModel> DownloadedSongs(string url, bool info = false)
        {
            if (!Streams.Contains(url)) return new List<SongModel>();
            
            var directoryName = StreamRadioModels[url].FolderName;

            directoryName = Path.Combine(FolderPrefix, directoryName);

            return Directory.GetFiles(directoryName)
                .Select(x => x.Replace(@"\", "/"))
                .Select(x => x.Replace("wwwroot", string.Empty))
                .Select(x =>
                {
                    var (artist, name) = FileNameToArtistAndSongName(x);

                    var tagList = new List<string>();
                    var songUrl = string.Empty;

                    if (info)
                    {
                        var metaTagRaw = new WebClient().DownloadString(
                            $"http://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key=9f72c879aaaf5089e635dc93b1fc013e&artist={artist}&track={name}&format=json");

                        var metaTagRawDynamic = JsonConvert.DeserializeObject<dynamic>(metaTagRaw);

                        if (metaTagRawDynamic["error"] == null)
                        {
                            var tags = metaTagRawDynamic?.track?.toptags?.tag;
                            tagList = tags.ToObject<List<string>>();

                            songUrl = metaTagRawDynamic?.track?.url.ToObject<string>();
                        }
                    }
                    
                    return new SongModel()
                    {
                        Artist = artist,
                        Name = name,
                        Path = x,
                        Tags = tagList,
                        Url = songUrl,
                        StreamUrl = url,
                        FileName = x.Substring(x.LastIndexOf("/") + 1)
                    };
                })
                .ToList();
        }
        
        /// <summary>
        /// Stream ripper factory
        /// </summary>
        public class Factory
        {
            public static void NewStream(string url)
            {
                var streamRadioRipper = new StreamRadioRipper(url);
            }
        }
    }
}

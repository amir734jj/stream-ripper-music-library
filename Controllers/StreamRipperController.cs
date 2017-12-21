using Microsoft.AspNetCore.Mvc;
using StreamRipperMusicLibrary.Services;
using StreamRipperMusicLibrary.Models;
using System;

namespace StreamRipperMusicLibrary.Controllers
{
    public class StreamRipperController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(StreamRadioRipper.Streams);
        }

        [HttpPost]
        public IActionResult Index([FromForm] StreamViewModel urlModel)
        {
            StreamRadioRipper.Factory.NewStream(urlModel?.Url);

            return Ok("Started streaming!");
        }

        [HttpPost]
        public IActionResult Stop([FromForm] StreamViewModel urlModel)
        {
            // cancel a task
            StreamRadioRipper.StopStream(urlModel?.Url);

            return Ok("Stop stream request received!");
        }

        [HttpPost]
        public IActionResult GetFiles([FromForm] StreamViewModel urlModel)
        {
            var files = StreamRadioRipper.DownloadedSongs(urlModel?.Url, urlModel.Info);
            return Ok(files);
        }

        [HttpPost]
        public IActionResult DeleteFile([FromForm] StreamViewModel urlModel)
        {
            try
            {
                StreamRadioRipper.DeleteSong(urlModel?.Url, urlModel?.FileName);
                return Ok("Deleted maybe!");
            } catch(Exception e)
            {
                return BadRequest(e?.Message);
            }
            
        }
    }
}
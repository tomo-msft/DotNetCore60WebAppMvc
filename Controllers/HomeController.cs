using SampleApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SampleApp.Controllers
{
    public class HomeController : Controller
    {
        private string LogHeader = ".NET Core 6.0 App [Application Insights SDK]";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.Title = LogHeader;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> HttpAccessTest(string? uri, string? size)
        {
            ViewBag.Title = "HttpAccessTest";
            var targetStr = uri ?? "http://www.bing.com";
            ViewBag.Result = await HttpAccessTestImpl(targetStr, size);
            return View("TestResult");
        }

        public IActionResult LogTest(string? msg)
        {
            ViewBag.Title = "LogTest";
            var msgStr = msg ?? "Sample Message";
            ViewBag.Result = LogTestImpl(msgStr);
            return View("TestResult");
        }

        public IActionResult ExceptionTest()
        {
            String str = null;
            var value = str.ToString();
            ViewBag.Title = "ExceptionTest";
            return View("TestResult");
        }

        public IActionResult LoadTest([FromQuery(Name = "loop")] int loop = 10 )
        {
            ViewBag.Title = "LoadTest";
            ViewBag.Result = LoadTestImpl(loop);
            return View("TestResult");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        protected async Task<Dictionary<string,string>> HttpAccessTestImpl(string url, string? sizeStr)
        {
            var current = DateTime.Now;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

            var size = 0;
            int.TryParse(sizeStr, out size);
            if (size > 0)
            {
                byte[] postData = new byte[size];
                System.Array.Fill<byte>(postData, 0x41);
                request.Method = "POST";
                request.ContentLength = postData.Length;
                request.ContentType = "application/text";
                System.IO.Stream reqStream = request.GetRequestStream();
                reqStream.Write(postData, 0, postData.Length);
                reqStream.Close();
            }
            HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                var body = await reader.ReadToEndAsync();
            }
            var header = resp.Headers.ToString();
            var code = resp.StatusCode.ToString();

            System.Diagnostics.Trace.TraceInformation($"{LogHeader} HttpAccessTest URL:{url}");
            System.Diagnostics.Trace.Flush();

            return new Dictionary<string, string>()
            {
                {"Now",current.ToString() },
                {"URL", url},
                {"Header",header},
                {"Code",code},
//                {"Last Modified",resp.LastModified.ToString()}
            };
        }

        protected Dictionary<string, string> LogTestImpl(string message)
        {
            var current = DateTime.Now;
            System.Diagnostics.Trace.TraceInformation($"{LogHeader} LogTest TraceInformation {message} {current}");
            System.Diagnostics.Trace.TraceWarning($"{LogHeader} LogTest TraceWarning {message} {current}");
            System.Diagnostics.Trace.TraceError($"{LogHeader} LogTest TraceError {message} {current}");
            System.Diagnostics.Trace.Flush();

            _logger.LogTrace($"{LogHeader} LogTest LogTrace {message} {current}");
            _logger.LogDebug($"{LogHeader} LogTest LogDebug {message} {current}");
            _logger.LogInformation($"{LogHeader} LogTest LogInformation {message} {current}");
            _logger.LogWarning($"{LogHeader} LogTest LogWarning {message} {current}");
            _logger.LogError($"{LogHeader} LogTest LogError {message} {current}");
            _logger.LogCritical($"{LogHeader} LogTest LogCritical {message} {current}");

            return new Dictionary<string, string>()
            {
                {"Now",current.ToString() },
                {"message", message}
            };
        }
        protected Dictionary<string, string> LoadTestImpl(int loop)
        {
            var start = DateTime.Now;
            int n = 0;
            for (int i = 0; i < loop; i++)
            {
                for (int j = 0; j < loop; j++)
                {
                    n += i;
                }
            }
            var end = DateTime.Now;
            var elapsed = end.Subtract(start);

            return new Dictionary<string, string>()
            {
                {"Loop",loop.ToString() },
                {"Elapsed", elapsed.ToString()}
            };
        }
    }
}

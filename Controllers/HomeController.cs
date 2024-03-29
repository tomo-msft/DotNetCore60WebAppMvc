﻿using SampleApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SampleApp.Controllers
{
    public class HomeController : Controller
    {
        private string LogHeader = ".NET Core 6.0 App [Application Insights Auto instrumentation]";

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

        public IActionResult MemoryTest([FromQuery(Name = "size")] int size = 1000, [FromQuery(Name = "sleep")] int sleep = 10)
        {
            ViewBag.Title = "MemoryTest";
            ViewBag.Result = MemoryTestImpl(size, sleep);
            return View("TestResult");
        }

        public IActionResult SQLTest()
        {
            ViewBag.Title = "SQLTest";
            ViewBag.Result = SQLTestImpl();
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
            _logger.LogError(new Exception("MyTestException"), $"{LogHeader} LogTest LogError with Exception {message} {current}");

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

        // size: MB, sleep: second
        protected Dictionary<string, string> MemoryTestImpl(int size, int sleep)
        {
            var start = DateTime.Now;
            var array = new Byte[size*1024*1024];
            for(int i=0; i<array.Length; i++)
            {
                array[i] = 0x01;
            }
            Thread.Sleep(sleep * 1000);

            var end = DateTime.Now;
            var elapsed = end.Subtract(start);

            return new Dictionary<string, string>()
            {
                {"Size",size.ToString() },
                {"Elapsed", elapsed.ToString()}
            };
        }

        protected Dictionary<string, string> SQLTestImpl()
        {
            var sql = "select top 3 * from [SalesLT].[Product]";
            var result = "";
            try
            {

                var constr = Environment.GetEnvironmentVariable("DB_CONN_STR");
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();           

                using (SqlConnection connection = new SqlConnection(constr))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _logger.LogInformation($"SQL Result: {reader.GetValue(0)}, {reader.GetValue(1)}, {reader.GetValue(2)}, {reader.GetValue(3)}");
                                result += $"{reader.GetValue(0)}, {reader.GetValue(1)}, {reader.GetValue(2)}, {reader.GetValue(3)} <br>";
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                _logger.LogError($"{LogHeader} LogTest LogError {e.ToString}", e);
            }

            return new Dictionary<string, string>()
            {
                {"Query", sql },
                {"Result", result }
            };
        }


    }
}

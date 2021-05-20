using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;

namespace HttpListenerExample
{
    internal static class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8080/";
        public static int requestCount;

        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <p>Page Views: {0}</p>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "  </body>" +
            "</html>";


        public static async Task HandleIncomingConnections()
        {
            var runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                var ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                var req = ctx.Request;
                var resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                string res;
                try
                {
                    res = HandleRequest(req);
                }
                catch(Exception e)
                {
                    res = e.Message;
                }
                // Write the response info
                var data = Encoding.UTF8.GetBytes(res);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            var listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }

        public static string HandleRequest(
            HttpListenerRequest request
        )
        {
            switch (request.HttpMethod)
            {
                case "POST":
                    return HandlePost(request);
                case "GET":
                    return HandleGet(request);
                case "DELETE":
                    return HandleDelete(request);
            }

            return "";
        }

        private static string HandleDelete(HttpListenerRequest request)
        {
            throw new NotImplementedException();
        }

        private static string HandleGet(HttpListenerRequest request)
        {
            var url = request.Url;

            if (url.AbsolutePath.Contains("show"))
            {
                var filename = url.AbsolutePath
                        .Split("/")
                        .WhereWithPrevious((prev, cur) => prev == "show")
                        .FirstOrDefault();
                filename = HttpUtility.UrlDecode(filename);

                return LoadHtmlFileContent(filename);
            }
            if (url.AbsolutePath == "/")
            {
                return GetAvailable(url.AbsoluteUri+"/show/");
            }

            throw new Exception("path does not match");
        }

        private static string LoadHtmlFileContent(string filename)
        {
            return File.ReadAllText(filename);
        }
        private static string HandlePost(HttpListenerRequest request)
        {
            throw new NotImplementedException();
        }

        private static string GetAvailable(string prefix)
        {
            var httpFiles = Directory.GetFiles(
                Directory.GetCurrentDirectory()
                ).Where(s => s.Contains(".html"))
                .ToList();

            var content = "<ul>";
            foreach (var file in httpFiles)
            {
                content += $"<li> <a href=\"{prefix+HttpUtility.UrlEncode(file)}\">{file}</a></li>";
            }

            content += "</ul>";
            return GetHTML(content);
        }

        private static string GetHTML(string htmlContent)
        {
            return "<!DOCTYPE>" +
                   "<html>" +
                   "  <head>" +
                   "    <title>HttpListener Example</title>" +
                   "  </head>" +
                   "  <body>" +
                   htmlContent +
                   "  </body>" +
                   "</html>";
        }

        public static IEnumerable<TSource> WhereWithPrevious<TSource>
        (
            this IEnumerable<TSource> source,
            Func<TSource, TSource, bool> predicate
            )
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    yield break;
                }
                TSource previous = iterator.Current;
                while (iterator.MoveNext())
                {
                    if (predicate(previous, iterator.Current))
                    {
                        yield return iterator.Current;
                    }
                    previous = iterator.Current;
                }
            }
        }
        public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>
        (this IEnumerable<TSource> source,
            Func<TSource, TSource, TResult> projection)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    yield break;
                }
                TSource previous = iterator.Current;
                while (iterator.MoveNext())
                {
                    yield return projection(previous, iterator.Current);
                    previous = iterator.Current;
                }
            }
        }
    }
}
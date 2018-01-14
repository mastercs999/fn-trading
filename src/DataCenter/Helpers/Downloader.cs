using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Helpers
{
    internal class Downloader
    {
        private static object Lock = new object();
    
        public delegate void OnProgress(double percent);

        public static Task<string> DownloadStringAsync(string url, OnProgress onProgressChanged)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            double previous = -1;
            using (WebClient wc = new WebClient())
            {
                if (onProgressChanged != null)
                    wc.DownloadProgressChanged += (s, e) =>
                        {
                            lock (Lock)
                            {
                                if (e.ProgressPercentage - previous > 0.3)
                                {
                                    previous = e.ProgressPercentage;

                                    onProgressChanged(e.ProgressPercentage);
                                }
                            }
                        };
                wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Error != null)
                        tcs.TrySetException(e.Error);
                    else if (e.Cancelled)
                        tcs.TrySetException(new Exception("Downloading was cancelled"));
                    else
                        tcs.TrySetResult(e.Result);
                }; 
                wc.DownloadStringAsync(new Uri(url));
            }

            return tcs.Task;
        }

        public static Task<object> DownloadFileAsync(string url, string file, OnProgress onProgressChanged, string cookie)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            double previous = -1;
            using (WebClient wc = new WebClient())
            {
                if (onProgressChanged != null)
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        lock (Lock)
                        {
                            if (e.ProgressPercentage - previous > 0.3)
                            {
                                previous = e.ProgressPercentage;

                                onProgressChanged(e.ProgressPercentage);
                            }
                        }
                    };
                wc.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                        tcs.TrySetException(e.Error);
                    else if (e.Cancelled)
                        tcs.TrySetException(new Exception("Downloading was cancelled"));
                    else
                        tcs.TrySetResult(new Object());
                };
                if (cookie != null)
                    wc.Headers.Add(HttpRequestHeader.Cookie, cookie);
                wc.DownloadFileAsync(new Uri(url), file);
            }

            return tcs.Task;
        }
    }
}

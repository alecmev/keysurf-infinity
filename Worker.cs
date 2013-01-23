using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;

namespace BrightCerulean.KeySurfInfinity
{
    public class Worker
    {
        private volatile bool _stop = false;
        private bool _success = false;

        public void Work()
        {
            while (!_stop)
            {
                try
                {
                    if (IsEthernetConnected())
                    {
                        var tmpRequest = WebRequest.Create("http://myfajoarco.lv/keysurf/ping/") as HttpWebRequest;
                        tmpRequest.Proxy = null;
                        ThreadPool.RegisterWaitForSingleObject(tmpRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), tmpRequest).AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), tmpRequest, 4000, true);
                    }
                    else
                    {
                        _success = false;
                        MainWindow.Instance.AppendStatusSafe("NO CABLE");
                    }
                }
                catch
                {
                    _success = false;
                    MainWindow.Instance.AppendStatusSafe("ERROR A");
                }

                Thread.Sleep(4000);

                if (_success)
                {
                    _success = false;
                    Thread.Sleep(56000);
                }
            }
        }

        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                if (!asynchronousResult.IsCompleted || asynchronousResult.AsyncState == null)
                    return;

                MainWindow.Instance.AppendStatusSafe("CHECKING KEYSURF");
                var tmpStream = (asynchronousResult.AsyncState as HttpWebRequest).EndGetResponse(asynchronousResult).GetResponseStream();
                var tmpResponse = new StreamReader(tmpStream).ReadToEnd();
                tmpStream.Close();

                if (tmpResponse != "OK")
                {
                    MainWindow.Instance.AppendStatusSafe("SUBMITTING LOGIN INFORMATION", false);
                    string tmpFormData = "username=" + System.Uri.EscapeUriString(MainWindow.Instance.Username) + "&password=" + System.Uri.EscapeUriString(MainWindow.Instance.Password) + "&original_url=%2Fwelcome.asp&login=Sign%20in";

                    var tmpRequest = WebRequest.Create("http://login.keycom.co.uk:8080/goform/HtmlLoginRequest") as HttpWebRequest;
                    tmpRequest.Method = "POST";
                    tmpRequest.Timeout = 4000;
                    tmpRequest.ContentType = "application/x-www-form-urlencoded";
                    tmpRequest.ContentLength = tmpFormData.Length;
                    tmpRequest.Headers.Add("Cache-Control", "max-age=0");
                    tmpRequest.Headers.Add("Origin", "http://login.keycom.co.uk:8080");
                    tmpRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1";
                    tmpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                    tmpRequest.Referer = "http://login.keycom.co.uk:8080/index.asp";
                    tmpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                    tmpRequest.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                    tmpRequest.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");

                    var tmpWriter = new StreamWriter(tmpRequest.GetRequestStream());
                    tmpWriter.Write(tmpFormData);
                    tmpWriter.Close();

                    _success = false;
                    MainWindow.Instance.AppendStatusSafe("SUCCESS", false);
                }
                else
                {
                    _success = true;
                    MainWindow.Instance.AppendStatusSafe("EVERYTHING IS FINE", false);
                }
            }
            catch
            {
                _success = false;
                MainWindow.Instance.AppendStatusSafe("FAIL", false);
            }
        }

        private void TimeoutCallback(object state, bool timedOut)
        {
            try
            {
                if (timedOut)
                {
                    var tmpRequest = state as HttpWebRequest;

                    if (tmpRequest != null)
                        tmpRequest.Abort();

                    _success = false;
                    MainWindow.Instance.AppendStatusSafe("NO INTERNET CONNECTION");
                }
            }
            catch
            {
                _success = false;
                MainWindow.Instance.AppendStatusSafe("ERROR B");
            }
        }

        public void Stop()
        {
            _stop = true;
        }

        private bool IsEthernetConnected()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface tmpNetworkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((tmpNetworkInterface.OperationalStatus != OperationalStatus.Up) ||
                    (tmpNetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (tmpNetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                if (tmpNetworkInterface.Speed <= 0)
                    continue;

                if ((tmpNetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet) &&
                    (tmpNetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet3Megabit) &&
                    (tmpNetworkInterface.NetworkInterfaceType != NetworkInterfaceType.FastEthernetFx) &&
                    (tmpNetworkInterface.NetworkInterfaceType != NetworkInterfaceType.FastEthernetT) &&
                    (tmpNetworkInterface.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet))
                    continue;

                if ((tmpNetworkInterface.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (tmpNetworkInterface.Description.IndexOf("hamachi", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (tmpNetworkInterface.Description.IndexOf("teamviewer", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (tmpNetworkInterface.Description.IndexOf("vpn", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (tmpNetworkInterface.Description.IndexOf("loopback", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                return true;
            }

            return false;
        }
    }
}

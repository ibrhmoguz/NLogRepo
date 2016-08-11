using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using NLog;

namespace LoggingApp.Infrastructure
{
    public class CaptureTraffic : IHttpModule
    {
        private StreamWatcher _watcher;
        public void Init(HttpApplication context)
        {
            context.BeginRequest += context_BeginRequest;
            context.EndRequest += context_EndRequest;
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null) return;

            //If the log level is debug, it logs request.
            var logger = LogManager.GetCurrentClassLogger();
            if (!logger.IsDebugEnabled) return;

            _watcher = new StreamWatcher(app.Response.Filter);
            app.Response.Filter = _watcher;

            // Add response header correlationId to match request and response.
            var correlationId = Guid.NewGuid().ToString();
            var correlationIdCollection = new NameValueCollection { { "ReqResCorrelationId", correlationId } };
            app.Response.Headers.Add(correlationIdCollection);

            var requestList = new Dictionary<string, string>
            {
                {"Method", app.Request.HttpMethod},
                {"Url", app.Request.Url.ToString()}
            };

            foreach (string key in app.Request.Headers.Keys)
            {
                requestList.Add(key, app.Request.Headers[key]);
            }

            foreach (string key in app.Request.Params.Keys)
            {
                if (!requestList.Any(p => p.Key.Equals(key)))
                    requestList.Add(key, app.Request.Params[key]);
            }

            foreach (string key in app.Request.ServerVariables.Keys)
            {
                if (!requestList.Any(p => p.Key.Equals(key)))
                    requestList.Add(key, app.Request.Params[key]);
            }

            byte[] bytes = app.Request.BinaryRead(app.Request.ContentLength);
            if (bytes.Count() > 0)
            {
                requestList.Add("Content", Encoding.ASCII.GetString(bytes));
            }
            app.Request.InputStream.Position = 0;

            // Log request info into database.
            GlobalDiagnosticsContext.Set("MessageType", "Request");
            GlobalDiagnosticsContext.Set("ReqResCorrelationId", correlationId);
            logger.Debug("Request_{0}  {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"), JsonConvert.SerializeObject(requestList));
        }

        void context_EndRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null) return;

            //If the log level is debug, it logs response.
            var logger = LogManager.GetCurrentClassLogger();
            if (!logger.IsDebugEnabled) return;

            var responseList = new Dictionary<string, string> { { "Status", app.Response.Status }, { "ContentEncoding", app.Response.ContentEncoding.ToString() } };

            foreach (string key in app.Response.Headers.Keys)
            {
                responseList.Add(key, app.Response.Headers[key]);
            }
            responseList.Add("Body", _watcher.ToString());

            // Log response info into database.
            GlobalDiagnosticsContext.Set("MessageType", "Response");
            logger.Debug("Response_{0}  {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"), JsonConvert.SerializeObject(responseList));
        }

        public void Dispose()
        {

        }
    }

    public class StreamWatcher : Stream
    {
        private Stream _base;
        private MemoryStream _memoryStream = new MemoryStream();

        public StreamWatcher(Stream stream)
        {
            _base = stream;
        }

        public override void Flush()
        {
            _base.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _base.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _memoryStream.Write(buffer, offset, count);
            _base.Write(buffer, offset, count);
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_memoryStream.ToArray());
        }

        #region Rest of the overrides
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
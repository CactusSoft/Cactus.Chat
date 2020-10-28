using log4net;

namespace Netcore.Simplest.Chat.Integration
{
    public class Log4NetTraceListener : System.Diagnostics.TraceListener
    {
        private readonly ILog log;

        public Log4NetTraceListener()
        {
            log = LogManager.GetLogger("System.Diagnostics", "");
        }

        public Log4NetTraceListener(ILog log)
        {
            this.log = log;
        }

        public override void Write(string message)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(message);
            }
        }

        public override void WriteLine(string message)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(message);
            }
        }

        public override void Fail(string message)
        {
            if (log.IsFatalEnabled)
            {
                log.Fatal(message);
            }
        }

        public override void Fail(string message, string detailMessage)
        {
            if (log.IsFatalEnabled)
            {
                log.Fatal(message);
            }
        }
    }
}

using System;

namespace SelfHostedJSONExample
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(int severity, string detail)
        {
            Severity = severity;
            Detail = detail;
        }

        public int Severity { get; set; }
        public string Detail { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskUniversum
{
    public class LogTapeFrame
    {
        public DateTime Time;
        public string Message;
        public string EventName;
        public string Scope;

        public override string ToString()
        {
            return string.Format("{0} [{1}]\t{2}:{3}", Time, EventName, Scope, Message);
        }
    }
    public class LogTapeFrameException : LogTapeFrame
    {
        public string ExceptionMessage;
        public string ExceptionStackTrace;
        public string failedOperationDescription;

        public override string ToString()
        {
            return string.Format("{0} [{1}]\t{2}:{3}\n\t\tEXCEPTION for '{6}':{4}, {5}\r\n", Time, EventName, Scope, Message, ExceptionMessage, ExceptionStackTrace, failedOperationDescription);
        }
    }
    public interface ILogTape
    {
        void RecordFrame(LogTapeFrame frame);
    }
    public interface ILogger
    {
        void Write(string format, params object[] args);

        void Info(string format, params object[] args);
        void Warning(string format, params object[] args);
        void Error(string format, params object[] args);

        void Debug(string format, params object[] args);
        void Exception(Exception ex, string failedOperationDescription, string format, params object[] args);
    }
    public interface IScopeLogger
    {
        string scopeSource { get; }
    }
}

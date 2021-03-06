﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Logging
{
    /// <summary>
    /// StringBuilderLog writes to shared StringBuffer.
    /// Made public so its testable
    /// </summary>
    public class StringBuilderLogFactory : ILogFactory
    {
        private readonly StringBuilder sb = new StringBuilder();

        private readonly bool debugEnabled;

        public StringBuilderLogFactory(bool debugEnabled = true)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new StringBuilderLog(type, sb) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string name)
        {
            return new StringBuilderLog(name, sb) { IsDebugEnabled = debugEnabled };
        }

        public string GetLogs()
        {
            lock (sb)
                return sb.ToString();
        }

        public void ClearLogs()
        {
            lock (sb)
                sb.Remove(0, sb.Length - 1);
        }
    }
}

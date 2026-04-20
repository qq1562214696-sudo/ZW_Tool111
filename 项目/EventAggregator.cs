using System;

namespace ZW_Tool.核心
{
    /// <summary>
    /// 轻量级事件聚合器，用于模块间解耦通信
    /// </summary>
    public static class EventAggregator
    {
        private static event EventHandler<LogEventArgs>? _logPublished;

        /// <summary>
        /// 日志发布事件（支持多播，线程安全）
        /// </summary>
        public static event EventHandler<LogEventArgs>? LogPublished
        {
            add
            {
                lock (typeof(EventAggregator))
                {
                    _logPublished += value;
                }
            }
            remove
            {
                lock (typeof(EventAggregator))
                {
                    _logPublished -= value;
                }
            }
        }

        /// <summary>
        /// 发布一条日志消息
        /// </summary>
        public static void PublishLog(string message, LogLevel level = LogLevel.Info)
        {
            _logPublished?.Invoke(null, new LogEventArgs(message, level));
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogLevel Level { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string message, LogLevel level)
        {
            Message = message;
            Level = level;
            Timestamp = DateTime.Now;
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}
﻿using System;
using Insights.Utilities;
using LogFileManager = Insights.Logging.InsightsLoggerLogFileManager;

namespace Insights.Logging
{
    /// <summary>
    /// Provides logging services for in-game events and telemetry.
    /// </summary>
    /// <typeparam name="T">Specifies the containing type of the logger instance.</typeparam>
    /// <remarks>
    /// This logger is used exclusively for logging in-game events and telemetry. Logging for
    /// technical concerns of the mod itself should be performed using InternalLogger.
    /// 
    /// Performance relies on the built-in buffering of the StreamWriter.
    /// 
    /// Thready safety is achieved through explicit locking code. The initial implementation
    /// used TextWriter.Synchronized() to create a thread-safe version of the StreamWriter,
    /// but the remaining buffer contents were never flushed on Dispose().
    /// </remarks>
    public class InsightsLogger<T>
    {
        private readonly string _loggerTypeName;

        public InsightsLogger()
        {
            // Configure the logging context.
            _loggerTypeName = GetLoggerTypeName();
        }

        /// <summary>
        /// Clears the log buffer and writes any pending data to storage.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The logger should not throw in order to avoid disrupting the game.")]
        public void Flush()
        {
            try
            {
                lock (LogFileManager.LogFileSyncRoot)
                {
                    LogFileManager.LogFileWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Log($"The logger encountered an error.", ex);
            }
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The logger should not throw in order to avoid disrupting the game.")]
        public void Log(string message)
        {
            try
            {
                // Get the current time and create a new log file if the rollover interval is reached.
                var timestamp = DateTime.UtcNow;
                var timestampMinute = timestamp.Truncate(TimeSpan.TicksPerMinute);

                // Use double-checked locking pattern. The safety of this approach for all situations
                // is debated, but it seems better than doing nothing. Open to improvement here.
                if (timestampMinute != LogFileManager.LogFileTime)
                {
                    lock (LogFileManager.LogFileSyncRoot)
                    {
                        if (timestampMinute != LogFileManager.LogFileTime)
                        {
                            LogFileManager.LogFileTime = timestampMinute;

                            LogFileManager.CreateLogFile(timestampMinute);
                        }
                    }
                }

                // TODO: Expand the message template to include module, method, context, and data.

                // Format and log the message.
                var messageText = $"{timestamp:O} {_loggerTypeName} {message}";

                lock (LogFileManager.LogFileSyncRoot)
                {
                    LogFileManager.LogFileWriter.WriteLine(messageText);
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Log($"The logger encountered an error.", ex);
            }
        }

        /// <summary>
        /// Gets the name of the type used to create the generic logger instance.
        /// </summary>
        /// <returns></returns>
        private string GetLoggerTypeName()
        {
            return GetType()
                .GetGenericArguments()[0]
                .Name;
        }
    }
}

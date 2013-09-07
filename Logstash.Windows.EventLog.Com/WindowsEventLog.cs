﻿// WindowsEventLog.cs
// compile with: /doc:XMLsample.xml
//-----------------------------------------------------------------------
// <copyright file="WindowsEventLog.cs" company="Naadir Jeewa">
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.    
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;

[assembly: CLSCompliant(true)]

namespace Logstash.Windows.EventLog.Com
{
    /// <summary>
    /// The interface for the Logstash event handler.
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch), Guid("248DB2EA-2DC6-4D10-8499-AA84ED61D141"), ComVisible(true)]
    public interface IEventWrittenInterface
    {
        /// <summary>
        /// A method on the Logstash side that must accept a JSON object.
        /// </summary>
        /// <param name="json">A Logstash v1 JSON Event</param>
        [DispId(1), ComVisible(true)]
        void EventWritten(string json);
    }

    /// <summary>
    /// The COM interface that is seen by LogStash.
    /// </summary>
    [Guid("7E2D8600-6983-4FCC-8A0F-A859F4970999"), ComVisible(true)]
    public interface IWindowsEventLog : IDisposable
    {
        /// <summary>
        /// Gets something not currently implemented.
        /// Sets a position if you need to resume consumption of logs after a reboot.
        /// </summary>
        [DispId(1), ComVisible(true)]
        string Bookmark
        {
            get;
        }

        /// <summary>
        /// Get a Windows Event object.
        /// </summary>
        /// <param name="logName">The name of the log, or its file path.</param>
        /// <param name="pathTypeName">Either 'LogName', or 'FilePath'</param>
        /// <param name="query">A filter to apply to the events before being sent.</param>
        [DispId(2), ComVisible(true)]
        void GetWindowsEventLog(string logName, string pathTypeName, string query);

        /// <summary>
        /// Same as above, but allows you to specify a bookmark to start from.
        /// </summary>
        /// <param name="logName">The name of the log, or its file path.</param>
        /// <param name="pathTypeName">Either 'LogName', or 'FilePath'</param>
        /// <param name="query">A filter to apply to the events before being sent.</param>
        /// <param name="bookmark">A bookmark XML string</param>
        [DispId(3), ComVisible(true)]
        void GetWindowsEventLogWithBookmark(string logName, string pathTypeName, string query, string bookmark);

        /// <summary>
        /// The standard method that sets up the event handler and enables it.
        /// </summary>
        [DispId(4), ComVisible(true)]
        void Run();

        /// <summary>
        /// Same as above, but we keep track of how many events are waiting to be sent
        /// to Logstash.
        /// </summary>
        [DispId(5), ComVisible(true)]
        void RunSpinWait();

        /// <summary>
        /// If there are no events, then .NET will after a time send LogStash to sleep.
        /// Once events are waiting to be sent, this methods exits and the caller should
        /// execute the OLE32 message loop.
        /// </summary>
        [DispId(6), ComVisible(true)]
        void SpinWaitLoop();

        /// <summary>
        /// Sets the default culture for the AppDomain as well as the current thread's.
        /// </summary>
        /// <param name="culture">The localization string to be used, such as en-GB</param>
        [DispId(7), ComVisible(true)]
        void SetCulture(string culture);
    }

    /// <summary>
    /// Provides a Windows Event Log API for Logstash using the post Vista ETW model.
    /// This allows the capturing of non-standard logs (i.e. not just Application, System
    /// and Security), as well as forwarded events captured from other computers.
    /// </summary>
    /// <remarks>
    /// There are two sets of methods. One is a standard COM interface, the other uses
    /// .NET 4 techniques for multiprocessing. This helps keep the CPU load down for
    /// LogStash by a significant amount (idle at less than 1%).
    /// </remarks>
    [ComSourceInterfaces(typeof(IEventWrittenInterface)), ClassInterface(ClassInterfaceType.None), Guid("6685C0E6-66F1-4F39-BD1A-182D2B005F37"), ProgId("Logstash.Windows.EventLog"), ComVisible(true)]  
    public class WindowsEventLog : IWindowsEventLog
    {
        /// <summary>
        /// The number of events waiting to be sent to Logstash.
        /// </summary>
        [ComVisible(false)]
        private static int queue = 0;

        /// <summary>
        /// The .NET interface for ETW logs.
        /// </summary>
        [ComVisible(false)]
        private EventLogWatcher watcher;

        /// <summary>
        /// Internal variable to help implement iDisposable.
        /// </summary>
        [ComVisible(false)]
        private bool disposed = false;

        /// <summary>
        /// Messages for events are only in one language for MS - en-US. Allow users to set their own culture.
        /// </summary>
        [ComVisible(false)]
        private CultureInfo logCulture;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsEventLog"/> class. Set the culture to en-US.
        /// </summary>
        public WindowsEventLog()
        {
            this.SetCulture("en-us");
        }

        /// <summary>
        /// Delegate which will hold the Ruby method.
        /// </summary>
        /// <param name="json">A Logstash v1 JSON Event</param>
        [ComVisible(true)]
        private delegate void EventWrittenTarget(string json);

        /// <summary>
        /// COM Proxy for the above.
        /// </summary>
        private event EventWrittenTarget EventWritten;

        /// <summary>
        /// Gets something not currently implemented.
        /// Sets a position if you need to resume consumption of logs after a reboot.
        /// </summary>
        [ComVisible(true)]
        public string Bookmark
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the default culture for the AppDomain as well as the current thread's.
        /// </summary>
        /// <param name="culture">The localization string to be used, such as en-GB</param>
        [ComVisible(true)]
        public void SetCulture(string culture)
        {
            this.logCulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentCulture = this.logCulture;
            Thread.CurrentThread.CurrentCulture = this.logCulture;
        }

        /// <summary>
        /// Get a Windows Event object.
        /// </summary>
        /// <param name="logName">The name of the log, or its file path.</param>
        /// <param name="pathTypeName">Either 'LogName', or 'FilePath'</param>
        /// <param name="query">A filter to apply to the events before being sent.</param>
        [ComVisible(true)]
        public void GetWindowsEventLog(string logName, string pathTypeName, string query)
        {
            this.GetWindowsEventLogWithBookmark(logName, pathTypeName, query, null);
        }

        /// <summary>
        /// Same as above, but allows you to specify a bookmark to start from.
        /// </summary>
        /// <param name="logName">The name of the log, or its file path.</param>
        /// <param name="pathTypeName">Either 'LogName', or 'FilePath'</param>
        /// <param name="query">A filter to apply to the events before being sent.</param>
        /// <param name="bookmark">A bookmark XML string</param>
        [ComVisible(true)]
        public void GetWindowsEventLogWithBookmark(string logName, string pathTypeName, string query, string bookmark)
        {
            PathType pathType;
            switch (pathTypeName)
            {
                case "LogName":
                    {
                        pathType = PathType.LogName;
                        break;
                    }

                case "FilePath":
                    {
                        pathType = PathType.FilePath;
                        break;
                    }

                default:
                    {
                        throw new ArgumentException("Incorrect path type. Specify 'LogName' or 'FilePath'", "pathTypeName");
                    }
            }

            EventLogQuery q = new EventLogQuery(logName, pathType, query);
            EventBookmark eventBookmark = this.ParseBookmarkString(bookmark) ?? null;
            if (bookmark == null)
            {
                this.watcher = new EventLogWatcher(q);
            }
            else
            {
                this.watcher = new EventLogWatcher(q, eventBookmark);
            }
        }

        /// <summary>
        /// The standard method that sets up the event handler and enables it.
        /// </summary>
        [ComVisible(true)]
        public void Run()
        {
            this.watcher.EventRecordWritten += (object s, EventRecordWrittenEventArgs e1) =>
            {
                EventWritten(SerializeEventRecord(e1.EventRecord));
            };
            this.watcher.Enabled = true;
        }

        /// <summary>
        /// Same as above, but we keep track of how many events are waiting to be sent
        /// to Logstash.
        /// </summary>
        [ComVisible(true)]
        public void RunSpinWait()
        {
            this.watcher.EventRecordWritten += (object s, EventRecordWrittenEventArgs e1) =>
            {
                queue++;
                EventWritten(SerializeEventRecord(e1.EventRecord));
                queue--;
            };
            this.watcher.Enabled = true;
        }

        /// <summary>
        /// If there are no events, then .NET will after a time send LogStash to sleep.
        /// Once events are waiting to be sent, this methods exits and the caller should
        /// execute the OLE32 message loop.
        /// </summary>
        [ComVisible(true)]
        public void SpinWaitLoop()
        {
            SpinWait spinWait = new SpinWait();
            while (queue < 1)
            {
                spinWait.SpinOnce();
            }
        }

        /// <summary>
        ///  Stuff to implement IDisposable. Not working just yet.
        /// </summary>
        [ComVisible(true)]
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Stuff to implement IDisposable. Not working just yet.
        /// </summary>
        /// <param name="disposing">Hmm. Something.</param>
        [ComVisible(false)]
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    this.watcher.Dispose();
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }

        /// <summary>
        /// Converts an event into a JSON event for ingesting by Logstash.
        /// </summary>
        /// <remarks>Have tried to copy the existing eventlog format.
        /// Some fields are new, some are gone.</remarks>
        /// <param name="e">An event record</param>
        /// <returns>A logstash JSON event version 1</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ComVisible(false)]
        private static string SerializeEventRecord(EventRecord e)
        {
            string serializedRecord = null;
            StringWriter sw = null;
            try
            {
                sw = new StringWriter(CultureInfo.InvariantCulture);
                JsonTextWriter writer = new JsonTextWriter(sw);
                writer.WriteStartObject();
                AddEventProperty(writer, "@version", 1);
                string normalisedDate = ((DateTime)e.TimeCreated).ToString("o", CultureInfo.InvariantCulture);
                AddEventProperty(writer, "@timestamp", normalisedDate);
                AddEventProperty(writer, "type", "eventlog");
                AddEventProperty(writer, "SourceName", e.ProviderName);
                AddEventProperty(writer, "EventIdentifier", e.Id);
                AddEventProperty(writer, "ComputerName", e.MachineName);
                AddEventProperty(writer, "TaskName", e.TaskDisplayName);
                try
                {
                    AddEventProperty(writer, "Message", e.FormatDescription());
                }
                catch (System.Diagnostics.Eventing.Reader.EventLogNotFoundException)
                {
                }

                AddEventProperty(writer, "host", Environment.MachineName);
                AddEventProperty(writer, "path", e.LogName);
                AddEventProperty(writer, "LogFile", e.LogName);
                AddEventProperty(writer, "SourceIdentifier", e.ProviderId);
                AddEventProperty(writer, "Type", System.Enum.GetName(typeof(StandardEventLevel), e.Level));
                AddEventProperty(writer, "EventType", e.Level);
                if (!IsNull(e.UserId))
                {
                    AddEventProperty(writer, "User", ResolveSIDtoUsername(e.UserId.Value));
                }

                AddEventProperty(writer, "RecordNumber", e.RecordId);
                AddEventProperty(writer, "ActivityIdentifier", e.ActivityId);
                AddEventProperty(writer, "RelatedActivityIdentifier", e.RelatedActivityId);
                AddEventProperty(writer, "Opcode", e.Opcode);
                AddEventProperty(writer, "OpcodeName", e.OpcodeDisplayName);
                AddEventProperty(writer, "pid", e.ProcessId);
                AddEventProperty(writer, "ThreadId", e.ThreadId);
                SerializeEventProperties(writer, e.Properties);
                AddEventProperty(writer, "Qualifiers", e.Qualifiers);
                AddEventProperty(writer, "Task", e.Task);
                AddEventProperty(writer, "EventVersion", e.Version);
                writer.WriteEndObject();
                serializedRecord = sw.ToString();
            }
            finally
            {
                sw.Dispose();
            }

            return serializedRecord;
        }

        /// <summary>
        /// Grab the nested data and convert to an array.
        /// </summary>
        /// <param name="writer">A JSON writer object</param>
        /// <param name="properties">The event properties of the event.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SerializeEventProperties(JsonWriter writer, System.Collections.Generic.IList<EventProperty> properties)
        {
            writer.WritePropertyName("InsertionStrings");
            writer.WriteStartArray();
            for (int i = 0; i < properties.Count; i++)
            {
                writer.WriteValue(properties[i].Value.ToString());
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Helper to check if objects are null.
        /// </summary>
        /// <param name="o">An object to check.</param>
        /// <returns>Is it null or not?</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNull(object o)
        {
            if (o == null)
            {
                return true;
            }

            switch (o.GetType().Name.ToUpperInvariant())
            {
                case "STRING":
                    {
                        return string.IsNullOrEmpty((string)o);
                    }

                default:
                    {
                        return true;
                    }
            }
        }

        /// <summary>
        /// A helper method to build a JSON event.
        /// </summary>
        /// <param name="writer">The JSON writer</param>
        /// <param name="key">A key</param>
        /// <param name="value">A value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddEventProperty(JsonTextWriter writer, string key, dynamic value)
        {
            if (!IsNull(value))
            {
                writer.WritePropertyName(key);
                writer.WriteValue(value);
            }
        }

        /// <summary>
        /// Resolves SIDs to usernames if at all possible.
        /// Uses an 'object cache' to prevent multiple calls to DCs.
        /// </summary>
        /// <param name="sid">The account's security identifier</param>
        /// <returns>A string of the user name</returns>
        [ComVisible(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ResolveSIDtoUsername(string sid)
        {
            if (!IsNull(sid))
            {
                if (Cache.GetData(sid) != null)
                {
                    return Cache.GetData(sid);
                }
                else
                {
                    try
                    {
                        return Cache.AddData(sid, new System.Security.Principal.SecurityIdentifier(sid).Translate(typeof(System.Security.Principal.NTAccount)).ToString());
                    }
                    catch (System.Security.Principal.IdentityNotMappedException)
                    {
                        return Cache.AddData(sid, sid);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Unimplemented method.
        /// </summary>
        /// <param name="bookmarkString">Unimplemented parameter.</param>
        /// <returns>Unimplemented return.</returns>
        [ComVisible(false)]
        private EventBookmark ParseBookmarkString(string bookmarkString)
        {
            return null;
        }
    }
}
// compile with: /doc:XMLsample.xml
//-----------------------------------------------------------------------
// <copyright file="IWindowsEventLog.cs" company="Naadir Jeewa">
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
namespace Logstash.Windows.EventLog.Com
{
    using System;
    using System.Runtime.InteropServices;

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
        /// Same as above, but we keep track of how many events are waiting to be sent
        /// to Logstash.
        /// </summary>
        [DispId(4), ComVisible(true)]
        void EnableHandlers();

        /// <summary>
        /// If there are no events, then .NET will after a time send LogStash to sleep.
        /// Once events are waiting to be sent, this methods exits and the caller should
        /// execute the OLE32 message loop.
        /// </summary>
        /// <returns>The number of messages to be picked up</returns>
        [DispId(5), ComVisible(true)]
        int YieldAndWaitForMessages();

        /// <summary>
        /// Sets the default culture for the AppDomain as well as the current thread's.
        /// </summary>
        /// <param name="culture">The localization string to be used, such as en-GB</param>
        [DispId(6), ComVisible(true)]
        void SetCulture(string culture);

        /// <summary>
        /// Lists all enabled logs on the local computer.
        /// </summary>
        /// <returns>String array of log names</returns>
        [DispId(7), ComVisible(true)]
        string[] GetAllLogNames();

        /// <summary>
        /// Dispose of this object
        /// </summary>
        [DispId(8), ComVisible(true)]
        void ComDispose();
    }
}

// compile with: /doc:XMLsample.xml
//-----------------------------------------------------------------------
// <copyright file="IEventWrittenInterface.cs" company="Naadir Jeewa">
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
}

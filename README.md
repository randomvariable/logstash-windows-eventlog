logstash-windows-eventlog
=========================

An input plugin for Logstash which supports the newer ETW logging format.

# NOTICE
This current implementation is a hack job of Ruby meets Java meets COM meets .NET. This is less than ideal. I'm intending to replace this with a pure .NET implementation of the LumberJack protocol. Your comments are appreciated  [here](https://github.com/randomvariable/logstash-windows-eventlog/issues/2) 


##To Install
###Requirements
a) .NET Framework 4.5
###Steps
1. Run `c:\windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild`
1. Get and install PowerShell GAC from http://powershellgac.codeplex.com/
1. Copy the DLLs to a useful place and register the binaries:
  1. Using Powershell GAC, run:
     1. `Add-GacAssembly -Path Logstash.Windows.EventLog.Com.dll`
	 1. `Add-GacAssembly -Path Newtonsoft.Json.dll` You'll need to get this from the packages directory.
  1. `c:\windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe "Logstash.Windows.EventLog.Com.dll"`
1. Copy the contents of logstash-windows-eventlog\plugin to Logstash's plugin directory.

Parameters
----------

* `log_files` - An array of logs to collect, all simply "all" to capture all enabled event logs.
   
   To get the name of an event log, use the following PowerShell command:
    
        Get-WinEvent -ListLog *
  
* `filter`    - A string representation of an xPath query that can filter events before being processed
              by Logstash.
* `log_type`  - What type of logs are being processed? A string of either 'LogName' or 'FilePath'.

   * `LogName` - The name of an event log.

   * `FilePath` - The filename of an evtx file.

Examples
--------

To collect all registered event logs, use the following config:

    input {
      windowseventlog {}
    }

To collect events from just Hyper-V, use a config like :

    input {
      windowseventlog {
      log_files  => ['Microsoft-Windows-Hyper-V-Hypervisor-Operational','Microsoft-Windows-Hyper-V-Config-Admin']
    }
  
Let's capture some IIS tracing logs (from AppFabric or some such):

    input {
    windowseventlog {
      log_files => ['C:\inetpub\logs\tracing\evt.evtx']
      log_type  => 'FilePath'
    }

Capture all logs, but filter for only a certain system event ID using an xPath query:
  
    input {
    windowseventlog {
      filter => "*[System[(EventID=2202)]]"
    }

Why a new event log plugin?
---------------------------
The author wanted to consolidate logs from Windows servers using Group Policy and WS-MAN. All these logs go into the
"Forwarded Events" section, which can't be ingested by Logstash. This plugin has been tested to capture at least 2000 events per second
on a quad-core Core i5, with CPU of around 1.2 cores.

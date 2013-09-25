require 'logstash/inputs/base'
require 'logstash/namespace'
require 'socket'

# Pull events from the Windows 2008/Vista+ Windows Event Log
#
# Use this to capture more than just the Application, System and Security
# logs. This plugin has been tested to capture at least 2000 events per second
# on a quad-core Core i5.
#
# Requires .NET Framework 4.5
#
#     input {
#
#      To collect all registered event logs, use the following config:
#       windowseventlog {}
#
#      To collect events from just Hyper-V, use a config like :
#       windowseventlog {
#         log_files  => ['Microsoft-Windows-Hyper-V-Hypervisor-Operational','Microsoft-Windows-Hyper-V-Config-Admin']
#       }
#
#     In addition, let's capture some IIS tracing logs as well:
#        windowseventlog {
#          log_files => ['C:\inetpub\logs\tracing\evt.evtx']
#          log_type  => 'FilePath'
#        }
#
#     Capture all logs, but filter for only a certain system event ID
#     Uses xPath
#
#       windowseventlog {
#         filter => "*[System[(EventID=2202)]]"
#     }

class  LogStash::Inputs::WindowsEventLog < LogStash::Inputs::Base
  class Interrupted < StandardError; end
  config_name 'windowseventlog'
  milestone 1

  default :codec, 'json'

  # Array
  #config :log_files,  :validate => [:array,'all'],         :default => 'all'
  config :log_files,  :default => ['all']
  config :filter,     :validate => :string,                :default => '*'
  config :log_type,   :validate => ['LogName','FilePath'], :default => 'LogName'

  @@com_provider = 'Logstash.Windows.EventLog'
  @olearray = Array.new
  
  public
  def register
  @olearray ||= Array.new
  if RUBY_PLATFORM == "java"
    require "logstash/inputs/eventlog/racob_fix"
    require "jruby-win32ole"
  else
    require "win32ole"
  end   
    @hostname = Socket.gethostname
    if (@log_files == ['all'])
        @logger.info("Registering input windowseventlog://#{@hostname}/. Will enumerate all logs.")
    else
        @logger.info("Registering input windowseventlog://#{@hostname}/#{@log_files}")    
    end # if
  end # def register

  
  public
  def run(output_queue)
    @logger.debug("Creating top level OLE object to execute static methods")
	  eventing = WIN32OLE.new(@@com_provider)
    if(@log_files == 'all') 
      @log_files = eventing.GetAllLogNames()
    end # if   
    @log_files.each do |log_name|
        @olearray << run_log(output_queue,log_name,@log_type,@filter)
    end # @log_files.each
    @dispose ||= false
    loop do
      eventing.YieldAndWaitForMessages()
      WIN32OLE_EVENT.message_loop
    end # loop
  end # run(output_queue)

  def run_log(output_queue,log_name,log_type,filter)
      event_interface = WIN32OLE.new(@@com_provider)
      event_interface.GetWindowsEventLog(log_name,log_type,filter)
      event_pub = WIN32OLE_EVENT.new(event_interface)
      event_pub.on_event('EventWritten') do |*args|
        codec.decode("#{args[0]}") do |event|
          decorate(event)
          output_queue << event
        end # codec.decode
      end # on_event
    @logger.debug("Initialise registered event handlers for #{log_name}") 
    begin  
      event_interface.EnableHandlers()
    rescue
      @logger.warn( "Failed to create new OLE instance for #{log_name} of type #{log_type} using filter #{filter}")
    end # rescue
    return event_interface
  end #def run_log


  public
  def teardown
      @logger.warn("Shutting down OLE event handlers")
      unless @olearray.nil?
        @olearray.each do |ole|
          ole.ComDispose()
        end
      end
  end # def teardown

end # class LogStash::Inputs::WindowsEventLog


require 'logstash/inputs/base'
require 'logstash/namespace'
require 'win32ole'
require 'socket'

class  LogStash::Inputs::WindowsEventLog < LogStash::Inputs::Base
  class Interrupted < StandardError; end
  config_name 'windowseventlog'
  milestone 2

  default :codec, 'json'

  # log
  config :logfile, :validate => :array, :default => ['Application', 'Security', 'System']
  config :filter, :validate => :string, :default => '*'
  config :log_type, :validate => ['LogName','FilePath'], :default => 'LogName'



  public
  def register
    @hostname = Socket.gethostname
    @logger.warn("Registering input windowseventlog://#{@hostname}/#{@logfile}")
  end # def register

  public
  def run(output_queue)
     @logfile.each   {
       |log_name|
       @logger.warn("Starting #{log_name}")
       run_log(output_queue,log_name,@log_type,@filter)
	   event_waiter = WIN32OLE.new('Logstash.Windows.EventLog')
	 	        loop {
		event_waiter.SpinWaitLoop()
        WIN32OLE_EVENT.message_loop
      }	  
     }

  end # run(output_queue)

  def run_log(output_queue,log_name,log_type,filter)
  @logger.warn( "setting up shit for #{log_name}")
      event_interface = WIN32OLE.new('Logstash.Windows.EventLog')
      event_interface.GetWindowsEventLog(log_name,log_type,filter)
      event_pub = WIN32OLE_EVENT.new(event_interface)
      event_pub.on_event('EventWritten') {
          |*args|
         
        codec.decode("#{args[0]}") do |event|
          decorate(event)
          output_queue << event
        end

      }
      event_interface.RunSpinWait()

    end


  public
  def teardown

  end # def teardown

end # class LogStash::Inputs::WindowsEventLog


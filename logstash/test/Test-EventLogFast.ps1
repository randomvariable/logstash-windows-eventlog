while($true)
{
    $i = 0
    $nextTime = [DateTime]::Now + (new-timespan -Seconds 1)    
    while([DateTime]::Now -le $nextTime)
    {
       write-eventlog -LogName Application -Source "Test Script" -EntryType Information -EventId 1 -Message "A test message" 
        $i++
    }
    write-host $i events per second
}

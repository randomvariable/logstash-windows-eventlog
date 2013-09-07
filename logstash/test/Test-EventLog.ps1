[array] $Vowels = "a;a;a;a;e;e;e;e;i;i;i;o;o;o;u;u;y" -split ";"
[array] $Consonants = "b;b;br;c;c;c;ch;cr;d;f;g;h;j;k;l;m;m;m;n;n;p;p;ph;qu;r;r;r;s;s;s;sh;t;tr;v;w;x;z" -split ";"
[array] $Endings = "r;r;s;r;l;n;n;n;c;c;t;p" -split ";"
 
function Get-RandomVowel 
{ return $Vowels[(Get-Random($Vowels.Length))] }
 
function Get-RandomConsonant
{ return $Consonants[(Get-Random($Consonants.Length))] }
 
function Get-RandomEnding
{ return $Endings[(Get-Random($Endings.Length))] }
 
function Get-RandomSyllable ([int32] $PercentConsonants, [int32] $PercentEndings)
{  
   [string] $Syllable = ""
   if ((Get-Random(100)) -le $PercentConsonants) 
   { $Syllable+= Get-RandomConsonant }
   $Syllable+= Get-RandomVowel
   if ((Get-Random(100)) -le $PercentEndings) 
   { $Syllable+= Get-RandomEnding }
   return $Syllable
}
 
function Get-RandomWord ([int32] $MinSyllables, [int32] $MaxSyllables)
{  
   [string] $Word = ""
   [int32] $Syllables = ($MinSyllables) + (Get-Random(($MaxSyllables - $MinSyllables + 1)))
   for ([int32] $Count=1; $Count -le $Syllables; $Count++) 
   { $Word += Get-RandomSyllable 70 20 } <# Consonant 70% of the time, Ending 20% #>
   return $Word
}
 
function Get-RandomSentence ([int32] $MinWords, [int32] $MaxWords) 
{  
   [string] $Sentence = ""
   [int32] $Words = ($MinWords) + (Get-Random($MaxWords - $MinWords + 1))
   for ([int32] $Count=1; $Count -le $Words; $Count++) 
   { 
      $Sentence += Get-RandomWord 1 5 <# Word with 1 to 5 syllables #>
      $Sentence += " "
   }
   $Sentence = $Sentence.substring(0,1).ToUpper() + $Sentence.substring(1,$Sentence.Length-2) + "."
   return $Sentence
}

Add-Type -TypeDefinition $textGen  -ErrorAction SilentlyContinue

$rnd = New-Object System.Random
$logs = @("Application","System","Security")
$sources = @("Test1","Test2","Test3","Test4","Test5")

foreach($log in $logs)
{
    foreach($source in $sources)
    {
        New-EventLog -LogName $log -Source ($log + $source) -ErrorAction SilentlyContinue
    }
}


$EntryTypes = @("Error", "FailureAudit", "Information", "SuccessAudit", "Warning")
do
{
    $content = Get-RandomSentence 10 20
    $source = $sources[$rnd.Next(0,$sources.Count -1)]
    $EntryType = $EntryTypes[$rnd.Next(0,$EntryTypes.Count -1)]
    $log = $logs[$rnd.Next(0,$logs.Count -1)]
    write-eventlog -LogName $log -Source ($log + $source) -EntryType $EntryType -EventId ($rnd.Next(1,1000)) -Message $content
    #sleep ($rnd.Next(1,5))
}
while($true)

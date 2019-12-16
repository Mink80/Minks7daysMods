Write-Host "7dtd Watchdog v2"
Write-Host "----------------`n"

# Configuration
$gamepath = 'C:\7dtd'
$php = 'C:\php7\php.exe'

# see https://github.com/Mink80/PHP-Source-Query
$7days_checker = 'C:\7dtd-tools\PHP-Source-Query\Examples\7days_checker.php'

$logpath = "${gamepath}" + "\" + "logs"
# ///

Write-Host "Configuration"
Write-Host "Game path     : ${gamepath}"
Write-Host "Log Path      : ${logpath}"
Write-Host "PHP binary    : ${php}"
Write-Host "Checker script: ${7days_checker} `n"

function CheckConfig
{
    if ( !(Test-Path $gamepath))
    {
        Write-Host "Gamepath does not exist!"
        return 1
    }
    if ( !(Test-path -Path $php -PathType Leaf))
    {
        Write-Host "PHP binary does not exist!"
        return 2
    }
    if ( !(Test-Path -Path $7days_checker -PathType Leaf))
    {
        Write-Host "Checker script does not exist!"
        return 3
    }

    if ( !(Test-Path -Path $logpath))
    {
        Write-Host "Log Directory not existent. Creating."
        New-Item -Path $logpath -ItemType Directory
        return 0
    }

    return 0
}

If (CheckConfig -gt 0)
{
    Pause
    Exit
}

Set-Location $gamepath

function GetDateString
{
    return Get-Date -UFormat "%Y-%m-%d--%H-%M"
}

function StartServer
{
    $startdate = GetDateString

    & "$gamepath\7DaysToDieServer.exe" -logfile "$gamepath\logs\$startdate.log" -quit -batchmode -nographics -configfile="$gamepath\serverconfig.xml" -dedicated
    Write-Host "${startdate}: Starting Server."

    # give the server some time to start up
    Start-Sleep -s 180
}

function CheckServer
{
    if ( (&$php $7days_checker).count -ge 5 )
    {
        return $true
    }
    return $false
}

function Get7DaysProcess
{
    return Get-WmiObject -Class Win32_Process -Filter "name='7DaysToDieServer.exe'"
}


while ($true)
{
    # check for not responding 7days server process and kill if not responding
    if (CheckServer) 
    { 
        $datestring = GetDateString
        Write-Host "${datestring}: all ok"
        # server is running. do nothing.
    } 
    else 
    { 
        # get 7days server process
        $process = Get7DaysProcess

        # found process: process is not functional.
        if ($process)
        {
            # may the server is in shutdown process. dont kill it in this state. wait some time, check it again
            $datestring = GetDateString
            Write-Host "${datestring}: Detected nonfunctional server process. Waiting to check again..."
            Start-Sleep -s 30

            # get 7days server process again
            $process = Get7DaysProcess
            if ($process) 
            {
                #if (CheckServer -eq $false)
                if (!(CheckServer))
                {
                    $process.terminate()
                    $datestring = GetDateString
                    Write-Host "${datestring}: Killed non functional server process."
                    Start-Sleep -s 5
                    
                    StartServer
                }
                else 
                {
                    # process functional again. do nothing.   
                }
            }
            else 
            {
                StartServer
            }
        }
        else 
        {
            StartServer
        }
    }

    # wait 3m to start over
    Start-Sleep -s 180
}

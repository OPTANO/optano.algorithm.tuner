Param(
  [string]$sourcepath ="",
  [string]$targetpath ="",
  [string]$targetserver ="",
  [string]$targetuser = "",
  [string]$targetpassword = "",
  [string]$targetkey = ""
)

# Load WinSCP .NET assembly (download from https://winscp.net/eng/download.php) 
Add-Type -Path ".\optano.algorithm.tuner.internal\winscp-automation\WinSCPnet.dll"

# Setup session options
$sessionOptions = New-Object WinSCP.SessionOptions -Property @{
    Protocol = [WinSCP.Protocol]::Sftp
    HostName = $targetserver
    UserName = $targetuser
    Password =  $targetpassword
    SshHostKeyFingerprint = $targetkey
}

$session = New-Object WinSCP.Session

try
{
    # Connect
    $session.Open($sessionOptions)

    # Upload
    $session.PutFiles($sourcepath, $targetpath).Check()
}
finally
{
    # Disconnect, clean up
    $session.Dispose()
}
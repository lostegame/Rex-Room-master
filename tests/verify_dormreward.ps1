$ErrorActionPreference = 'Continue'
$base = 'https://127.0.0.1:5229'
$src  = 'C:\Users\LOSTE GAME\Desktop\Rex Room master'
$tok  = (Get-Content "$src\Player\3\account.json" -Raw | ConvertFrom-Json).Token

function Balance {
  $r = & curl.exe -s -k "$base/api/storefronts/v4/balance/2" -H "Authorization: Bearer $tok" 2>&1 | Out-String
  if ($r -match '"Balance":(\d+)') { return [int]$Matches[1] }
  return -1
}

function Pending($label) {
  $r = & curl.exe -s -k "$base/api/gamerewards/v1/pending" -H "Authorization: Bearer $tok" 2>&1 | Out-String
  $r = ($r -replace '\s+',' ').Trim()
  if ($r.Length -gt 420) { $r = $r.Substring(0,420) + ' ...' }
  "[$label] $r"
}

# Drain anything left over from earlier tests so the counts below are clean.
Pending 'drain leftovers'
$b0 = Balance
Write-Output "balance before entering the Dorm = $b0"
Write-Output ''

# POST /matchmake/room/3070 with a fresh LoginLock = a brand new Dorm entry.
$lock = "dormtest-$([guid]::NewGuid().ToString('N').Substring(0,12))"
$out = & curl.exe -s -k -o - -X POST "$base/matchmake/room/3070?LoginLock=$lock" `
       -H "Authorization: Bearer $tok" --data-raw "LoginLock=$lock&JoinMode=0" 2>&1 | Out-String
if ($out -match '"subRoomId":(\d+)') { "matchmake ok, subRoomId=$($Matches[1])" } else { "matchmake response unexpected" }

Write-Output ''
$b1 = Balance
Write-Output "balance immediately after entering the Dorm = $b1   <- MUST still be $b0 (no direct deposit)"
Write-Output ''
Write-Output 'the 100 tokens should be sitting in a gift box:'
Pending 'pending'

Write-Output ''
$b2 = Balance
Write-Output "balance after collecting the box = $b2   <- should be $($b0 + 100)"
Pending 'pending (drained)'

Write-Output ''
Write-Output '--- re-entering the Dorm with the SAME LoginLock must not pay again ---'
& curl.exe -s -k -o /dev/null -X POST "$base/matchmake/room/3070?LoginLock=$lock" `
   -H "Authorization: Bearer $tok" --data-raw "LoginLock=$lock&JoinMode=0" 2>&1 | Out-Null
Pending 'pending after re-entry'
$b3 = Balance
Write-Output "balance = $b3   <- unchanged means no second payout"

Write-Output ''
Write-Output '--- entering a NON-dorm room (2 = RecCenter) must pay nothing ---'
$lock2 = "roomtest-$([guid]::NewGuid().ToString('N').Substring(0,12))"
& curl.exe -s -k -o /dev/null -X POST "$base/matchmake/room/2?LoginLock=$lock2" `
   -H "Authorization: Bearer $tok" --data-raw "LoginLock=$lock2&JoinMode=0" 2>&1 | Out-Null
Pending 'pending after RecCenter entry'
$b4 = Balance
Write-Output "balance = $b4   <- unchanged means RecCenter paid nothing"

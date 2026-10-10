param(
  # Reference: apps/auth/src/auth.app.ts placeNewPlayerInOrientation()
  #   ORIENTATION_ROOM_ID = 13, ORIENTATION_INSTANCE_ID = -2, photonRoomId "rec.13"
  # and match.app.ts /player/logout, which must NOT clear that seed.
  [switch]$KeepAccounts
)
$ErrorActionPreference = 'Continue'
$base = 'https://127.0.0.1:5229'
$src  = 'C:\Users\LOSTE GAME\Desktop\Rex Room master'

function NewAccount([string]$tag) {
  # Unique per run: /account/create rejects a duplicate username, so a fixed tag made
  # every run after the first fail to get a token.
  $u = "orient_${tag}_$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
  $r = & curl.exe -s -k "$base/account/create" -X POST `
        --data-raw "username=$u&password=test1234&platformId=orient_$tag&displayName=$u&birthday=2000-01-01" 2>&1 | Out-String
  # Extract with explicit matches: two -match calls in sequence would let the second
  # overwrite $Matches, which handed back the accountId as the token and made every
  # request fall through to the 2056 fallback.
  $tokM = [regex]::Match($r, '"token":"([^"]+)"')
  $idM  = [regex]::Match($r, '"accountId":(\d+)')
  if ($tokM.Success -and $idM.Success) {
    return @{ Token = $tokM.Groups[1].Value; Id = [int]$idM.Groups[1].Value }
  }
  return $null
}

function Heartbeat([string]$tok) {
  $r = & curl.exe -s -k "$base/player/heartbeat" -X POST -H "Authorization: Bearer $tok" 2>&1 | Out-String
  return ($r -replace '\s+', ' ').Trim()
}

function Field([string]$json, [string]$name) {
  # Booleans are bare literals in this JSON ("isPrivate":false), not quoted, so they
  # have to be part of the pattern or the field reads as absent.
  $m = [regex]::Match($json, '"' + [regex]::Escape($name) + '":(-?\d+|"[^"]*"|true|false|null)')
  if ($m.Success) { return $m.Groups[1].Value.Trim('"') }
  return '<absent>'
}

$pass = 0; $fail = 0
function Check([string]$label, [bool]$ok, [string]$detail = '') {
  if ($ok) { $script:pass++; Write-Output "  PASS  $label $detail" }
  else     { $script:fail++; Write-Output "  FAIL  $label $detail" }
}

Write-Output '=== 1. a BRAND-NEW account must be seeded into Orientation ==='
$a = NewAccount 'alpha'
if (-not $a) { Write-Output '  FAIL  could not create the test account'; exit 1 }
Write-Output "  created account $($a.Id)"

$hb = Heartbeat $a.Token
Write-Output "  heartbeat: $($hb.Substring(0, [Math]::Min(230, $hb.Length)))"
Check 'heartbeat echoes instance -2'      ((Field $hb 'roomInstanceId') -eq '-2')        "got $(Field $hb 'roomInstanceId')"
Check 'roomId is 13'                     ((Field $hb 'roomId') -eq '13')               "got $(Field $hb 'roomId')"
Check 'subRoomId is 23'                  ((Field $hb 'subRoomId') -eq '23')            "got $(Field $hb 'subRoomId')"
Check 'roomInstanceType is 0 (Public)'   ((Field $hb 'roomInstanceType') -eq '0')       "got $(Field $hb 'roomInstanceType')"
Check 'isPrivate is false'               ((Field $hb 'isPrivate') -eq 'false')          "got $(Field $hb 'isPrivate')"
Check 'photonRoomId is rec.13'           ((Field $hb 'photonRoomId') -eq 'rec.13')      "got $(Field $hb 'photonRoomId')"
Check 'name is ^Orientation'             ((Field $hb 'name') -eq '^Orientation')       "got $(Field $hb 'name')"
Check 'location is the real scene id'    ((Field $hb 'location') -eq 'c79709d8-a31b-48aa-9eb8-cc31ba9505e8') "got $(Field $hb 'location')"
Check 'maxCapacity is 1'                 ((Field $hb 'maxCapacity') -eq '1')            "got $(Field $hb 'maxCapacity')"

Write-Output ''
Write-Output '=== 2. the SPURIOUS logout right after creation must NOT wipe the seed ==='
& curl.exe -s -k "$base/player/logout" -X POST -H "Authorization: Bearer $($a.Token)" 2>&1 | Out-Null
$hb2 = Heartbeat $a.Token
Check 'seed survived logout'             ((Field $hb2 'roomInstanceId') -eq '-2')       "got $(Field $hb2 'roomInstanceId')"

Write-Output ''
Write-Output '=== 3. a real /matchmake/dorm must REPLACE the Orientation seed ==='
$mm = (& curl.exe -s -k "$base/matchmake/dorm" -X POST -H "Authorization: Bearer $($a.Token)" 2>&1 | Out-String) -replace '\s+', ' '
$inst = [regex]::Match($mm, '"roomInstanceId":(-?\d+)').Groups[1].Value
# "$1016400 ..." inside a string reads $1016400 as a variable name, not a number,
# which silently reduced the expectation to ($id * 1000).
$expectedDorm = 1016400 + ($a.Id * 1000)
Check 'dorm replaced the seed'           ($inst -ne '-2' -and $inst -ne '')            "got '$inst'"
Check 'dorm instance is per-account'     ($inst -eq [string]$expectedDorm)            "expected $expectedDorm"
$hb3 = Heartbeat $a.Token
Check 'heartbeat now echoes the dorm'    ((Field $hb3 'roomInstanceId') -eq $inst)     "got $(Field $hb3 'roomInstanceId')"

Write-Output ''
Write-Output '=== 4. logout NOW clears normally (presence is a real instance) ==='
& curl.exe -s -k "$base/player/logout" -X POST -H "Authorization: Bearer $($a.Token)" 2>&1 | Out-Null
$hb4 = Heartbeat $a.Token
# A cleared assignment is advertised as "roomInstance":null, so roomInstanceId is not
# merely absent - the whole room instance must be gone.
Check 'assignment cleared'               ($hb4 -match '"roomInstance":\s*null')        "got $(Field $hb4 'roomInstanceId')"

Write-Output ''
Write-Output '=== 5. an EXISTING account must NOT be seeded into Orientation ==='
$old = Get-Content "$src\Player\1\account.json" -Raw | ConvertFrom-Json
$hbOld = Heartbeat $old.Token
Check 'account 1 not in Orientation'     ((Field $hbOld 'roomInstanceId') -ne '-2')      "got $(Field $hbOld 'roomInstanceId')"

Write-Output ''
Write-Output "=== $pass passed / $fail failed ==="
if (-not $KeepAccounts) {
  Write-Output "  (test accounts $($a.Id) left in place - delete Player\$($a.Id) if unwanted)"
}
exit $fail
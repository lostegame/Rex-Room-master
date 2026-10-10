# Verifies per-account dorm isolation: two accounts must NOT share roomInstanceId
# or photonRoomId, and each must get its own name. Also checks the friend seed
# is gone (no relationships file => no friends).
$store = 'C:\Users\LOSTE GAME\Desktop\Rex Room master\bin\Release\net10.0\Storage'

function GetTok($id) {
  $p = "$store\$id\account.json"
  if (-not (Test-Path $p)) { return $null }
  return (Get-Content $p -Raw | ConvertFrom-Json).Token
}

function Dorm($tok) {
  $r = & curl.exe -s -k "https://127.0.0.1:5229/matchmake/dorm" `
      -X POST -H "Authorization: Bearer $tok" 2>&1 | Out-String
  return $r.Trim()
}

$t1 = GetTok 1
$t2 = GetTok 2

if (-not $t1 -or -not $t2) { "MISSING account token (1=$([bool]$t1) 2=$([bool]$t2))"; exit 1 }

Write-Output '=== POST /matchmake/dorm as account 1 ==='
$d1 = Dorm $t1
Write-Output "  $d1"
Write-Output ''
Write-Output '=== POST /matchmake/dorm as account 2 ==='
$d2 = Dorm $t2
Write-Output "  $d2"
Write-Output ''

$j1 = $d1 | ConvertFrom-Json
$j2 = $d2 | ConvertFrom-Json
$r1 = $j1.roomInstance
$r2 = $j2.roomInstance

Write-Output '=== isolation verdict ==='
"  account1 instance = $($r1.roomInstanceId)  photon = $($r1.photonRoomId)  name = $($r1.name)"
"  account2 instance = $($r2.roomInstanceId)  photon = $($r2.photonRoomId)  name = $($r2.name)"
if ($r1.roomInstanceId -eq $r2.roomInstanceId) { "  FAIL: shared roomInstanceId" } else { "  PASS: distinct roomInstanceId" }
if ($r1.photonRoomId   -eq $r2.photonRoomId)   { "  FAIL: shared photonRoomId" }   else { "  PASS: distinct photonRoomId" }
if ($r1.name -eq $r2.name) { "  FAIL: shared dorm name" } else { "  PASS: distinct dorm names" }

# stability: same account must get the SAME ids again
$d1b = (Dorm $t1) | ConvertFrom-Json
Write-Output ''
Write-Output '=== stability (account 1 re-asked) ==='
"  instance = $($d1b.roomInstance.roomInstanceId)  photon = $($d1b.roomInstance.photonRoomId)"
if ($d1b.roomInstance.roomInstanceId -eq $r1.roomInstanceId) { "  PASS: instanceId stable" } else { "  FAIL: instanceId changed mid-session" }
if ($d1b.roomInstance.photonRoomId -eq $r1.photonRoomId) { "  PASS: photonRoomId stable" } else { "  FAIL: photonRoomId changed mid-session" }

Write-Output ''
Write-Output '=== friends (must be empty until a real request+accept) ==='
foreach ($id in 1,2) {
  $tk = GetTok $id
  $f = (& curl.exe -s -k "https://127.0.0.1:5229/api/relationships/v2/get" -H "Authorization: Bearer $tk" 2>&1 | Out-String).Trim()
  "  account $id relationships = $f"
}
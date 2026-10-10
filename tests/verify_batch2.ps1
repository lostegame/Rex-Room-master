$ErrorActionPreference = 'Continue'
$base = 'https://127.0.0.1:5229'
$src  = 'C:\Users\LOSTE GAME\Desktop\Rex Room master'
$tok  = (Get-Content "$src\Player\3\account.json" -Raw | ConvertFrom-Json).Token

function Req($method, $path, $body, $label) {
  $args = @('-s','-k','-o','-','-w','`n[HTTP %{http_code}]','-X',$method,"$base$path",'-H',"Authorization: Bearer $tok")
  if ($null -ne $body) { $args += @('--data-raw', $body) }
  $out = & curl.exe @args 2>&1 | Out-String
  $out = $out -replace '\s+',' '
  if ($out.Length -gt 340) { $out = $out.Substring(0,340) + ' ...' }
  "[$label] $out"
}

Write-Output '=== bio / profileimage (form-urlencoded, no Content-Type) ==='
Req 'PUT' '/account/me/bio' 'bio=fffff' 'PUT bio'
Req 'GET' '/account/3/bio' $null 'GET bio'
Req 'PUT' '/account/me/profileimage' 'imageName=sharecamera%2F2026-10-01%2Ftestpic.jpg' 'PUT profileimage'
Req 'GET' '/account/me' $null 'GET me'

Write-Output ''
Write-Output '=== sanitize / chat ==='
Req 'POST' '/api/sanitize/v1' '{"Value":"hello <world>","ReplacementChar":42,"Context":"RoomChat","Intent":1,"ruleset":0}' 'sanitize object'
Req 'POST' '/api/sanitize/v1' '"plain string"' 'sanitize jsonstring'

Write-Output ''
Write-Output '=== dorm name + collection shapes ==='
Req 'GET' '/roomserver/rooms/3070' $null 'roomserver room 3070'
Req 'GET' '/rooms/ownedby/3' $null 'rooms ownedby 3'
Req 'GET' '/roomserver/rooms/ownedby/3' $null 'roomserver ownedby 3'
Req 'GET' '/rooms/bulk?name=RecCenter' $null 'rooms bulk by name'
Req 'GET' '/rooms/bulk?ids=2,3' $null 'rooms bulk by ids'
Req 'GET' '/rooms/2/similar' $null 'rooms similar'
Req 'GET' '/roomserver/rooms/2/similar' $null 'roomserver similar'

Write-Output ''
Write-Output '=== leaderboard / referrers (client POSTs these) ==='
Req 'POST' '/leaderboard/GetNearbyScores' '{"PlayerId":3,"StatChannel":1,"RoomId":9,"FilterType":0,"SortAscending":false,"WindowSize":10}' 'nearby scores'
Req 'POST' '/api/externalfriendinvite/v1/getplatformreferrers' '' 'referrers'

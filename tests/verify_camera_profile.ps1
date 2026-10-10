# End-to-end in-game camera photo -> profile picture, exactly as the client does it:
#   1. POST /api/images/v4/uploadsaved   (multipart binary, NO usable content-type on some clients)
#   2. PUT  /account/me/profileimage     (form-urlencoded, no Content-Type header)
#   3. GET  /account/me                  (profileImage must be the sharecamera name)
$src   = 'C:\Users\LOSTE GAME\Desktop\Rex Room master'
$store = "$src\bin\Release\net10.0\Storage"
$jpg   = 'C:\Users\LOSTEG~1\AppData\Local\Temp\opencode\sharecam.jpg'

$tk = (Get-Content "$store\1\account.json" -Raw | ConvertFrom-Json).Token
$base = 'https://127.0.0.1:5229'

Write-Output "=== 1. POST /api/images/v4/uploadsaved (multipart, $( (Get-Item $jpg).Length ) byte jpeg) ==="
$up = (& curl.exe -s -k "$base/api/images/v4/uploadsaved" -X POST `
        -H "Authorization: Bearer $tk" -F "file=@$jpg;type=image/jpeg" 2>&1 | Out-String).Trim()
Write-Output "  $up"

$img = ($up | ConvertFrom-Json).ImageName
if (-not $img) { Write-Output '  FAIL: no ImageName returned'; exit 1 }
Write-Output "  -> ImageName = $img"

Write-Output ''
Write-Output '=== 2. PUT /account/me/profileimage (form-urlencoded, no Content-Type) ==='
$enc = [uri]::EscapeDataString($img)
$put = (& curl.exe -s -k "$base/account/me/profileimage" -X PUT `
         -H "Authorization: Bearer $tk" --data-raw "imageName=$enc" 2>&1 | Out-String).Trim()
Write-Output "  $put"

Write-Output ''
Write-Output '=== 3. GET /account/me ==='
$me = (& curl.exe -s -k "$base/account/me" -H "Authorization: Bearer $tk" 2>&1 | Out-String)
$pi = [regex]::Match($me, '"profileImage":"([^"]*)"').Groups[1].Value
Write-Output "  profileImage = $pi"
if ($pi -eq $img) { Write-Output '  PASS: profile image set to the camera photo' }
else { Write-Output '  FAIL: profile image did not stick' }

Write-Output ''
Write-Output '=== 4. can the photo actually be FETCHED (what the avatar renders)? ==='
foreach ($p in @("/api/images/v6?name=$enc", "/img/$enc")) {
  $code = (& curl.exe -s -k -o NUL -w '%{http_code}' "$base$p" -H "Authorization: Bearer $tk" 2>&1 | Out-String).Trim()
  $type = (& curl.exe -s -k -o NUL -w '%{content_type}' "$base$p" -H "Authorization: Bearer $tk" 2>&1 | Out-String).Trim()
  Write-Output "  $p -> HTTP $code  type=$type"
}

Write-Output ''
Write-Output '=== server log for this upload ==='
Select-String -Path "$src\probe_out.log" -Pattern '\[ImagesUpSld\]' -EA SilentlyContinue |
  Select-Object -Last 4 | ForEach-Object { '  ' + $_.Line.Trim().Substring(0,[Math]::Min(220,$_.Line.Trim().Length)) }
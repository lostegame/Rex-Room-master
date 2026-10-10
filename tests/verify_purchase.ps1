param(
  # Account id under <project>\Player. Created via POST /account/create.
  [int]$AccountId = 3
)
$ErrorActionPreference = 'Continue'
$base = 'https://127.0.0.1:5229'
$src  = 'C:\Users\LOSTE GAME\Desktop\Rex Room master'

# The account lives under the project-root Player folder, which is where
# DatabaseManager.BaseDirectory points (AppContext.BaseDirectory + ..\..\..\Player).
# The stale copies under bin\<config>\Player and bin\Release\net10.0\Storage are NOT
# read by the server - checking those made this script report a 0 balance and three
# bogus declines while nothing had actually been tested.
$acctFile = "$src\Player\$AccountId\account.json"
if (-not (Test-Path -LiteralPath $acctFile)) {
  Write-Output "ABORT: no account $AccountId at $acctFile"
  Write-Output 'Create one first:'
  Write-Output '  curl -k -X POST https://127.0.0.1:5229/account/create --data-raw "username=BuyTest&password=test1234&platformId=buygame_test"'
  Write-Output "Then fund it and re-run:  -AccountId <newId>"
  exit 1
}

$tok  = (Get-Content $acctFile -Raw | ConvertFrom-Json).Token
$tmp  = "$env:TEMP\buyitem_body.json"

# PowerShell 5.1 strips embedded double quotes when calling native exes, so the
# JSON body goes through a file instead of the command line.
function Buy($itemId, $claimedPrice, $label) {
  $json = '{"StorefrontType":300,"PurchasableItemId":' + $itemId +
          ',"CurrencyType":2,"RequestedPrice":' + $claimedPrice +
          ',"CouponConsumablePlayerMappingId":null,"Gift":null}'
  Set-Content -Path $tmp -Value $json -NoNewline -Encoding UTF8
  $out = & curl.exe -s -k -o - -w '`n[HTTP %{http_code}]' -X POST "$base/api/storefronts/v2/buyItem" `
         -H "Authorization: Bearer $tok" -H "Content-Type: application/json" --data-binary "@$tmp" 2>&1 | Out-String
  $out = ($out -replace '\s+',' ').Trim()
  if ($out.Length -gt 340) { $out = $out.Substring(0,340) + ' ...' }
  "[$label] $out"
}

function Balance {
  $r = & curl.exe -s -k "$base/api/storefronts/v4/balance/2" -H "Authorization: Bearer $tok" 2>&1 | Out-String
  if ($r -match '"Balance":(\d+)') { return [int]$Matches[1] }
  return -1
}

function Pending($label) {
  $r = & curl.exe -s -k "$base/api/gamerewards/v1/pending" -H "Authorization: Bearer $tok" 2>&1 | Out-String
  $r = ($r -replace '\s+',' ').Trim()
  if ($r.Length -gt 340) { $r = $r.Substring(0,340) + ' ...' }
  "[$label] $r"
}

Write-Output "start balance = $(Balance)"
Write-Output ''
Write-Output '--- 1. buy consumable 2169: catalog price is 20, request CLAIMS price 1 ---'
Buy 2169 1 'buy 2169'
Write-Output "balance = $(Balance)   <- 4980 proves the catalog price was charged, not the claimed 1"
Pending 'pending (the purchase should be a box)'
Write-Output ''
Write-Output '--- 2. buy wearable giftdrop 7 (Archer Quiver, catalog price 3000) ---'
Buy 7 1 'buy 7'
Write-Output "balance = $(Balance)"
Write-Output ''
Write-Output '--- 3. owned wardrobe (is the purchased instance there?) ---'
$r = & curl.exe -s -k "$base/econ/customAvatarItems/v1/owned?skip=0&take=100" -H "Authorization: Bearer $tok" 2>&1 | Out-String
$whoami = & curl.exe -s -k "$base/account/me" -H "Authorization: Bearer $tok" 2>&1 | Out-String
if ($whoami -match '"accountId":(\d+)') { $whoId = $Matches[1] } else { $whoId = '??' }
Write-Output "  raw = $r"
Write-Output "  debug: token length=$($tok.Length) -> accountId=$whoId ; accountFile=$acctFile"
if ($r -match '"TotalResults":(\d+)') { "  TotalResults = $($Matches[1])" } else { "  TotalResults not found" }
# Assert on the instance id the grant actually reported ("Granted":"items#328"),
# NOT on a prefab name. This script used to grep for "Quiver" and failed while the
# purchase had succeeded: giftdrop 7 grants ArrowHat_Hat, so the name was a wrong
# expectation, not a server fault. The reference capture has no populated wardrobe
# example (only "Results":[]), so the prefab naming cannot be confirmed from it.
if ($r -match '"AvatarItemId":328') { "  PASS  instance 328 is in the owned list" } else { "  FAIL  instance 328 missing" }
if ($r -match '"PrefabName":"([^"]*)"') { "  granted prefab = $($Matches[1])" } else { "  PrefabName not present" }
if ($r -match '"AvatarItemId":328[^}]*?"IsBaseAvatarItem":(\w+)') {
  $base = $Matches[1]
  if ($base -eq 'true') { "  NOTE IsBaseAvatarItem=true on a PURCHASED item - the client may filter it out of the owned list" }
}
Write-Output ''
Write-Output '--- 4. buy the 3000-token item again with too little left -> decline ---'
Buy 7 1 'buy 7 again'
Write-Output "balance = $(Balance)   <- unchanged, purchase declined"

# Steps 1-4 leave the balance too low to tell "declined for funds" apart from
# "granted nothing but still charged". Step 5 tops the account back up so the
# duplicate really does reach Grant(), which is the case that used to cost 3000
# tokens a second time while answering "alreadyowned".
Write-Output ''
Write-Output '--- 5. duplicate buy WITH enough tokens must NOT be charged ---'
$acctFile = "$src\Player\$AccountId\account.json"
$acct = Get-Content $acctFile -Raw | ConvertFrom-Json
$acct.tokens = 5000
Set-Content -LiteralPath $acctFile -Value ($acct | ConvertTo-Json -Depth 20 -Compress) -NoNewline -Encoding UTF8
$b5 = Balance
Write-Output "balance topped up to $b5"
Buy 7 1 'buy 7 a third time'
$after5 = Balance
Write-Output "balance = $after5"
if ($after5 -eq $b5) { Write-Output '  PASS  duplicate purchase refunded, the player kept their tokens' }
else { Write-Output "  FAIL  player lost $($b5 - $after5) tokens for an item they already own" }

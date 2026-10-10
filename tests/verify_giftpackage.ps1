param([int]$AccountId = 3, [int]$ReceiverId = 0)

# Verifies the gift box pipeline end to end against the running server:
#   1. buying a wearable mints a box into the buyer's bag (not an instant unlock)
#   2. the box shows up in GET /api/avatar/v2/gifts with avatarItemType a NUMBER
#   3. the buyItem response carries that box in BalanceUpdates[].Data
#   4. consuming the box unlocks the wearable, and only then
#   5. a gift line with ToPlayerId puts the box in the RECEIVER's bag, and the
#      sender is still charged
#   6. a consumable box carries avatarItemType NULL, not 0
#
# Reference: CLAUDE.md client-contract-notes - "without a box the purchase shows the
# player nothing", and a box's avatarItemType is NULL for anything that is not an
# avatar item and a NUMBER when it holds one. Field set from the reference
# GiftPackagePayload (apps/notify/src/notification-payloads.ts).

$ErrorActionPreference = 'Stop'
# The server listens on https://localhost:5229 (Server.cs UseUrls), so -SkipCertificateCheck
# is required - same as the other verify_*.ps1 scripts.
$base = 'https://127.0.0.1:5229'
$root = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $root 'Player'

$pass = 0; $fail = 0
function Ok($msg)   { $script:pass++; Write-Host "  PASS  $msg" -ForegroundColor Green }
function Bad($msg)  { $script:fail++; Write-Host "  FAIL  $msg" -ForegroundColor Red }
function Head($msg) { Write-Host "`n$msg" -ForegroundColor Cyan }

function Get-Json([string]$path, $token) {
    $h = @()
    if ($token) { $h += @('-H', "Authorization: Bearer $token") }
    (& curl.exe -s -k @h "$base$path" 2>&1 | Out-String).Trim()
}

function Post-Json([string]$path, $token, $body) {
    # --data-raw mangles a JSON body when curl.exe is invoked from Windows
    # PowerShell 5.1: the server logged raw=56 chars for a 62 char body and could
    # not parse it. Writing the body to a file and using --data-binary @file sends
    # it byte for byte (verified: the same request then parses and grants).
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('giftbody_' + [Guid]::NewGuid().ToString('N') + '.json')
    [System.IO.File]::WriteAllText($tmp, $body, (New-Object System.Text.UTF8Encoding $false))
    $h = @('-X', 'POST', '-H', 'Content-Type: application/json')
    if ($token) { $h += @('-H', "Authorization: Bearer $token") }
    try { (& curl.exe -s -k @h --data-binary "@$tmp" "$base$path" 2>&1 | Out-String).Trim() }
    finally { Remove-Item -LiteralPath $tmp -Force -EA SilentlyContinue }
}

function Read-Account([int]$id) {
    $p = Join-Path $dataDir "$id\account.json"
    if (-not (Test-Path -LiteralPath $p)) { return $null }
    Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
}

function Write-Account($acct) {
    $p = Join-Path $dataDir "$($acct.Id)\account.json"
    $acct | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $p -Encoding UTF8
}

# ---------------------------------------------------------------- fixtures
$acctFile = Join-Path $dataDir "$AccountId\account.json"
if (-not (Test-Path -LiteralPath $acctFile)) {
    Write-Host "account $AccountId not found at $acctFile - create it first" -ForegroundColor Red
    exit 1
}
$acct = Read-Account $AccountId
$token = $acct.Token
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "account $AccountId has no Token" -ForegroundColor Red
    exit 1
}

if ($ReceiverId -le 0) {
    $ReceiverId = if ($AccountId -eq 1) { 2 } else { 1 }
}
$rFile = Join-Path $dataDir "$ReceiverId\account.json"
if (-not (Test-Path -LiteralPath $rFile)) {
    Write-Host "receiver account $ReceiverId not found at $rFile" -ForegroundColor Red
    exit 1
}
$rAcct = Read-Account $ReceiverId
$rToken = $rAcct.Token

Write-Host "buyer    = account $AccountId ($($acct.Username)) tokens=$($acct.Tokens)" -ForegroundColor Yellow
Write-Host "receiver = account $ReceiverId ($($rAcct.Username))" -ForegroundColor Yellow

# sf 2000 = Archer Quiver (Grey), 3000 tokens, a wearable (AvatarItemDesc present)
$WearableId = 7
$ConsumableId = 2169   # Film (Dawn), 20 tokens, consumable
$WearPrice = 3000
$ConsPrice = 20

# ---------------------------------------------------------------- 1. gifts list starts empty
Head "1. gift list is empty before buying"
$before = Get-Json '/api/avatar/v2/gifts' $token
try { $beforeObj = $before | ConvertFrom-Json } catch { $beforeObj = $null }
if ($null -eq $beforeObj) { Bad "gifts list is not JSON: $before" }
elseif (@($beforeObj).Count -eq 0) { Ok "no boxes before the test ($($before.Length) bytes)" }
else { Write-Host "  note: $($beforeObj.Count) pre-existing box(es), using ids above them" -ForegroundColor Yellow }

$preExisting = @($beforeObj).Count

# ---------------------------------------------------------------- fund the buyer
Head "2. fund the buyer"
$acct = Read-Account $AccountId
$acct.tokens = 5000
Write-Account $acct
$acct = Read-Account $AccountId
if ($acct.Tokens -eq 5000) { Ok "buyer tokens = 5000" } else { Bad "buyer tokens = $($acct.Tokens)" }

# ---------------------------------------------------------------- 3. buy a wearable -> a box
Head "3. buy a wearable: must mint a BOX, not unlock instantly"
$acct = Read-Account $AccountId
$unlockedBefore = @($acct.UnlockedAvatarItemIds).Count
$buy = Post-Json '/api/storefronts/v2/buyItem' $token ('{{"StorefrontType":300,"PurchasableItemId":{0},"CurrencyType":2,"RequestedPrice":{1},"Gift":null}}' -f $WearableId, $WearPrice)
Write-Host "  response: $($buy.Substring(0, [Math]::Min(230, $buy.Length)))" -ForegroundColor DarkGray
try { $buyObj = $buy | ConvertFrom-Json } catch { $buyObj = $null; Bad "buyItem response is not JSON: $buy" }

if ($null -ne $buyObj) {
    if ($buyObj.ErrorCode -eq 0) { Ok "purchase accepted (ErrorCode 0)" }
    else { Bad "purchase declined: ErrorCode $($buyObj.ErrorCode)" }

    # The whole point: Data must not be empty any more.
    $data = @($buyObj.BalanceUpdates[0].Data)
    if ($data.Count -ge 1) { Ok "BalanceUpdates[0].Data carries $($data.Count) box(es)" }
    else { Bad "BalanceUpdates[0].Data is empty - the client is told nothing was bought" }

    if ($data.Count -ge 1) {
        $b = $data[0]
        if ($null -ne $b.AvatarItemType) { Ok "wearable box avatarItemType = $($b.AvatarItemType) (a NUMBER, required)" }
        else { Bad "wearable box avatarItemType is null - the client will look for the wrong item type" }
        if (-not [string]::IsNullOrWhiteSpace($b.AvatarItemDesc)) { Ok "box names the item: $($b.AvatarItemDesc)" }
        else { Bad "box has an empty AvatarItemDesc" }
        if ($null -eq $b.PlatformsToSpawnOn) { Ok "PlatformsToSpawnOn omitted (unknown in the reference, not guessed)" }
        else { Ok "PlatformsToSpawnOn present" }
    }
}

$acct = Read-Account $AccountId
$unlockedAfterBuy = @($acct.UnlockedAvatarItemIds).Count
if ($unlockedAfterBuy -eq $unlockedBefore) { Ok "nothing unlocked yet - the box is what delivers it" }
else { Bad "the purchase unlocked $unlockedAfterBuy item(s) directly, bypassing the box" }

$tokensAfterBuy = $acct.Tokens
if ($tokensAfterBuy -eq (5000 - $WearPrice)) { Ok "charged ${WearPrice}: tokens = $tokensAfterBuy" }
else { Bad "tokens = $tokensAfterBuy, expected $((5000 - $WearPrice))" }

# ---------------------------------------------------------------- 4. the box is in the bag
Head "4. the box is listed by GET /api/avatar/v2/gifts"
$list = Get-Json '/api/avatar/v2/gifts' $token
try { $listObj = $list | ConvertFrom-Json } catch { $listObj = $null; Bad "gifts list is not JSON: $list" }
$boxes = @($listObj)
if ($boxes.Count -eq ($preExisting + 1)) { Ok "gifts list grew from $preExisting to $($boxes.Count)" }
else { Bad "gifts list has $($boxes.Count) box(es), expected $($preExisting + 1)" }

$box = $null
if ($boxes.Count -gt 0) {
    $box = $boxes | Where-Object { -not [string]::IsNullOrWhiteSpace($_.AvatarItemDesc) } | Select-Object -First 1
    if ($null -ne $box) { Ok "found the wearable box, id=$($box.Id) from=$($box.FromPlayerId)" }
    else { Bad "no box naming an avatar item in the list" }
}

# ---------------------------------------------------------------- 5. consume unlocks it
Head "5. consuming the box unlocks the wearable (and only then)"
if ($null -eq $box) {
    Bad "no box to consume, skipping"
} else {
    $consume = Post-Json '/api/avatar/v2/gifts/consume/' $token ('{{"giftPackageId":{0}}}' -f $box.Id)
    Write-Host "  response: $($consume.Substring(0, [Math]::Min(200, $consume.Length)))" -ForegroundColor DarkGray
    try { $cObj = $consume | ConvertFrom-Json } catch { $cObj = $null; Bad "consume response is not JSON" }
    if ($null -ne $cObj) {
        if ($cObj.success -eq $true) { Ok "consume succeeded" }
        else { Bad "consume failed: $($cObj.error)" }
        if ($null -ne $cObj.value) { Ok "consume returned the opened box in value (not null)" }
        else { Bad "consume returned value:null - the client re-renders from this" }
    }

    $acct = Read-Account $AccountId
    $unlockedAfterConsume = @($acct.UnlockedAvatarItemIds).Count
    if ($unlockedAfterConsume -eq ($unlockedBefore + 1)) { Ok "exactly one item unlocked by opening the box" }
    elseif ($unlockedAfterConsume -gt $unlockedBefore) { Bad "opening one box unlocked $($unlockedAfterConsume - $unlockedBefore) items" }
    else { Bad "opening the box unlocked nothing (still $unlockedAfterConsume)" }

    $after = Get-Json '/api/avatar/v2/gifts' $token
    try { $afterObj = $after | ConvertFrom-Json } catch { $afterObj = $null }
    if (@($afterObj).Count -eq $preExisting) { Ok "consumed box is gone from the list" }
    else { Bad "list still has $(@($afterObj).Count) boxes, expected $preExisting" }

    # re-consuming must not double-grant
    $again = Post-Json '/api/avatar/v2/gifts/consume/' $token ('{{"giftPackageId":{0}}}' -f $box.Id)
    if ($again -match '"success":false') { Ok "re-consuming the same id is refused" }
    else { Bad "re-consuming the same id was accepted: $again" }
    $acct = Read-Account $AccountId
    if (@($acct.UnlockedAvatarItemIds).Count -eq $unlockedAfterConsume) { Ok "no double grant on re-consume" }
    else { Bad "re-consume changed the unlock count" }
}

# ---------------------------------------------------------------- 6. consumable box: avatarItemType NULL
Head "6. a consumable box must carry avatarItemType NULL, not 0"
$acct = Read-Account $AccountId
$acct.tokens = 5000
Write-Account $acct
$buyC = Post-Json '/api/storefronts/v2/buyItem' $token ('{{"StorefrontType":300,"PurchasableItemId":{0},"CurrencyType":2,"RequestedPrice":{1},"Gift":null}}' -f $ConsumableId, $ConsPrice)
try { $cBuy = $buyC | ConvertFrom-Json } catch { $cBuy = $null; Bad "consumable buyItem is not JSON" }
if ($null -ne $cBuy) {
    $cData = @($cBuy.BalanceUpdates[0].Data)
    if ($cData.Count -ge 1) {
        $cb = $cData[0]
        if ($null -eq $cb.AvatarItemType) { Ok "consumable box avatarItemType is null (required - 0 would fail 'can't find avatar item')" }
        else { Bad "consumable box avatarItemType = $($cb.AvatarItemType), must be null" }
        if (-not [string]::IsNullOrWhiteSpace($cb.ConsumableItemDesc)) { Ok "box names the consumable: $($cb.ConsumableItemDesc)" }
        else { Bad "consumable box has an empty ConsumableItemDesc" }
    } else { Bad "consumable purchase delivered no box" }
}

# clean the consumable box up so the next run starts cleaner
$list = Get-Json '/api/avatar/v2/gifts' $token
try { $listObj = $list | ConvertFrom-Json } catch { $listObj = $null }
foreach ($b in @($listObj)) {
    if ($b -and -not [string]::IsNullOrWhiteSpace($b.ConsumableItemDesc)) {
        [void](Post-Json '/api/avatar/v2/gifts/consume/' $token ('{{"giftPackageId":{0}}}' -f $b.Id))
    }
}

# ---------------------------------------------------------------- 7. gift to another player
Head "7. gift line with ToPlayerId reaches the RECEIVER's bag"
$acct = Read-Account $AccountId
$acct.tokens = 5000
Write-Account $acct
$senderTokens = (Read-Account $AccountId).Tokens

$rBefore = @((Get-Json '/api/avatar/v2/gifts' $rToken | ConvertFrom-Json)).Count
$rUnlockedBefore = @((Read-Account $ReceiverId).UnlockedAvatarItemIds).Count

$gift = Post-Json '/api/items/bulkpurchase' $token ('{{"StorefrontType":300,"PurchasableItemId":{0},"CurrencyType":2,"RequestedPrice":{1},"Gift":{{"ToPlayerId":{2}}}}}' -f $WearableId, $WearPrice, $ReceiverId)
Write-Host "  response: $($gift.Substring(0, [Math]::Min(230, $gift.Length)))" -ForegroundColor DarkGray
try { $gObj = $gift | ConvertFrom-Json } catch { $gObj = $null; Bad "bulkpurchase response is not JSON: $gift" }
if ($null -ne $gObj) {
    if ($gObj.ErrorCode -eq 0) { Ok "gift accepted (ErrorCode 0)" }
    else { Bad "gift declined: ErrorCode $($gObj.ErrorCode) Error=$($gObj.Error)" }
}

$senderAfter = (Read-Account $AccountId).Tokens
if ($senderAfter -eq ($senderTokens - $WearPrice)) { Ok "sender still charged: $senderTokens -> $senderAfter" }
else { Bad "sender tokens = $senderAfter, expected $($senderTokens - $WearPrice)" }

$rList = Get-Json '/api/avatar/v2/gifts' $rToken
try { $rObj = $rList | ConvertFrom-Json } catch { $rObj = $null; Bad "receiver's gifts list is not JSON: $rList" }
$rAfter = @($rObj).Count
if ($rAfter -eq ($rBefore + 1)) { Ok "receiver's bag grew from $rBefore to $rAfter" }
else { Bad "receiver has $rAfter box(es), expected $($rBefore + 1)" }

$rBox = @($rObj) | Where-Object { $_.FromPlayerId -eq $AccountId } | Select-Object -First 1
if ($null -ne $rBox) { Ok "receiver's box records FromPlayerId = $AccountId (the sender)" }
else { Bad "no box on the receiver names the sender as FromPlayerId" }

$rUnlockedAfter = @((Read-Account $ReceiverId).UnlockedAvatarItemIds).Count
if ($rUnlockedAfter -eq $rUnlockedBefore) { Ok "receiver is not unlocked until they open their box" }
else { Bad "receiver was unlocked by the gift without opening a box" }

# the sender must NOT have received their own gift box
$sList = @((Get-Json '/api/avatar/v2/gifts' $token | ConvertFrom-Json))
$sSelfGift = $sList | Where-Object { $_.FromPlayerId -eq $ReceiverId } | Select-Object -First 1
if ($null -eq $sSelfGift) { Ok "the box went to the receiver only, not back to the sender" }
else { Bad "a box landed in the sender's own bag" }

# open the receiver's box so the run leaves nothing dangling
if ($null -ne $rBox) {
    $rc = Post-Json '/api/avatar/v2/gifts/consume/' $rToken ('{{"giftPackageId":{0}}}' -f $rBox.Id)
    if ($rc -match '"success":true') { Ok "receiver opened their gift box" }
    else { Bad "receiver could not open the box: $rc" }
    $rUnlockedFinal = @((Read-Account $ReceiverId).UnlockedAvatarItemIds).Count
    if ($rUnlockedFinal -eq ($rUnlockedBefore + 1)) { Ok "opening the gifted box unlocked exactly one item for the receiver" }
    else { Bad "receiver unlock count went $rUnlockedBefore -> $rUnlockedFinal" }
}

# ---------------------------------------------------------------- 8. gift to a receiver that does not exist
Head "8. a gift to a receiver that does not exist costs the sender nothing"
$acct = Read-Account $AccountId
$acct.tokens = 5000
Write-Account $acct
$before8 = (Read-Account $AccountId).Tokens
$bad = Post-Json '/api/items/bulkpurchase' $token ('{{"PurchasableItemId":{0},"CurrencyType":2,"Gift":{{"ToPlayerId":999999}}}}' -f $WearableId)
$after8 = (Read-Account $AccountId).Tokens
if ($after8 -eq $before8) { Ok "no tokens taken for a refused receiver ($before8 -> $after8)" }
else { Bad "sender lost $($before8 - $after8) tokens on a refused gift" }

# ---------------------------------------------------------------- summary
Write-Host "`n===================================" -ForegroundColor Cyan
Write-Host " passed: $pass   failed: $fail" -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
Write-Host "===================================" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
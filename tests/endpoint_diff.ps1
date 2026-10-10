$ref  = "C:\Users\LOSTE GAME\Desktop\recflaer logs\extracted\live-20260928-1744\LogOutput_live.log"
$ours = "C:\Users\LOSTE GAME\Desktop\rex room\BepInEx\LogOutput.log"

function Endpoints($path) {
  $set = @{}
  Select-String -Path $path -Pattern '\[HTTP\] (?:Get|Post|Put|Delete) https?://\S+' -AllMatches -EA SilentlyContinue |
    ForEach-Object {
      foreach ($m in $_.Matches) {
        # drop the "[HTTP] <verb> " prefix, then scheme+host, so recflare.net and
        # localhost share one namespace; also drop the query string
        $u = $m.Value -replace '^\[HTTP\]\s+\S+\s+', ''
        $u = $u -replace '^\w+://[^/]+', ''
        $u = $u -replace '\?.*$', ''
        if ($u.Length -gt 2) { $set[$u.ToLower()] = $true }
      }
    }
  $set
}

$r = Endpoints $ref
$o = Endpoints $ours

Write-Output "reference endpoints : $($r.Count)"
Write-Output "our client endpoints: $($o.Count)"
Write-Output ''

$refOnly = $r.Keys | Where-Object { -not $o.ContainsKey($_) } | Sort-Object
Write-Output "=== in the REFERENCE but our client never called ($($refOnly.Count)) ==="
$refOnly | ForEach-Object { "  $_" }
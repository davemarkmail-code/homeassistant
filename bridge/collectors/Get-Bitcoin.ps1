<#
    Get-Bitcoin.ps1
    ---------------
    The simplest possible collector: call a public JSON API, write one line.

    Use this as the template for ANY "fetch a number on a timer" sensor —
    weather, exchange rates, server stats, a REST endpoint at work.

    Output:  GBP 48,161|48161|+ 0.03%

    No auth, no config. Swap the URL and the field names and you're done.
#>

$OutputFile = Join-Path $PSScriptRoot 'Bitcoin.txt'

try {
    $Url  = 'https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=gbp&include_24hr_change=true'
    $Data = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 15

    $Price  = [math]::Round($Data.bitcoin.gbp)
    $Change = [math]::Round($Data.bitcoin.gbp_24h_change, 2)

    $PriceText = 'GBP ' + $Price.ToString('N0')
    $ChangeText = if ($Change -gt 0) { '+ ' + $Change + '%' }
                  elseif ($Change -lt 0) { '- ' + [math]::Abs($Change) + '%' }
                  else { '0%' }

    "$PriceText|$Price|$ChangeText" |
        Set-Content -Path $OutputFile -Encoding UTF8
}
catch {
    # Always leave a parseable line. A half-written file is worse than a stale one.
    'Unavailable|0|Check connection' |
        Set-Content -Path $OutputFile -Encoding UTF8
}

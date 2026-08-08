<#
    Get-NewsFeed.ps1
    ----------------
    Pulls any RSS/Atom feed and writes one story per line.

    Works with any feed — news, a blog, a status page, GitHub releases,
    a podcast. Change $feedUrl and you're done.

    Output (one line per story):
        Headline|Summary|08 Aug  14:23|https://link|https://image.jpg

    Handles the two things that always bite with RSS:
      * HTML entities and stray tags in titles/descriptions
      * Namespaced elements like <media:thumbnail>, which need local-name()
#>

$itemsFile = Join-Path $PSScriptRoot 'NewsItems.txt'
$feedUrl   = 'https://feeds.bbci.co.uk/news/rss.xml'   # <- change me
$maxItems  = 8

function Clean-Field([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return '' }
    $value = [System.Net.WebUtility]::HtmlDecode($value)      # &amp; -> &
    $value = [regex]::Replace($value, '<[^>]+>', ' ')         # strip tags
    $value = [regex]::Replace($value, '\s+', ' ').Trim()
    return $value.Replace('|', '-')                           # protect the delimiter
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    [xml]$rss  = (Invoke-WebRequest -Uri $feedUrl -UseBasicParsing -TimeoutSec 20).Content
    $feedItems = @($rss.rss.channel.item) | Select-Object -First $maxItems
    if ($feedItems.Count -eq 0) { throw 'Feed returned no items.' }

    $lines = foreach ($item in $feedItems) {
        $headline  = Clean-Field ([string]$item.SelectSingleNode('title').InnerText)
        $summary   = Clean-Field ([string]$item.SelectSingleNode('description').InnerText)
        $link      = Clean-Field ([string]$item.SelectSingleNode('link').InnerText)
        $published = ([DateTimeOffset]::Parse(
                        [string]$item.SelectSingleNode('pubDate').InnerText)
                     ).ToLocalTime().ToString('dd MMM  HH:mm')

        # Namespaced elements (media:thumbnail) need local-name() to match
        $thumb = $item.SelectSingleNode("*[local-name()='thumbnail']")
        $image = if ($null -ne $thumb -and $null -ne $thumb.Attributes['url']) {
                     Clean-Field ([string]$thumb.Attributes['url'].Value)
                 } else { '' }

        "$headline|$summary|$published|$link|$image"
    }

    [IO.File]::WriteAllLines($itemsFile, $lines, (New-Object Text.UTF8Encoding($false)))
}
catch {
    # Only write a placeholder if we have nothing at all — otherwise keep the
    # last good copy so the dashboard shows stale news rather than an error.
    if (-not (Test-Path -LiteralPath $itemsFile)) {
        [IO.File]::WriteAllText($itemsFile,
            'Feed unavailable|It will retry automatically.|Not updated||',
            (New-Object Text.UTF8Encoding($false)))
    }
}

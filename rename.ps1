$path = "c:\Users\Asus\OneDrive\Desktop\RezerveApp"
$log = "$path\rename_log.txt"

function Rename-Content {
    $files = Get-ChildItem -Path $path -Recurse -File | Where-Object { 
        $_.Extension -match "\.(cs|cshtml|json|csproj|sln|css|js|html|txt|md)$" -and $_.FullName -notmatch "\\(bin|obj|node_modules|\.git|node_modules)\\"
    }

    foreach ($f in $files) {
        try {
            $content = [System.IO.File]::ReadAllText($f.FullName)
            if ($content -match "(?i)berbersaas") {
                $newContent = [System.Text.RegularExpressions.Regex]::Replace($content, "BerberSaaS", "RezerveApp")
                $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, "berbersaas", "rezerveapp")
                $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, "(?i)berbersaas", "RezerveApp")
                [System.IO.File]::WriteAllText($f.FullName, $newContent, [System.Text.Encoding]::UTF8)
                Add-Content -Path $log -Value "Replaced content in: $($f.FullName)"
            }
        } catch {
            Add-Content -Path $log -Value "Failed to read/write: $($f.FullName)"
        }
    }
}

function Rename-FilesAndDirs {
    # 2. Rename files
    $filesToRename = Get-ChildItem -Path $path -Recurse -File | Where-Object { $_.Name -match "(?i)berbersaas" -and $_.FullName -notmatch "\\(bin|obj|node_modules|\.git)\\" }
    foreach ($f in $filesToRename) {
        $newName = [System.Text.RegularExpressions.Regex]::Replace($f.Name, "(?i)berbersaas", "RezerveApp")
        Rename-Item -Path $f.FullName -NewName $newName
        Add-Content -Path $log -Value "Renamed file: $($f.FullName) to $newName"
    }

    # 3. Rename directories
    $dirsToRename = Get-ChildItem -Path $path -Recurse -Directory | Where-Object { $_.Name -match "(?i)berbersaas" -and $_.FullName -notmatch "\\(bin|obj|node_modules|\.git)\\" } | Sort-Object -Property @{Expression={$_.FullName.Length}; Descending=$true}
    foreach ($d in $dirsToRename) {
        $newName = [System.Text.RegularExpressions.Regex]::Replace($d.Name, "(?i)berbersaas", "RezerveApp")
        Rename-Item -Path $d.FullName -NewName $newName
        Add-Content -Path $log -Value "Renamed directory: $($d.FullName) to $newName"
    }
}

Rename-Content
Rename-FilesAndDirs
Write-Host "Done"

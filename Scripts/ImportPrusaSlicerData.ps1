Write-Output 'PrusaSlicer Printers Instalation'
Write-Output 'This will replace printers, all changes will be discarded'

$input_dir = "$Env:AppData\PrusaSlicer"
$output_dir = Resolve-Path "$PSScriptRoot\..\PrusaSlicer" | select -ExpandProperty Path 
$print_dir = 'sla_print'
$printer_dir = 'printer'

Write-Output $input_dir
Write-Output $output_dir

# Need to ignore FDM printers since they are on the same folder
Get-ChildItem "$input_dir\$printer_dir" -Filter "*.ini" | 
Foreach-Object {
    $content = Get-Content $_.FullName
    $regex = $content -match 'printer_technology.*=.*(SLA)'
    if($regex){
        xcopy /d /y $_.FullName "$output_dir\$printer_dir"
    }
}

#foreach ($printer in $printers) {
#    xcopy /d /y "$input_dir\$printer_dir\$printer" "$output_dir\$printer_dir"
#}

Write-Output 'Importing Profiles'
xcopy /i /y /d "$input_dir\$print_dir" "$output_dir\$print_dir"
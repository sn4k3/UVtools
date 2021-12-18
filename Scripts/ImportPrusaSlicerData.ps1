Set-Location $PSScriptRoot\..

$input_dir = "$Env:AppData\PrusaSlicer"
$output_dir = 'PrusaSlicer'
$print_dir = 'sla_print'
$printer_dir = 'printer'

$printers = 
    'UVtools Prusa SL1.ini',
    'UVtools Prusa SL1S Speed.ini',
    'EPAX E6 Mono.ini',
    'EPAX E10 Mono.ini',
    'EPAX E10 5K.ini',
    'EPAX X1.ini',
    'EPAX X10.ini',
    'EPAX X10 4K Mono.ini',
    'EPAX X10 5K.ini',
    'EPAX X133 4K Mono.ini',
    'EPAX X156 4K Color.ini',
    'EPAX X1K 2K Mono.ini',
    'Zortrax Inkspire.ini',
    'Nova3D Elfin.ini',
    'Nova3D Elfin2.ini',
    'Nova3D Elfin2 Mono SE.ini',
    'Nova3D Elfin3 Mini.ini',
    'Nova3D Bene4.ini',
    'Nova3D Bene4 Mono.ini',
    'Nova3D Bene5.ini',
    'Nova3D Whale.ini',
    'Nova3D Whale2.ini',
    'AnyCubic Photon.ini',
    'AnyCubic Photon S.ini',
    'AnyCubic Photon Zero.ini',
    'AnyCubic Photon X.ini',
    'AnyCubic Photon Ultra.ini',
    'AnyCubic Photon Mono.ini',
    'AnyCubic Photon Mono 4K.ini',
    'AnyCubic Photon Mono SE.ini',
    'AnyCubic Photon Mono X.ini',
    'AnyCubic Photon Mono X 6K.ini',
    'AnyCubic Photon Mono SQ.ini',
    'Elegoo Mars.ini',
    'Elegoo Mars 2 Pro.ini',
    'Elegoo Mars 3.ini',
    'Elegoo Mars C.ini',
    'Elegoo Saturn.ini',
    'Peopoly Phenom.ini',
    'Peopoly Phenom L.ini',
    'Peopoly Phenom Noir.ini',
    'Peopoly Phenom XXL.ini',
    'QIDI Shadow5.5.ini',
    'QIDI Shadow6.0 Pro.ini',
    'QIDI S-Box.ini',
    'QIDI I-Box Mono.ini',
    'Phrozen Shuffle.ini',
    'Phrozen Shuffle Lite.ini',
    'Phrozen Shuffle XL.ini',
    'Phrozen Shuffle XL Lite.ini',
    'Phrozen Shuffle 16.ini',
    'Phrozen Shuffle 4K.ini',
    'Phrozen Sonic.ini',
    'Phrozen Sonic 4K.ini',
    'Phrozen Sonic Mighty 4K.ini',
    'Phrozen Sonic Mini.ini',
    'Phrozen Sonic Mini 4K.ini',
    'Phrozen Sonic Mini 8K.ini',
    'Phrozen Transform.ini',
    'Phrozen Sonic Mega 8K.ini',
    'Kelant S400.ini',
    'Wanhao D7.ini',
    'Wanhao D8.ini',
    'Wanhao CGR Mini Mono.ini',
    'Wanhao CGR Mono.ini',
    'Creality LD-002R.ini',
    'Creality LD-002H.ini',
    'Creality LD-006.ini',
    'Creality HALOT-ONE CL-60.ini',
    'Creality HALOT-SKY CL-89.ini',
    'Creality HALOT-MAX CL-133.ini',
    'Voxelab Polaris 5.5.ini',
    'Voxelab Proxima 6.ini',
    'Voxelab Ceres 8.9.ini',
    'Longer Orange 10.ini',
    'Longer Orange 30.ini',
    'Longer Orange 120.ini',
    'Longer Orange 4K.ini',
    'Uniz IBEE.ini',
    'FlashForge Explorer MAX.ini',
    'FlashForge Focus 8.9.ini',
    'FlashForge Focus 13.3.ini',
    'FlashForge Foto 6.0.ini',
    'FlashForge Foto 8.9.ini',
    'FlashForge Foto 13.3.ini',
    'FlashForge Hunter.ini'
;

Write-Output 'PrusaSlicer Printers Instalation'
Write-Output 'This will replace printers, all changes will be discarded'
Write-Output $input_dir
Write-Output $output_dir

foreach ($printer in $printers) {
    xcopy /d /y "$input_dir\$printer_dir\$printer" "$output_dir\$printer_dir"
}

Write-Output 'Importing Profiles'
xcopy /i /y /d "$input_dir\$print_dir" "$output_dir\$print_dir"
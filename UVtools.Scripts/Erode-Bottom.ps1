# UVtools Script
########################
# Script Configuration #
########################
$_TITLE         = 'Erode Bottom'
$_DESCRIPTION   = 'Erodes bottom layers with a given iteration'
$_AUTHOR        = 'Tiago Conceição'
$_VERSION       = 1
$_CORE_PATH     = '' # Input folder path to 'UVtools.Core.dll' if unable to work with env variables
#eg: [System.Environment]::SetEnvironmentVariable('UVTOOLS_PATH','C:\Program Files (x86)\UVtools', [System.EnvironmentVariableTarget]::User)

##############
# Dont touch #
##############
# Sets the culture
$currentThread = [System.Threading.Thread]::CurrentThread
$culture = [System.Globalization.CultureInfo]::InvariantCulture
$currentThread.CurrentCulture = $culture
$currentThread.CurrentUICulture = $culture

##############
# Dont touch #
##############
# Usefull Variables
$dirSeparator = [IO.Path]::DirectorySeparatorChar
$scriptPath = $MyInvocation.MyCommand.Name
$scriptFilenameNoExt =  [System.IO.Path]::GetFileNameWithoutExtension($scriptPath);
$coreDll = 'UVtools.Core.dll'
$inputFile = $null
$slicerFile = $null

##############
# Dont touch #
##############
# Script information
Write-Output "###############################################"
Write-Output "UVtools Script: ${scriptFilenameNoExt}.ps1"
Write-Output "Title: $_TITLE"
Write-Output $_DESCRIPTION
Write-Output "Author: $_AUTHOR"
Write-Output "Version: $_VERSION"
Write-Output "###############################################"

try{
if(Test-Path "$Env:UVTOOLS_PATH${dirSeparator}${coreDll}" -PathType Leaf){
    Add-Type -Path "$Env:UVTOOLS_PATH${dirSeparator}${coreDll}"
}
elseif(Test-Path "${_CORE_PATH}${dirSeparator}${coreDll}" -PathType Leaf) {
    Add-Type -Path "${_CORE_PATH}${dirSeparator}${coreDll}"
} else {
    Write-Error "Unable to find $coreDll, solutions are:
1) Open powershell with admin and run: [System.Environment]::SetEnvironmentVariable('UVTOOLS_PATH','FOLDER/PATH/TO/UVTOOLS', [System.EnvironmentVariableTarget]::User)
2) Edit the script and set the _CORE_PATH variable with the FOLDER/PATH/TO/UVTOOLS
Path example: 'C:\Program Files (x86)\UVtools'
Exiting now!"
    return
}

# Progress variable, not really used here but require with some methods
$progress = New-Object UVtools.Core.Operations.OperationProgress

##############
# Dont touch #
##############
# Input file and validation
while ($null -eq $inputFile){ # Keep asking for a file if the input is invalid
$inputFile = read-host "Enter input file"
    if($inputFile -eq 'q' -or $inputFile -eq 'e' -or $inputFile -eq 'exit')
    {
        return;
    }
    if (-not(test-path $inputFile)){
        Write-host "Invalid file path, re-enter."
        $inputFile = $null
    }
    elseif ((get-item $inputFile).psiscontainer){
        Write-host "Input file must be an valid file, re-enter."
        $inputFile = $null
    }
    else {
        $slicerFile = [UVtools.Core.FileFormats.FileFormat]::FindByExtension($inputFile, $true, $true)
        if(!$slicerFile){
            Write-host "Invalid file format, re-enter."
            $inputFile = $null
        }
    }
}




######################################
# All user inputs should be put here #
######################################
# Input iterations number
[int]$iterations = 0;
while ($iterations -le 0) { # Keep asking for a number if the input is invalid
    [int]$iterations = Read-Host "Number of bottom erode iterations"
}

##############
# Dont touch #
##############
# Decode the file
Write-Output "Decoding, please wait..."
$slicerFile.Decode($inputFile, $progress);

###################################################
# All operations over the file should be put here #
###################################################
# Morph bottom erode
Write-Output "Eroding bottoms with ${iterations} iterations, please wait..."
$morph = New-Object UVtools.Core.Operations.OperationMorph
$morph.MorphOperation = [Emgu.CV.CvEnum.MorphOp]::Erode
$morph.IterationsStart = $iterations
$morph.LayerIndexEnd = $slicerFile.BottomLayerCount - 1
if(!$morph.Execute($slicerFile, $progress)){ return; }


##############
# Dont touch #
##############
# Save file with _modified name appended
$filePath = [System.IO.Path]::GetDirectoryName($inputFile);
$fileExt = [System.IO.Path]::GetExtension($inputFile);
$fileNoExt = [System.IO.Path]::GetFileNameWithoutExtension($inputFile)
$fileOutput = "${filePath}${dirSeparator}${fileNoExt}_modified${fileExt}"
Write-Output "Saving as ${fileNoExt}_modified${fileExt}, please wait..."
$slicerFile.SaveAs("$fileOutput", $progress)
Write-Output "$fileOutput"
Write-Output "Finished!"
} 
catch{
    # Catch errors
    Write-Error "An error occurred:"
    Write-Error $_.ScriptStackTrace
    Write-Error $_.Exception.Message
    Write-Error $_.Exception.ItemName
}
param (
    [Parameter(Mandatory=$true)][string]$FirstName,
    [Parameter(Mandatory=$true)][string]$LastName,
    [Parameter(Mandatory=$true)][string]$SamAccountName,
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Description,
    [Parameter(Mandatory=$true)][string]$Pager,
    [Parameter(Mandatory=$true)][int]$Iod,
    [Parameter(Mandatory=$true)][string]$Pesel,
    [Parameter(Mandatory=$true)][string]$Npwz,
    [Parameter(Mandatory=$true)][string]$FirstPass,
    [Parameter(Mandatory=$false)][string]$EmailAddress,
    [Parameter(Mandatory=$false)][string]$AccountExpirationDate,
    [Parameter(Mandatory=$false)][string]$Groups
)

function Add-JkAdUserHomeDirectory {

    param (
        [string] $userprincipalname,
        [string] $homedir = '\\wss5-fs\uzytkownicy$\'
    )

    $path = "$homedir$userprincipalname";
    if (!(Test-Path $path))
    {
        # ## Create the directory
        [void] (New-Item -path $homedir -Name $userprincipalname -ItemType Directory);

        ## Modify  Permissions
        $Rights= [System.Security.AccessControl.FileSystemRights]::Read -bor [System.Security.AccessControl.FileSystemRights]::Write -bor [System.Security.AccessControl.FileSystemRights]::Modify -bor [System.Security.AccessControl.FileSystemRights]::FullControl
        $Inherit=[System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
        $Propagation=[System.Security.AccessControl.PropagationFlags]::None
        $Access=[System.Security.AccessControl.AccessControlType]::Allow
        $AccessRule = new-object System.Security.AccessControl.FileSystemAccessRule($userprincipalname, $Rights, $Inherit, $Propagation, $Access)
        $administratorsName = "Administratorzy";
        $AccessRuleForAdministrators = new-object System.Security.AccessControl.FileSystemAccessRule($administratorsName, $Rights, $Inherit, $Propagation, $Access)
        $ACL = Get-Acl $path
        $ACL.AddAccessRule($AccessRule)
        $ACL.AddAccessRule($AccessRuleForAdministrators)
        $Account = new-object system.security.principal.ntaccount($administratorsName)
        $ACL.setowner($Account)
        $ACL.SetAccessRule($AccessRule)
        Set-Acl $path $ACL
    }
}

if (-not $AccountExpirationDate) {
    $AccountExpirationDateParam = $null;
} else {
    $AccountExpirationDateParam = $AccountExpirationDate
}
if (-not $EmailAddress) {
    $emailAddressParam = $null;
} else {
    $emailAddressParam = $EmailAddress;
}
if (-not $Groups) {
    $groupsParam = $null;
} else {
    $groupsParam = $Groups
}

$accountPassword = ConvertTo-SecureString -String $FirstPass -AsPlainText -Force;
$title = $null;
$changePasswordAtLogon = $true;
$homeDirectoryBase = "\\wss5-fs\uzytkownicy$\";
$homeDrive = "P:";
$enabled = $true;
$name = "$firstName $lastName";
$info = "IOD: " + $iod + [System.Environment]::NewLine + `
"PESEL: " + $pesel + [System.Environment]::NewLine + `
"NPWZ: " + $npwz + [System.Environment]::NewLine +`
$pager;

# Czy karta nie jest przypisana do innego użytkownika
if ($pager -notin @("brak", "")) {
    $userWithTheSamePager = Get-ADUser -Filter ("pager -eq " + $pager) -Properties info;
    if ($null -ne $userWithTheSamePager) {
        foreach ($u in $userWithTheSamePager) {
            Set-ADUser -Identity $u.SamAccountName -Replace @{ info = "$($u.info)$([System.Environment]::NewLine)$pager"; pager = "brak" };
        }
        
        Write-Host ("Usunięto pager użytkownikom: " + $userWithTheSamePager.SamAccountName);
    }
}

$parameters = @{
    Name = $name
    AccountExpirationDate = $AccountExpirationDateParam
    AccountPassword = $accountPassword 
    ChangePasswordAtLogon = $changePasswordAtLogon
    Description = $description
    DisplayName = $name
    EmailAddress = $emailAddressParam
    Enabled = $enabled
    GivenName = $firstName
    HomeDirectory = ($homeDirectoryBase + $samAccountName)
    HomeDrive = $homeDrive
    Path = $path
    SamAccountName = $samAccountName
    Surname = $lastName
    Title = $title
    UserPrincipalName = ($samAccountName + "@wss5.net")
};

New-ADUser @parameters;

Set-ADUser -Identity $samAccountName -Replace @{ info = $info; pager = $pager };

if ($null -ne $groupsParam) {
    $groupsSplitted = $groupsParam -split ',';
    foreach ($group in $groupsSplitted) {
        Add-ADGroupMember -Identity $group -Members $samAccountName;
    }
}

Start-Sleep -Seconds 5; # Czasem konto nie jest jeszcze gotowe

Add-JkAdUserHomeDirectory -userprincipalname $samAccountName;

Write-Host "Założono konto użytkownikowi $samAccountName";

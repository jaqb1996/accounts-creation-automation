using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using System.Diagnostics;
using System.Text;

namespace ActiveDirectoryAccountsLibrary
{
    public interface IActiveDirectoryAccountCreator
    {
        Result<string> CreateAccount(ActiveDirectoryUserModel user);
    }

    public class ActiveDirectoryAccountCreator : IActiveDirectoryAccountCreator
    {
        private readonly AdConfig adConfig;

        public ActiveDirectoryAccountCreator(AdConfig adConfig)
        {
            this.adConfig = adConfig;
        }

        public Result<string> CreateAccount(ActiveDirectoryUserModel user)
        {
            string arguments = CreateArguments(user);

            ProcessStartInfo startInfo = new()
            {
                FileName = "pwsh.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = Process.Start(startInfo)!;
            process.WaitForExit();

            string standardError = process.StandardError.ReadToEnd();
            string standardOutput = process.StandardOutput.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return Result.Failure<string>($"Kod błędu: {process.ExitCode}, opis: {standardError}");
            }

            if (!string.IsNullOrEmpty(standardError))
            {
                return Result.Failure<string>($"Wystąpiły błędy: {standardError}");
            }

            return Result.Success(standardOutput);
        }

        private string CreateArguments(ActiveDirectoryUserModel user)
        {
            StringBuilder output = new($"-File {adConfig.Ps1File} -FirstName \"{user.FirstName}\" -LastName \"{user.LastName}\" -SamAccountName \"{user.SamAccountName}\" ");
            output.Append($"-Path \"{user.Path}\" -Description \"{user.Description}\" -Pager \"{user.Pager}\" -Iod \"{user.Iod}\" -Pesel \"{user.Pesel}\" -Npwz \"{user.Npwz}\" -FirstPass \"{adConfig.FirstPassword}\" ");
            
            if (user.EmailAddress is not null)
            {
                output.Append($"-EmailAddress \"{user.EmailAddress}\" ");
            }

            if (user.DateTo is not null)
            {
                output.Append($"-AccountExpirationDate \"{user.DateTo}\" ");
            }

            if (user.Groups is not null)
            {
                output.Append($"-Groups \"{string.Join(',', user.Groups)}\"");
            }

            return output.ToString();
        }
    }
}

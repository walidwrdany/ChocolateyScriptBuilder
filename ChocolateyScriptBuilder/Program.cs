using System;
using System.Collections.Generic;
using System.IO;

namespace ChocolateyScriptBuilder
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string[] commands = new string[] { "info", "install", "upgrade", "uninstall" };

            string batchFileContent = @"
@echo off
:: https://stackoverflow.com/a/10052222
:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
    IF ""%PROCESSOR_ARCHITECTURE%"" EQU ""amd64"" (
>nul 2>&1 ""%SYSTEMROOT%\SysWOW64\cacls.exe"" ""%SYSTEMROOT%\SysWOW64\config\system""
) ELSE (
>nul 2>&1 ""%SYSTEMROOT%\system32\cacls.exe"" ""%SYSTEMROOT%\system32\config\system""
)

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^(""Shell.Application""^) > ""%temp%\getadmin.vbs""
    set params= %*
    echo UAC.ShellExecute ""cmd.exe"", ""/c """"%~s0"""" %params:""=""""%"", """", ""runas"", 1 >> ""%temp%\getadmin.vbs""

    ""%temp%\getadmin.vbs""
    del ""%temp%\getadmin.vbs""
    exit /B

:gotAdmin
    pushd ""%CD%""
    CD /D ""%~dp0""
:-------------------------------------- 
:: {0}
choco {1} {2} --confirm
pause
";




            try
            {
                Console.WriteLine("Data read from CSV:");

                foreach (string[] software in ReadFromCsv("software_installs.csv"))
                {
                    string softwareName = software[0].Trim();
                    string packageName = software[1].Replace("choco install", "").Trim();
                    string packagePath = Path.Combine("packages", softwareName);

                    if (!Directory.Exists(packagePath))
                    {
                        Directory.CreateDirectory(packagePath);
                    }

                    Console.WriteLine("Create package: {0}", softwareName);

                    foreach (string command in commands)
                    {
                        string batchFileName = $"{command}.bat";
                        string batchFilePath = Path.Combine(packagePath, batchFileName);

                        if (File.Exists(batchFilePath))
                        {
                            File.Delete(batchFilePath);
                        }

                        using (StreamWriter writer = new StreamWriter(batchFilePath))
                        {
                            writer.Write(string.Format(batchFileContent, softwareName, command, packageName));
                        }
                    }

                    string shortcutFileName = $"{softwareName}.url";
                    string shortcutFilePath = Path.Combine(packagePath, shortcutFileName);

                    using (StreamWriter writer = new StreamWriter(shortcutFilePath))
                    {
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("URL=https://community.chocolatey.org/packages/{0}", packageName);
                    }
                }

                Console.WriteLine("Batch file 'software_installs.csv' has been successfully created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            Console.ReadLine();
        }

        private static List<string[]> ReadFromCsv(string filePath)
        {
            List<string[]> data = new List<string[]>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    data.Add(line.Split(','));
                }
            }

            return data;
        }
    }
}

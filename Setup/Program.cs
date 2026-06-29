using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SetupComercialPerezGonzales;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.Title = "Instalador - Comercial González Pérez";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        catch (Exception) { /* Ignorar errores de consola en entornos no interactivos o headless */ }

        // Arte ASCII de la empresa
        Console.WriteLine(@"
===================================================================
       __   _  _  ____  _  _   __   ____  _  _  __    __   ____ 
      / _\ ( \/ )(  _ \( \/ ) / _\ / ___)( \/ )(  )  / _\ / ___)
     /    \ )  (  ) __// \/ \/    \\___ \ )  ( / (_//    \\___ \
     \_/\_/(_/\_)(__)  \_/\_/\_/\_/(____/(_/\_)\____/\_/\_/(____/
===================================================================
             INSTALADOR - COMERCIAL GONZÁLEZ PÉREZ POS
===================================================================
");
        Console.ResetColor();

        // 1. Definir rutas
        string appDirName = "ComercialPerezGonzalesApp";
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string defaultInstallPath = Path.Combine(localAppData, "Programs", appDirName);

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Este instalador configurará el sistema de Punto de Venta.");
        Console.WriteLine($"Ruta de instalación predeterminada:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {defaultInstallPath}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.Write("¿Desea cambiar la ruta de instalación? (s/N): ");
        
        string ans = Console.ReadLine()?.Trim().ToLower() ?? "";
        string installPath = defaultInstallPath;

        if (ans == "s" || ans == "si")
        {
            Console.Write("Ingrese la nueva ruta completa: ");
            string customPath = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                installPath = customPath;
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Iniciando instalación en: {installPath}");
        Console.ResetColor();

        try
        {
            // Crear el directorio de instalación si no existe
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }
            else
            {
                // Si la carpeta existe y tiene archivos, limpiarla
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("La carpeta de destino ya existe. Limpiando archivos antiguos...");
                Console.ResetColor();
                try
                {
                    foreach (var file in Directory.GetFiles(installPath))
                    {
                        File.Delete(file);
                    }
                    foreach (var dir in Directory.GetDirectories(installPath))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Aviso al limpiar: {ex.Message}. Algunos archivos podrían estar en uso.");
                    Console.ResetColor();
                }
            }

            // 2. Extraer recurso incrustado app.zip
            Console.WriteLine("Extrayendo archivos de la aplicación...");
            
            var assembly = Assembly.GetExecutingAssembly();
            // Buscar el nombre del recurso
            string resourceName = "Setup.app.zip";
            
            using (Stream? resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    // Fallback para nombres de recurso alternativos si varía el namespace
                    var names = assembly.GetManifestResourceNames();
                    foreach (var n in names)
                    {
                        if (n.EndsWith("app.zip"))
                        {
                            resourceName = n;
                            break;
                        }
                    }
                }
            }

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"No se pudo encontrar el recurso incrustado '{resourceName}' en el ensamblado.");
                }

                using (ZipArchive archive = new ZipArchive(stream))
                {
                    int totalFiles = archive.Entries.Count;
                    int currentFile = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        currentFile++;
                        string destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));

                        // Asegurar de no salir del directorio destino
                        if (!destinationPath.StartsWith(installPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (entry.Name == "") // Es un directorio
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, true);

                        // Mostrar barra de progreso
                        double pct = (double)currentFile / totalFiles * 100;
                        Console.Write($"\rProgreso: [{(new string('=', (int)pct / 5))}{(new string(' ', 20 - (int)pct / 5))}] {pct:0.0}% ({currentFile}/{totalFiles} archivos)");
                    }
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("¡Extracción completada con éxito!");
            Console.ResetColor();

            // 3. Crear accesos directos
            Console.WriteLine("Creando accesos directos...");

            string exePath = Path.Combine(installPath, "ComercialPerezGonzales.exe");
            string shortcutName = "Punto de Venta Comercial Gonzalez Perez.lnk";
            
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcut = Path.Combine(desktopPath, shortcutName);

            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            string startMenuShortcut = Path.Combine(startMenuPath, shortcutName);

            CrearAccesoDirecto(exePath, desktopShortcut, installPath);
            CrearAccesoDirecto(exePath, startMenuShortcut, installPath);

            // 4. Crear desinstalador .bat
            string uninstallBatPath = Path.Combine(installPath, "Desinstalar.bat");
            string uninstallContent = @"@echo off
title Desinstalador - Comercial Gonzalez Perez POS
echo ===================================================================
echo             DESINSTALADOR - COMERCIAL GONZALEZ PEREZ
echo ===================================================================
echo.
echo Este script removera el Punto de Venta de su equipo.
echo.
set /p CONFIRM=""¿Esta seguro que desea desinstalar el sistema? (s/N): ""
if /i ""%CONFIRM%"" neq ""s"" if /i ""%CONFIRM%"" neq ""si"" (
    echo Desinstalacion cancelada.
    pause
    exit /b
)

echo.
echo Eliminando accesos directos...
del /f /q ""%USERPROFILE%\Desktop\Punto de Venta Comercial Gonzalez Perez.lnk"" >nul 2>&1
del /f /q ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\Punto de Venta Comercial Gonzalez Perez.lnk"" >nul 2>&1

set /p DELDB=""¿Desea eliminar la base de datos de ventas e historial? (s/N): ""
if /i ""%DELDB%""==""s"" (
    echo Eliminando base de datos local...
    rd /s /q ""%LOCALAPPDATA%\ComercialPerezGonzales"" >nul 2>&1
)

echo.
echo Desinstalacion completada con exito.
echo.
echo Para completar la limpieza, la carpeta de la aplicacion se borrara ahora.
echo Presione una tecla para cerrar y limpiar...
pause

:: Auto-eliminacion de la propia carpeta en segundo plano despues de salir
start """" cmd /c ""timeout /t 2 /nobreak >nul && rd /s /q """"%~dp0""""""
exit
";
            File.WriteAllText(uninstallBatPath, uninstallContent);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Acceso directo creado en el Escritorio.");
            Console.WriteLine("✓ Acceso directo creado en el Menú Inicio.");
            Console.WriteLine("✓ Creado desinstalador Desinstalar.bat en la carpeta de instalación.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===================================================================");
            Console.WriteLine("         ¡INSTALACIÓN COMPLETADA SATISFACTORIAMENTE!");
            Console.WriteLine("===================================================================");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("¿Desea iniciar la aplicación ahora? (S/n): ");
            string launchAns = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (launchAns == "" || launchAns == "s" || launchAns == "si")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = installPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("Ocurrió un error crítico durante la instalación:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Presione cualquier tecla para salir...");
            Console.ReadKey();
        }
    }

    private static void CrearAccesoDirecto(string targetExe, string shortcutPath, string workingDir)
    {
        try
        {
            // Ejecutar un script powershell inline de forma segura y portable en Windows
            // para instanciar el objeto COM WScript.Shell y generar el acceso directo .lnk
            string escapedTarget = targetExe.Replace("'", "''");
            string escapedShortcut = shortcutPath.Replace("'", "''");
            string escapedWorkDir = workingDir.Replace("'", "''");

            string psCommand = $"$WshShell = New-Object -ComObject WScript.Shell; " +
                              $"$Shortcut = $WshShell.CreateShortcut('{escapedShortcut}'); " +
                              $"$Shortcut.TargetPath = '{escapedTarget}'; " +
                              $"$Shortcut.WorkingDirectory = '{escapedWorkDir}'; " +
                              $"$Shortcut.Save()";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear acceso directo: {ex.Message}");
        }
    }
}

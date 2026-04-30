using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;
using System.Text.RegularExpressions;

namespace DevNetControl.Api.Infrastructure.Services;

public class SshUserManager
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;

    public SshUserManager(ApplicationDbContext context, EncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    private static readonly Regex SafeUsername = new(@"^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);
    private static readonly Regex SafePassword = new(@"^[^\x27\x22\\`$]{6,128}$", RegexOptions.Compiled);

    public async Task<(bool Success, string Output, string Error)> CreateUserOnVpsAsync(
        Guid nodeId, string username, string password, int maxDevices, DateTime? expiryDate)
    {
        if (!SafeUsername.IsMatch(username))
            return (false, string.Empty, "Nombre de usuario invalido.");

        if (!SafePassword.IsMatch(password))
            return (false, string.Empty, "La contraseña no cumple los requisitos de seguridad.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var sanitizedPassword = EscapeShellArg(password);

                var createUserCmd = $"useradd -m -s /bin/bash {username} && " +
                                    $"echo '{username}:{sanitizedPassword}' | chpasswd";

                if (expiryDate.HasValue)
                {
                    var expiryStr = expiryDate.Value.ToString("yyyy-MM-dd");
                    createUserCmd += $" && chage -E {expiryStr} {username}";
                }

                createUserCmd += $" && echo 'OK: Usuario {username} creado exitosamente'";

                var createResult = await RunCommandAsync(client, createUserCmd);

                if (createResult.ExitStatus != 0)
                    return (false, string.Empty, createResult.Error);

                var maxSessionsCmd = $"mkdir -p /etc/ssh/sshd_config.d && " +
                                     $"echo 'MaxSessions {maxDevices}' > /etc/ssh/sshd_config.d/{username}.conf && " +
                                     $"systemctl reload sshd 2>/dev/null || service ssh reload 2>/dev/null || true";

                var sessionResult = await RunCommandAsync(client, maxSessionsCmd);

                return (true, createResult.Output, sessionResult.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error creando usuario en VPS: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Output, string Error)> DeleteUserFromVpsAsync(
        Guid nodeId, string username)
    {
        if (!SafeUsername.IsMatch(username))
            return (false, string.Empty, "Nombre de usuario invalido.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var cmd = $"userdel -r {username} 2>/dev/null; " +
                          $"rm -f /etc/ssh/sshd_config.d/{username}.conf; " +
                          $"systemctl reload sshd 2>/dev/null || service ssh reload 2>/dev/null || true; " +
                          $"echo 'OK: Usuario {username} eliminado'";

                var result = await RunCommandAsync(client, cmd);
                return (result.ExitStatus == 0, result.Output, result.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error eliminando usuario del VPS: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Output, string Error)> ExtendUserExpiryOnVpsAsync(
        Guid nodeId, string username, DateTime newExpiryDate)
    {
        if (!SafeUsername.IsMatch(username))
            return (false, string.Empty, "Nombre de usuario invalido.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var expiryStr = newExpiryDate.ToString("yyyy-MM-dd");
                var cmd = $"chage -E {expiryStr} {username} && " +
                          $"echo 'OK: Expiracion extendida hasta {expiryStr}'";

                var result = await RunCommandAsync(client, cmd);
                return (result.ExitStatus == 0, result.Output, result.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error extendiendo expiracion: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Output, string Error)> ChangeUserPasswordOnVpsAsync(
        Guid nodeId, string username, string newPassword)
    {
        if (!SafeUsername.IsMatch(username))
            return (false, string.Empty, "Nombre de usuario invalido.");

        if (!SafePassword.IsMatch(newPassword))
            return (false, string.Empty, "La contraseña no cumple los requisitos de seguridad.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var sanitizedPassword = EscapeShellArg(newPassword);
                var cmd = $"echo '{username}:{sanitizedPassword}' | chpasswd && " +
                          $"echo 'OK: Contraseña actualizada'";

                var result = await RunCommandAsync(client, cmd);
                return (result.ExitStatus == 0, result.Output, result.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error cambiando contraseña: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Output, string Error)> GetUserExpiryAsync(
        Guid nodeId, string username)
    {
        if (!SafeUsername.IsMatch(username))
            return (false, string.Empty, "Nombre de usuario invalido.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var cmd = $"chage -l {username} | grep 'Account expires'";
                var result = await RunCommandAsync(client, cmd);
                return (result.ExitStatus == 0, result.Output, result.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error obteniendo info del usuario: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Output, string Error)> ListExpiredUsersAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        var (client, error) = await ConnectAsync(node, _encryption);
        if (client == null || error != null)
            return (false, string.Empty, error ?? "Error de conexion SSH.");

        using (client)
        {
            try
            {
                var cmd = "awk -F: '$2 != \"*\" && $2 != \"!!\" {print $1}' /etc/shadow | " +
                          "while read user; do " +
                          "  expiry=$(chage -l \"$user\" 2>/dev/null | grep 'Account expires' | cut -d: -f2 | xargs); " +
                          "  if [ \"$expiry\" != \"never\" ] && [ ! -z \"$expiry\" ]; then " +
                          "    exp_epoch=$(date -d \"$expiry\" +%s 2>/dev/null || echo 0); " +
                          "    now_epoch=$(date +%s); " +
                          "    if [ \"$exp_epoch\" -lt \"$now_epoch\" ]; then " +
                          "      echo \"$user\"; " +
                          "    fi; " +
                          "  fi; " +
                          "done";

                var result = await RunCommandAsync(client, cmd);
                return (result.ExitStatus == 0, result.Output, result.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error listando usuarios expirados: {ex.Message}");
            }
        }
    }

    private static async Task<(SshClient? Client, string? Error)> ConnectAsync(VpsNode node, EncryptionService encryption)
    {
        try
        {
            var password = encryption.Decrypt(node.EncryptedPassword);
            var client = new SshClient(node.IP, node.SshPort, "root", password);

            await Task.Run(() => client.Connect());

            if (!client.IsConnected)
                return (null, "No se pudo establecer conexion SSH.");

            return (client, null);
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexion SSH: {ex.Message}");
        }
    }

    private static async Task<ShellCommandResult> RunCommandAsync(SshClient client, string command)
    {
        var cmd = client.CreateCommand(command);
        var output = await Task.Run(() => cmd.Execute());

        return new ShellCommandResult(
            ExitStatus: cmd.ExitStatus ?? -1,
            Output: output,
            Error: cmd.Error
        );
    }

    private static string EscapeShellArg(string arg)
    {
        return arg.Replace("'", "'\\''");
    }

    private record ShellCommandResult(int ExitStatus, string Output, string Error);
}

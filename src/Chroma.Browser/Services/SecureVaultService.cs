using System.Runtime.InteropServices;
using System.Text.Json;
using Chroma.Browser.Models;
using Windows.Security.Credentials.UI;

namespace Chroma.Browser.Services;

public sealed class SecureVaultService
{
    private const int CryptProtectUiForbidden = 0x1;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<bool> RequestUnlockAsync()
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available)
            {
                return false;
            }

            var result = await UserConsentVerifier.RequestVerificationAsync(
                "Подтвердите доступ к паролям Chroma Browser");
            return result == UserConsentVerificationResult.Verified;
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Windows Hello verification failed: {exception.Message}");
            return false;
        }
    }

    public IReadOnlyList<VaultEntry> Load()
    {
        if (!File.Exists(AppPaths.VaultFile))
        {
            return [];
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(AppPaths.VaultFile);
            var bytes = Unprotect(protectedBytes);
            return JsonSerializer.Deserialize<List<VaultEntry>>(bytes, JsonOptions) ?? [];
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Password vault could not be read", exception);
            return [];
        }
    }

    public void Save(IReadOnlyCollection<VaultEntry> entries)
    {
        AppPaths.EnsureCreated();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entries, JsonOptions);
        var protectedBytes = Protect(bytes);
        var temporaryPath = AppPaths.VaultFile + ".tmp";
        File.WriteAllBytes(temporaryPath, protectedBytes);
        File.Move(temporaryPath, AppPaths.VaultFile, true);
    }

    private static byte[] Protect(byte[] bytes) => RunCryptography(bytes, true);
    private static byte[] Unprotect(byte[] bytes) => RunCryptography(bytes, false);

    private static byte[] RunCryptography(byte[] bytes, bool protect)
    {
        var input = CreateBlob(bytes);
        DataBlob output = default;
        try
        {
            var success = protect
                ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output);

            if (!success)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            if (input.Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(input.Data);
            }

            if (output.Data != IntPtr.Zero)
            {
                LocalFree(output.Data);
            }
        }
    }

    private static DataBlob CreateBlob(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return new DataBlob { Size = bytes.Length, Data = pointer };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr prompt,
        int flags,
        out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr prompt,
        int flags,
        out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}


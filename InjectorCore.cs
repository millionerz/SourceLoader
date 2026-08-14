using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace inwdwhg
{
    public static class InjectorCore
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpPage, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;

        public static (bool Success, string Message) InjectStandard(int processId, string dllPath)
        {
            if (processId <= 0)
                return (false, "Неверный PID процесса.");

            if (!System.IO.File.Exists(dllPath))
                return (false, "Файл DLL не найден.");

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
                return (false, "Нет доступа. Запустите от имени Администратора!");

            try
            {
                IntPtr loadLibAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibAddr == IntPtr.Zero)
                    return (false, "Ошибка адреса LoadLibraryA.");

                uint pathLength = (uint)((dllPath.Length + 1) * sizeof(char));
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, pathLength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                    return (false, "Ошибка выделения памяти.");

                byte[] bytes = Encoding.ASCII.GetBytes(dllPath + "\0");
                if (!WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out _))
                    return (false, "Ошибка записи памяти.");

                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibAddr, allocMemAddress, 0, IntPtr.Zero);
                if (hThread == IntPtr.Zero)
                    return (false, "Ошибка создания потока.");

                CloseHandle(hThread);
                return (true, "Инъекция успешно выполнена!");
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }
    }
}
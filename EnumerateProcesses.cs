using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class EnumerateProcesses
    {
        #region APIS
        [DllImport("psapi")]
        private static extern bool EnumProcesses([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)][In][Out] IntPtr[] processIds, UInt32 arraySizeBytes, [MarshalAs(UnmanagedType.U4)] out UInt32 bytesCopied);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, IntPtr dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(IntPtr hProcess,
        [Out] IntPtr lphModule,
        uint cb,
        [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

        [DllImport("psapi.dll")]
        static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        #endregion
        #region ENUMS

        [Flags]
        enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
        #endregion

        static string PrintProcessName(IntPtr processID)
        {
            string sName = "";
            bool bFound = false;
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead, false, processID);
            if (hProcess != IntPtr.Zero)
            {
                StringBuilder szProcessName = new StringBuilder(260);
                IntPtr hMod = IntPtr.Zero;
                uint cbNeeded = 0;
                EnumProcessModules(hProcess, hMod, (uint)Marshal.SizeOf(typeof(IntPtr)), out cbNeeded);
                if (GetModuleBaseName(hProcess, hMod, szProcessName, szProcessName.Capacity) > 0)
                {
                    sName = szProcessName.ToString();
                    bFound = true;
                }

                // Close the process handle
                CloseHandle(hProcess);
            }
            if (!bFound)
            {
                sName = "<unknown>";
            }
            return sName;
        }
        public static void Testy()
        {
            UInt32 arraySize = 9000;
            UInt32 arrayBytesSize = arraySize * sizeof(UInt32);
            IntPtr[] processIds = new IntPtr[arraySize];
            UInt32 bytesCopied;

            bool success = EnumProcesses(processIds, arrayBytesSize, out bytesCopied);

            Console.WriteLine("success={0}", success);
            Console.WriteLine("bytesCopied={0}", bytesCopied);

            if (!success)
            {
                Console.WriteLine("Boo!");
                return;
            }
            if (0 == bytesCopied)
            {
                Console.WriteLine("Nobody home!");
                return;
            }

            UInt32 numIdsCopied = bytesCopied >> 2; ;

            if (0 != (bytesCopied & 3))
            {
                UInt32 partialDwordBytes = bytesCopied & 3;

                Console.WriteLine("EnumProcesses copied {0} and {1}/4th DWORDS...  Please ask it for the other {2}/4th DWORD",
                    numIdsCopied, partialDwordBytes, 4 - partialDwordBytes);
                return;
            }

            for (UInt32 index = 0; index < numIdsCopied; index++)
            {
                string sName = PrintProcessName(processIds[index]);
                IntPtr PID = processIds[index];
                Console.WriteLine("Name '" + sName + "' PID '" + PID + "'");
                if (sName.ToLower().Equals("WeChat.exe".ToLower()))
                {
                    IntPtr WX_PID = PID;
                    Console.WriteLine("WX_Name '" + sName + "' PID '" + PID + "'");
                }
            }
        }
    }
}

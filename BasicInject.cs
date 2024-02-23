using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public class BasicInject
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        public static IntPtr Main1()
        {
            // name of the dll we want to inject
            string dllName = "wxhelper.dll";
            string dllpath = System.Environment.CurrentDirectory + "\\" + dllName;
            bool flag1 = !File.Exists(dllpath);
            if (flag1)
            {
                MessageBox.Show("被注入的DLL文件(" + dllpath + ")不存在！\n请把被注入的DLL文件放在本程序所在目录下。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return IntPtr.Zero;
            }
            // the target process - I'm using a dummy process for this
            // if you don't have one, open Task Manager and choose wisely
            //Process targetProcess = Process.GetProcessesByName("WeChat")[0];
            Process[] processes = Process.GetProcesses();
            Process process = null;
            foreach (Process process2 in processes)
            {
                bool flag2 = process2.ProcessName.ToLower() == "WeChat".ToLower();
                if (flag2)
                {
                    process = process2;
                    foreach (object obj in process.Modules)
                    {
                        ProcessModule processModule = (ProcessModule)obj;
                        bool flag3 = processModule.ModuleName == dllName;
                        if (flag3)
                        {
                            MessageBox.Show("DLL文件“" + dllpath + "”之前已注入!\n\n若要重新注入，请先重启微信!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            return IntPtr.Zero;
                        }
                    }
                    break;
                }
            }
            bool flag4 = process == null;
            if (flag4)
            {
                MessageBox.Show("注入前请先启动微信！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return IntPtr.Zero;
            }
            // geting the handle of the process - with required privileges
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);
            if (procHandle == IntPtr.Zero)
            {
                MessageBox.Show("获取微信线程句柄失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return IntPtr.Zero;
            }
            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            bool flag5 = loadLibraryAddr == IntPtr.Zero;
            if (flag5)
            {
                loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
                flag5 = loadLibraryAddr == IntPtr.Zero;
                if (flag5)
                {
                    MessageBox.Show("查找LoadLibraryA或LoadLibraryW地址失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return IntPtr.Zero;
                } 
            }

            // alocating some memory on the target process - enough to store the name of the dll
            // and storing its address in a pointer  
            //参数procHandle换成process.Handle也是一样
            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllpath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            bool flag6 = allocMemAddress == IntPtr.Zero;
            if (flag6)
            {
                MessageBox.Show("内存分配失败！");
                return IntPtr.Zero;
            }
            // writing the name of the dll there
            UIntPtr bytesWritten;
            bool flag7 = WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllpath), (uint)((dllpath.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);
            if (!flag7)
            {
                MessageBox.Show("内存写入失败！");
                return IntPtr.Zero;
            }
            // creating a thread that will call LoadLibraryA with allocMemAddress as argument
            IntPtr retthreadid = IntPtr.Zero;
            IntPtr retptr = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out retthreadid);
            if (retptr== IntPtr.Zero)
            {
                MessageBox.Show("执行远程线程失败！");
                return IntPtr.Zero;
            }
            MessageBox.Show("注入成功！");
            return retptr;
        }
    }
}

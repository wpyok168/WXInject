using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll")]
        public static extern int VirtualAllocEx(IntPtr hwnd, int lpaddress, int size, int type, int tect);
        [DllImport("kernel32.dll")]
        public static extern int WriteProcessMemory(IntPtr hwnd, int baseaddress, string buffer, int nsize, int filewriten); //错的
        [DllImport("kernel32.dll")]
        public static extern Boolean WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hwnd, string lpname);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll")]
        public static extern int CreateRemoteThread(IntPtr hwnd, int attrib, int size, int address, IntPtr par, int flags, ref int threadid);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const int MEM_COMMIT = 0x00001000;
        const int MEM_RESERVE = 0x00002000;
        const int PAGE_READWRITE = 4;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //ZRWX();
            //EnumerateProcesses.Testy();
            BasicInject.Main1();
            
        }

        public void Msg(string msg) 
        {
            this.textBox1.AppendText(msg + Environment.NewLine);
        }
        
        private void ZRWX()
        {
            

            //int moduleHandleA1 = Form1.GetModuleHandle("kernel32.dll");
            //IntPtr userApi = LoadLibrary("C:\\Windows\\SysWOW64\\kernel32.dll");
            //IntPtr procAddress1 = Form1.GetProcAddress(moduleHandleA1, "LoadLibraryW");
            string dllpath = System.Environment.CurrentDirectory + "\\wxhelper.dll";
            Process[] processes = Process.GetProcesses();
            Process process = null;
            foreach (Process process2 in processes)
            {
                bool flag = process2.ProcessName.ToLower() == "WeChat".ToLower();
                if (flag)
                {
                    process = process2;
                    foreach (object obj in process.Modules)
                    {
                        ProcessModule processModule = (ProcessModule)obj;
                        bool flag2 = processModule.ModuleName == "WeChat.exe";
                        if (flag2)
                        {
                            MessageBox.Show("DLL文件“" + dllpath + "”之前已注入!\n\n若要重新注入，请先重启微信!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            return;
                        }
                    }
                    break;
                }
            }
            bool flag3 = process == null;
            if (flag3)
            {
                MessageBox.Show("注入前请先启动微信！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);
            bool flag6 = dllpath == null || dllpath == "";
            if (flag6)
            {
                MessageBox.Show("没找到被注入的DLL文件！\n请把被注入的DLL文件放在本程序所在目录下。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            string fullPath = Path.GetFullPath(dllpath);
            bool flag7 = !File.Exists(fullPath);
            if (flag7)
            {
                MessageBox.Show("被注入的DLL文件(" + fullPath + ")不存在！\n请把被注入的DLL文件放在本程序所在目录下。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            //int num = fullPath.Length * 2 + 1;
            int num = (fullPath.Length * 1) + Marshal.SizeOf(typeof(char)); 
            int num2 = 4096;
            int num3 = 4;
            int num4 = Form1.VirtualAllocEx(procHandle, 0, num, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            bool flag8 = num4 == 0;
            if (flag8)
            {
                MessageBox.Show("内存分配失败！");
                return;
            }
            this.textBox1.AppendText("内存地址:\t0x" + num4.ToString("X8") + Environment.NewLine);
            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllpath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            UIntPtr retnum = UIntPtr.Zero;
            bool flag9 = Form1.WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(fullPath), (uint)num, out retnum);
            //static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
            if (!flag9)
            {
                MessageBox.Show("内存写入失败！");
                return;
            }
            IntPtr moduleHandleA = GetModuleHandle("Kernel32.dll");
            IntPtr procAddress = GetProcAddress(moduleHandleA, "LoadLibraryA");
            bool flag10 = procAddress == IntPtr.Zero;
            if (flag10)
            {
                MessageBox.Show("查找LoadLibraryW地址失败！","错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            bool flag11 = CreateRemoteThread(procHandle, IntPtr.Zero, 0, procAddress, allocMemAddress, 0, IntPtr.Zero)== IntPtr.Zero;
            if (flag11)
            {
                MessageBox.Show("执行远程线程失败！");
                return;
            }
            this.textBox1.AppendText("成功注入:\t" + dllpath + Environment.NewLine);
        }

    }
}

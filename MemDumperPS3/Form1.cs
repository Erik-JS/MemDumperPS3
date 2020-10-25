using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MemDumperPS3.NativeMethods;

namespace MemDumperPS3
{
    public partial class Form1 : Form
    {
        public struct ColoredItem
        {
            public Color bgColor;
            public Color fgColor;
            public string text;
        }

        public static Form1 mainForm;

        public Form1()
        {
            InitializeComponent();
            mainForm = this;
        }

        public static void Log(string text, Color color)
        {
            mainForm.Invoke((MethodInvoker)delegate
               {
                   mainForm.listBox1.Items.Add(new ColoredItem { fgColor = color, text = text });
               }
            );
        }

        public static string GetSaveFile()
        {
            string filename = "";
            mainForm.Invoke((MethodInvoker)delegate
            {
                var sfd = new SaveFileDialog();
                sfd.Filter = "BIN|*.bin|*.*|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                    filename = sfd.FileName;
            }
            );
            return filename;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;
            ListBox lb = (ListBox)sender;
            ColoredItem ci = (ColoredItem)lb.Items[e.Index];
            e.DrawBackground();
            Color drawColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : ci.fgColor;
            Brush fgBrush = new SolidBrush(drawColor);
            e.Graphics.DrawString(ci.text, e.Font, fgBrush, e.Bounds, StringFormat.GenericDefault);
            e.DrawFocusRectangle();
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            var dumpThread = new Thread(DumpMemThread);
            btnDump.Enabled = false;
            chkSwap.Enabled = false;
            dumpThread.Start(chkSwap.Checked);
            while(dumpThread.IsAlive)
            {
                Thread.Sleep(100);
                Application.DoEvents();
            }
            btnDump.Enabled = true;
            chkSwap.Enabled = true;
            GC.Collect();
        }

        public static void DumpMemThread(object param)
        {
            bool swapflag = (bool)param;
            var emuProcess = Process.GetProcessesByName("rpcs3").FirstOrDefault();
            if (emuProcess == null)
            {
                Log("RPCS3 is not running.", Color.Red);
                return;
            }
            Log("RPCS3 PID: " + emuProcess.Id.ToString(), Color.Blue);
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.All, false, (uint)emuProcess.Id);
            if (hProcess == IntPtr.Zero)
            {
                Log("OpenProcess has failed.", Color.Red);
                return;
            }
            Log("Reading game's RAM from RPCS3...", Color.Blue);
            byte[] gameMem = new byte[0x10000000];
            ReadProcessMemory(hProcess, (IntPtr)0x330000000, gameMem, 0x10000000, IntPtr.Zero);

            if (swapflag)
            {
                Log("Swapping bytes...", Color.Blue);
                gameMem = ByteSwapper.ByteSwap(gameMem, 4);
            }


            string dumpfilename = GetSaveFile();
            if(dumpfilename == "")
            {
                Log("SaveFileDialog cancelled. Dump wasn't saved.", Color.Red);
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(new byte[0x30000000], 0, 0x30000000);
                ms.Write(gameMem, 0, 0x10000000);
                File.WriteAllBytes(dumpfilename, ms.ToArray());
            }
            Log("Done.", Color.Green);
            
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AppMolest
{
    class Program
    {
        public class MolestHandler
        {
            public class ProcessMonitored
            {
                public readonly MolestHandler Owner;
                bool focused;
                public bool Focused
                {
                    get { return focused; }
                    set
                    {
                        if (value)
                        {
                            Owner.OnMolested();
                        }
                        focused = value;
                    }
                }

                public ProcessMonitored(MolestHandler owner)
                {
                    this.Owner = owner;
                }
            }
            Dictionary<int, ProcessMonitored> monitoreds = new Dictionary<int, ProcessMonitored>();
            IntPtr lastFocusedWindow = IntPtr.Zero;
            uint lastFocusedPid = 0;
            Random rand = new Random();

            [DllImport("winmm.dll")]
            static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

            public MolestHandler()
            {
                // Calculate the volume that's being set. BTW: this is a trackbar!
                int NewVolume = ((ushort.MaxValue / 10) * 1);
                // Set the same volume for both the left and the right channels
                uint NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
                // Set the volume
                waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
            }

            public void OnMolested()
            {
                Console.WriteLine("arah!");
                var wavfiles = Directory.GetFiles(@"Voices\").Where(x => x.EndsWith(".wav")).ToArray();
                string wav = wavfiles[rand.Next() % wavfiles.Length];

                var player = new System.Media.SoundPlayer();
                player.SoundLocation = wav;
                player.Play();
            }

            [DllImport("user32.dll")]
            static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll", ExactSpelling = true)]
            internal static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestor_Flags gaFlags);
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern int GetWindowTextLength(IntPtr hWnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
            internal enum GetAncestor_Flags
            {
                GetParent = 1,
                GetRoot = 2,
                GetRootOwner = 3
            }
            public static string GetText(IntPtr hWnd)
            {
                // Allocate correct string length first
                int length = GetWindowTextLength(hWnd);
                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                return sb.ToString();
            }

            public void Update()
            {
                if (lastFocusedWindow == GetForegroundWindow())
                    return;
                    //Console.WriteLine(GetForegroundWindow().ToString());
                    //Console.WriteLine(GetText(GetForegroundWindow()));
                    //uint pid=0;
                    //GetWindowThreadProcessId(GetForegroundWindow(), out pid);
                    //Console.WriteLine(pid);
                lastFocusedWindow = GetForegroundWindow();
                lastFocusedPid = 0;
                GetWindowThreadProcessId(lastFocusedWindow, out lastFocusedPid);

                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    if (!p.Responding
                        || p.ProcessName.ToLower() == "werfault"
                        || p.ProcessName.ToLower() == "dwm")
                    {
                        if (!monitoreds.Keys.Any(x => x == p.Id))
                            monitoreds.Add(p.Id, new ProcessMonitored(this));
                    }
                }

                for (int i = 0; i < monitoreds.Count; i++)
                {
                    Process p = null;
                    try
                    {
                        p = Process.GetProcessById(monitoreds.Keys.ElementAt(i));
                    }
                    catch 
                    {
                    }
                    if (p != null)
                    {
                        monitoreds[p.Id].Focused = lastFocusedPid == p.Id; // GetAncestor(p.MainWindowHandle, GetAncestor_Flags.GetRoot);
                    }
                    else
                    {
                        monitoreds.Remove(monitoreds.Keys.ElementAt(i));
                        i--;
                        continue;
                    }

                }
            }
            public void Run()
            {
                for (; ; )
                {
                    System.Threading.Thread.Sleep(10);
                    Update();
                }
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("monitoring.. for nonresponding windows..");
            new MolestHandler().Run();
        }
    }
}

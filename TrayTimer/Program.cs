using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrayTimer
{
    static class Program
    {

        public sealed class Chronometer
        {
            private static volatile Chronometer instance;
            private static object syncRoot = new Object();

            private System.Timers.Timer timer = new System.Timers.Timer();
            private System.Timers.Timer updateTimer = new System.Timers.Timer();
            private DateTime targetTime = DateTime.Now;

            public NotifyIcon notifyIcon;

            public void AddToTimer(int minutes)
            {
                if (minutes < 0) return;

                timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Ended);
                timer.Interval = minutes * 60 * 1000;
                timer.AutoReset = false;
                targetTime = DateTime.Now.AddMinutes(minutes);
                timer.Start();

                // check the +=
                updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(Update_Tick);
                updateTimer.Interval = 1000;
                updateTimer.AutoReset = true;
                updateTimer.Start();

                UpdateNotification();
            }

            private static void Update_Tick(object source, System.Timers.ElapsedEventArgs e)
            {
                Chronometer.instance.UpdateNotification();
            }

            private static void Timer_Ended(object source, System.Timers.ElapsedEventArgs e)
            {
                Chronometer.instance.updateTimer.AutoReset = false;
                Chronometer.instance.updateTimer.Stop();

                Chronometer.instance.UpdateNotification();
                System.Windows.Forms.MessageBox.Show("Time's up!", "TrayTimer");
            }

            public string getStateName()
            {
                TimeSpan t = DateTime.Now - targetTime;
                if(t.TotalMinutes < 0)
                {
                    return Math.Truncate(t.TotalMinutes * -1).ToString() + " minutes remaining";
                }
                return "Idle";
            }

            private ContextMenuStrip GetContext()
            {
                ContextMenuStrip CMS = new ContextMenuStrip();
                CMS.Items.Add(new ToolStripLabel(getStateName()));
                CMS.Items.Add(new ToolStripSeparator());
                CMS.Items.Add("25 minutes", null, new EventHandler(Timer_Click));
                CMS.Items.Add("15 minutes", null, new EventHandler(Timer_Click));
                CMS.Items.Add("5 minutes", null, new EventHandler(Timer_Click));
                //CMS.Items.Add("Custom time...", null, new EventHandler(Timer_Click));
                //CMS.Items.Add(new ToolStripSeparator());
                //CMS.Items.Add("Settings...", null, new EventHandler(Exit_Click));
                CMS.Items.Add(new ToolStripSeparator());
                CMS.Items.Add("Exit", null, new EventHandler(Exit_Click));
                return CMS;
            }

            public void UpdateNotification()
            {
                notifyIcon.ContextMenuStrip = GetContext();
                notifyIcon.Icon = new System.Drawing.Icon("1.ico");
                notifyIcon.Text = "Tray Timer ("+ getStateName() + ")";
                notifyIcon.Visible = true;
            }

            public static Chronometer Instance
            {
                get
                {
                    if (instance == null)
                    {
                        lock (syncRoot)
                        {
                            if (instance == null)
                                instance = new Chronometer();
                        }
                    }
                    return instance;
                }
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Chronometer.Instance.notifyIcon = new NotifyIcon();
            Chronometer.Instance.UpdateNotification();

            Application.Run();
        }

        private static void Timer_Click(object sender, EventArgs e)
        {
            int minutes = 0;
            if (Int32.TryParse(sender.ToString().Split(' ')[0], out minutes))
            {
                Chronometer.Instance.AddToTimer(minutes);
            }
        }

        private static void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

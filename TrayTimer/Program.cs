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
            enum stateType { IDLE, RUNNING, PAUSED };

            private System.Timers.Timer timer;
            private System.Timers.Timer updateTimer;

            private DateTime targetTime = DateTime.Now;
            public NotifyIcon notifyIcon;

            private stateType State { get; set; }

            private int pauseRemainingMilliseconds;

            public Chronometer() => State = stateType.IDLE;

            public void AddMilliseconds(int milliseconds)
            {
                if (milliseconds <= 0) return;
                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Ended);
                targetTime = DateTime.Now.AddMilliseconds(milliseconds);
                timer.Interval = milliseconds;
                timer.AutoReset = false;
                timer.Start();

                updateTimer = new System.Timers.Timer();
                updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(Update_Tick);
                updateTimer.Interval = 1000;
                updateTimer.AutoReset = true;
                updateTimer.Start();

                State = stateType.RUNNING;
                UpdateTray();
            }

            public void AddMinutes(int minutes) => AddMilliseconds(minutes * 60 * 1000);

            private static void Update_Tick(object source, System.Timers.ElapsedEventArgs e) => Chronometer.instance.UpdateTray();

            private static void Timer_Ended(object source, System.Timers.ElapsedEventArgs e)
            {
                Chronometer.instance.Stop();
                System.Windows.Forms.MessageBox.Show("Time's up!", "TrayTimer");
            }

            public int GetTimeRemaining() => Convert.ToInt32(Math.Truncate((DateTime.Now - targetTime).TotalMinutes * -1));

            public string GetStateName()
            {
                switch (State) {
                    case stateType.RUNNING:
                        return GetTimeRemaining() + " minutes remaining"; ;
                    case stateType.PAUSED:
                        return "[Paused] "+ GetTimeRemaining() + " minutes remaining";
                }
                return "Idle";
            }

            private ContextMenuStrip GetContext()
            {
                ContextMenuStrip CMS = new ContextMenuStrip();
                CMS.Items.Add(new ToolStripLabel(GetStateName()));
                CMS.Items.Add(new ToolStripSeparator());
                if (State == stateType.IDLE)
                {
                    CMS.Items.Add("25 minutes", null, new EventHandler(Timer_Click));
                    CMS.Items.Add("15 minutes", null, new EventHandler(Timer_Click));
                    CMS.Items.Add("5 minutes", null, new EventHandler(Timer_Click));
                }
                else if (State == stateType.RUNNING)
                {
                    CMS.Items.Add("Stop", null, new EventHandler(Stop_Click));
                    CMS.Items.Add("Pause", null, new EventHandler(Pause_Click));
                    CMS.Items.Add("5 more minutes", null, new EventHandler(Timer_Click));
                }
                else // pause
                {
                    CMS.Items.Add("Resume", null, new EventHandler(Pause_Click));
                }
                CMS.Items.Add(new ToolStripSeparator());
                CMS.Items.Add("Exit", null, new EventHandler(Exit_Click));
                return CMS;
            }

            public void UpdateTray()
            {
                string iconfile = "icon-small.ico";
                notifyIcon.ContextMenuStrip = GetContext();
                if (State == stateType.RUNNING)
                {
                    iconfile = (GetTimeRemaining() < 60 ? GetTimeRemaining().ToString() : "more") + ".ico";
                }
                else if (State == stateType.PAUSED)
                    iconfile = "paused.ico";
                notifyIcon.Icon = new System.Drawing.Icon(iconfile);
                notifyIcon.Text = "Tray Timer ("+ GetStateName() + ")";
                notifyIcon.Visible = true;
            }

            public static Chronometer Instance
            {
                get
                {
                    if (instance == null)
                    {                    lock (syncRoot)
                        {
                            if (instance == null)
                                instance = new Chronometer();
                        }
                    }
                    return instance;
                }
            }

            public void Stop()
            {
                timer.AutoReset = false;
                timer.Stop();
                updateTimer.AutoReset = false;
                updateTimer.Stop();
                State = stateType.IDLE;
                Chronometer.instance.UpdateTray();
            }

            public void TogglePause()
            {
                if (State == stateType.RUNNING)
                {
                    timer.AutoReset = false;
                    timer.Stop();
                    updateTimer.AutoReset = false;
                    updateTimer.Stop();
                    pauseRemainingMilliseconds = (DateTime.Now - targetTime).Milliseconds;
                    State = stateType.PAUSED;
                }
                else if (State == stateType.PAUSED)
                {
                    AddMilliseconds(pauseRemainingMilliseconds);
                }
                Chronometer.instance.UpdateTray();
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Chronometer.Instance.notifyIcon = new NotifyIcon();
            Chronometer.Instance.UpdateTray();

            Application.Run();
        }

        private static void Timer_Click(object sender, EventArgs e)
        {
            int minutes = 0;
            if (Int32.TryParse(sender.ToString().Split(' ')[0], out minutes))
            {
                Chronometer.Instance.AddMinutes(minutes);
            }
        }

        private static void Stop_Click(object sender, EventArgs e) => Chronometer.Instance.Stop();

        private static void Pause_Click(object sender, EventArgs e) => Chronometer.Instance.TogglePause();

        private static void Exit_Click(object sender, EventArgs e) => Application.Exit();
    }
}

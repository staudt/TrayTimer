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
            enum StateType { IDLE, RUNNING, PAUSED };

            private System.Timers.Timer timer;
            private System.Timers.Timer updateTimer;

            private DateTime targetTime = DateTime.Now;
            public NotifyIcon notifyIcon;

            private StateType State { get; set; }

            private int PauseRemainingMilliseconds = 0;

            public Chronometer() => State = StateType.IDLE;

            public void AddMilliseconds(int milliseconds)
            {
                ClearTimers();
                if (milliseconds <= 0) return;
                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Ended);
                targetTime = targetTime.AddMilliseconds(milliseconds);
                timer.Interval = (DateTime.Now - targetTime).TotalMilliseconds * -1;
                timer.AutoReset = false;
                timer.Start();

                updateTimer = new System.Timers.Timer();
                updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(Update_Tick);
                updateTimer.Interval = 1000;
                updateTimer.AutoReset = true;
                updateTimer.Start();

                State = StateType.RUNNING;
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
                    case StateType.RUNNING:
                        return GetTimeRemaining()+1 + " minutes remaining"; ;
                    case StateType.PAUSED:
                        return "[Paused] " + GetTimeRemaining()+1 + " minutes remaining";
                }
                return "Idle";
            }

            private ContextMenuStrip GetContext()
            {
                ContextMenuStrip CMS = new ContextMenuStrip();
                CMS.Items.Add(new ToolStripLabel(GetStateName()));
                CMS.Items.Add(new ToolStripSeparator());
                if (State == StateType.IDLE)
                {
                    CMS.Items.Add("25 minutes", null, new EventHandler(Timer_Click));
                    CMS.Items.Add("15 minutes", null, new EventHandler(Timer_Click));
                    CMS.Items.Add("5 minutes", null, new EventHandler(Timer_Click));
                }
                else if (State == StateType.RUNNING)
                {
                    CMS.Items.Add("Stop", null, new EventHandler(Stop_Click));
                    CMS.Items.Add("Pause", null, new EventHandler(Pause_Click));
                    CMS.Items.Add("5 more minutes", null, new EventHandler(Timer_Click));
                }
                else // pause
                {
                    CMS.Items.Add("Resume", null, new EventHandler(Resume_Click));
                }
                CMS.Items.Add(new ToolStripSeparator());
                CMS.Items.Add("Exit", null, new EventHandler(Exit_Click));
                return CMS;
            }

            public void UpdateTray()
            {
                string iconfile = "icon-small.ico";
                notifyIcon.ContextMenuStrip = GetContext();
                if (State == StateType.RUNNING)
                {
                    iconfile = (GetTimeRemaining() < 60 ? (GetTimeRemaining()+1).ToString() : "more") + ".ico";
                }
                else if (State == StateType.PAUSED)
                    iconfile = "paused.ico";
                notifyIcon.Icon = new System.Drawing.Icon(iconfile);
                notifyIcon.Text = "Tray Timer (" + GetStateName() + ")";
                notifyIcon.Visible = true;
            }

            public static Chronometer Instance
            {
                get
                {
                    if (instance == null)
                    { lock (syncRoot)
                        {
                            if (instance == null)
                                instance = new Chronometer();
                        }
                    }
                    return instance;
                }
            }

            public void ClearTimers()
            {
                if (timer != null)
                {
                    timer.AutoReset = false;
                    timer.Stop();
                }
                if (updateTimer != null)
                {
                    updateTimer.AutoReset = false;
                    updateTimer.Stop();
                }
            }

            public void Stop()
            {
                ClearTimers();
                State = StateType.IDLE;
                Chronometer.instance.UpdateTray();
            }

            public void Pause()
            {
                PauseRemainingMilliseconds = (DateTime.Now - targetTime).Milliseconds * -1;
                ClearTimers();
                State = StateType.PAUSED;
                Chronometer.instance.UpdateTray();
            }

            public void Resume()
            {
                AddMilliseconds(PauseRemainingMilliseconds);
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

        private static void Pause_Click(object sender, EventArgs e) => Chronometer.Instance.Pause();

        private static void Resume_Click(object sender, EventArgs e) => Chronometer.Instance.Resume();

        private static void Exit_Click(object sender, EventArgs e) => Application.Exit();
    }
}

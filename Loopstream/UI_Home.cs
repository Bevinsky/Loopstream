﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/*
 * TODO before release:
 * 
 * Program.debug = false
 * DFC.CHECK_MD5 = true
 * LSSettings ver.check
 * assembly version
 * Loopstream.exe sign
 */

namespace Loopstream
{
    public partial class Home : Form
    {
        public Home()
        {
            invals = new Control[0];
            InitializeComponent();
            splash = new Splesh();
            splash.Show();
        }

        bool isPresetLoad;
        LSSettings settings;
        Splesh splash;
        Rectangle myBounds;
        LSMixer mixer;
        LSPcmFeed pcm;
        LSTag tag;
        Control[] invals; //sorry

        private void Form1_Load(object sender, EventArgs e)
        {
            myBounds = this.Bounds;
            this.Bounds = new Rectangle(0, -100, 0, 0);
            this.Icon = Program.icon;

            Timer t = new Timer();
            t.Tick += t_Tick;
            t.Interval = 100;
            t.Start();
        }

        void t_Tick(object sender, EventArgs e)
        {
            ((Timer)sender).Stop();
            this.Text += " v" + Application.ProductVersion;
            
            DFC.coreTest();
            if (Directory.Exists(@"..\..\tools\"))
            {
                splash.vis();
                new DFC().make(splash.pb);
            }
            if (!Directory.Exists(Program.tools))
            {
                splash.vis();
                new DFC().extract(splash.pb);
            }

            settings = LSSettings.load();
            settings.runTests(splash);
            isPresetLoad = true;

            gMusic.valueChanged += gSlider_valueChanged;
            gMic.valueChanged += gSlider_valueChanged;
            gSpeed.valueChanged += gSlider_valueChanged;
            gOut.valueChanged += gSlider_valueChanged;
            mixerPresetChanged(sender, e);

            Program.ni = new NotifyIcon();
            NotifyIcon ni = Program.ni;
            ni.Icon = this.Icon;
            ni.Visible = true;
            ni.DoubleClick += ni_DoubleClick;
            MenuItem[] items = {
                new MenuItem("Hide", ni_DoubleClick),
                new MenuItem("Connect", gConnect_Click),
                new MenuItem("---"),
                new MenuItem("A (1st preset)", gPreset_Click),
                new MenuItem("B (2nd preset)", gPreset_Click),
                new MenuItem("C (3nd impact)", gPreset_Click),
                new MenuItem("D (4th preset)", gPreset_Click),
                new MenuItem("---"),
                new MenuItem("Exit", gExit_Click)
            };
            ni.ContextMenu = new ContextMenu(items);

            /*
             *  Not using ContextMenuStrip because
             *  it's an unresponsive little shit
             * 
            *ContextMenuStrip cm = new ContextMenuStrip();
            ni.ContextMenuStrip = cm;
            ToolStripItem iShow = new ToolStripLabel("Hide");
            ToolStripItem iConn = new ToolStripLabel("Connect");
            ToolStripItem iA = new ToolStripLabel("A (1st preset)");
            ToolStripItem iB = new ToolStripLabel("B (2nd preset)");
            ToolStripItem iC = new ToolStripLabel("C (3nd impact)");
            ToolStripItem iD = new ToolStripLabel("D (4th preset)");
            ToolStripItem iExit = new ToolStripLabel("Exit");
            iShow.Click += ni_DoubleClick;
            iConn.Click += gConnect_Click;
            iExit.Click += gExit_Click;
            iA.Click += gPreset_Click;
            iB.Click += gPreset_Click;
            iC.Click += gPreset_Click;
            iD.Click += gPreset_Click;
            cm.Items.Add(iShow);
            cm.Items.Add(iConn);
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add(iA);
            cm.Items.Add(iB);
            cm.Items.Add(iC);
            cm.Items.Add(iD);
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add(iExit);*/

            this.Bounds = myBounds;
            splash.Focus();
            //splash.BringToFront();
            splash.fx = settings.splash;
            splash.gtfo();

            if (settings.autohide)
            {
                this.Visible = false;
            }
            if (settings.autoconn)
            {
                gConnect_Click(sender, e);
            }

            // please don't look
            invals = new Control[] {
                box_top_graden,
                box_bottom_graden,
                gMusic.giSlider,
                gMusic.graden1,
                gMusic.graden2,
                gMic.giSlider,
                gMic.graden1,
                gMic.graden2,
                gSpeed.giSlider,
                gSpeed.graden1,
                gSpeed.graden2,
                gOut.giSlider,
                gOut.graden1,
                gOut.graden2,
            };
            invalOnNext = false;

            Timer tTitle = new Timer();
            tTitle.Tick += tTitle_Tick;
            tTitle.Interval = 200;
            tTitle.Start();
        }

        bool invalOnNext;
        void inval()
        {
            foreach (Control c in invals)
            {
                c.Invalidate();
            }
            /*foreach (Control c in controls)
            {
                if (c.GetType() == typeof(LLabel) ||
                    c.GetType() == typeof(Graden))
                {
                    c.Invalidate();
                }
                //this.InvokePaintBackground(c, new PaintEventArgs(c.CreateGraphics(), c.Bounds));
                //this.InvokePaint(c, new PaintEventArgs(c.CreateGraphics(), c.Bounds));
                inval(c.Controls);
            }*/
        }

        void ni_DoubleClick(object sender, EventArgs e)
        {
            this.Visible = !this.Visible;
            //Program.ni.ContextMenuStrip.Items[0].Text = this.Visible ? "Hide" : "Show";
            Program.ni.ContextMenu.MenuItems[0].Text = this.Visible ? "Hide" : "Show";
        }

        void gSlider_valueChanged(object sender, EventArgs e)
        {
            settings.mixer.vRec = gMusic.level / 255.0;
            settings.mixer.vMic = gMic.level / 255.0;
            settings.mixer.vSpd = gSpeed.level / 200.0;
            settings.mixer.vOut = gOut.level / 255.0;
            settings.mixer.bRec = gMusic.enabled;
            settings.mixer.bMic = gMic.enabled;
            settings.mixer.bOut = gOut.enabled;

            if (mixer != null)
            {
                mixer.MuteChannel(LSMixer.Slider.Music, settings.mixer.bRec);
                mixer.MuteChannel(LSMixer.Slider.Mic, settings.mixer.bMic);
                mixer.MuteChannel(LSMixer.Slider.Out, settings.mixer.bOut);

                LSMixer.Slider sl = LSMixer.Slider.Out;
                if (sender == gSpeed) return;
                if (sender == gMusic) sl = LSMixer.Slider.Music;
                if (sender == gMic) sl = LSMixer.Slider.Mic;
                double dur = 0;
                if (((Verter)sender).eventType == Verter.EventType.slide)
                {
                    //dur = (float)(settings.mixer.vSpd);
                    dur = settings.mixer.vSpd;
                }
                if (((Verter)sender).eventType == Verter.EventType.mute)
                {
                    mixer.MuteChannel(sl, ((Verter)sender).enabled);
                }
                else
                {
                    mixer.FadeVolume(sl, (float)(((Verter)sender).level / 255.0), dur);
                }
            }
        }

        void mixerPresetChanged(object sender, EventArgs e)
        {
            gMusic.level = (int)(255 * settings.mixer.vRec);
            gMic.level = (int)(255 * settings.mixer.vMic);
            gSpeed.level = (int)(200 * settings.mixer.vSpd);
            gOut.level = (int)(255 * settings.mixer.vOut);
            gMusic.enabled = settings.mixer.bRec;
            gMic.enabled = settings.mixer.bMic;
            gOut.enabled = settings.mixer.bOut;

            // you should probably fix this
            if (mixer != null)
            {
                gMusic.eventType = Verter.EventType.slide;
                gMic.eventType = Verter.EventType.slide;
                gOut.eventType = Verter.EventType.slide;
                gSlider_valueChanged(gMusic, null);
                gSlider_valueChanged(gMic, null);
                gSlider_valueChanged(gOut, null);
            }
        }

        private void gConnect_Click(object sender, EventArgs e)
        {
            if (settings.devRec == null || settings.devOut == null)
            {
                if (DialogResult.OK == MessageBox.Show(
                    "Please take a minute to adjust your settings\r\n\r\n(soundcard and radio server)",
                    "Audio endpoint is null", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning))
                {
                    gSettings_Click(sender, e);

                    if (settings.devRec == null || settings.devOut == null)
                    {
                        MessageBox.Show("Config is still invalid.\r\n\r\nGiving up.", "Crit", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else return;
            }
            if (gConnect.Text == "Connect")
            {
                gConnect.Text = "D I S C O N N E C T";
                tag = new LSTag(settings);
                mixer = new LSMixer(settings);
                pcm = new LSPcmFeed(settings, mixer.lameOutlet);
            }
            else
            {
                gConnect.Text = "Connect";
                mixer.Dispose();
                pcm.Dispose();
            }
            //Program.ni.ContextMenuStrip.Items[1].Text = gConnect.Text == "Connect" ? "Connect" : "Disconnect";
            Program.ni.ContextMenu.MenuItems[1].Text = gConnect.Text == "Connect" ? "Connect" : "Disconnect";
        }

        private void gSettings_Click(object sender, EventArgs e)
        {
            this.Hide();
            new ConfigSC(settings).ShowDialog();
            this.Show();
        }

        private void gExit_Click(object sender, EventArgs e)
        {
            Program.kill();
        }

        private void gLoad_Click(object sender, EventArgs e)
        {
            isPresetLoad = !isPresetLoad;
            if (isPresetLoad)
            {
                gLoad.Text = "Load preset";
            }
            else
            {
                gLoad.Text = "[SAVE] preset";
            }
        }

        private void gPreset_Click(object sender, EventArgs e)
        {
            int preset = sender.GetType() == typeof(Button) ?
                ((Control)sender).Text[0] - 'A' :
                ((MenuItem)sender).Text[0] - 'A';

            if (isPresetLoad)
            {
                settings.mixer.apply(settings.presets[preset]);
                mixerPresetChanged(sender, e);
            }
            else
            {
                settings.presets[preset].apply(settings.mixer);
                gLoad_Click(sender, e);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            gExit_Click(sender, e);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            if (settings.devRec == null || settings.devRec.mm == null)
            {
                MessageBox.Show("I'm about to open the settings window.\r\n\r\n" +
                    "In the second dropdown (input Music), please select the speakers output you use when listening to music.\r\n\r\nPress apply when done.");
                gSettings_Click(sender, e);

                if (settings.devRec == null || settings.devRec.mm == null)
                {
                    MessageBox.Show("Sorry, but the settings are still invalid. Please try again or something.");
                    return;
                }
            }
            string app, str = "";
            app = "CAPTURE_ENDPOINT = " + settings.devRec.mm.ToString() + "\r\n";
            File.AppendAllText("Loopstream.log", app);
            str += app;

            wdt_waveIn = new NAudio.Wave.WasapiLoopbackCapture(settings.devRec.mm);

            app = "WASAPI_FMT = " + LSDevice.stringer(wdt_waveIn) + "\r\n";
            File.AppendAllText("Loopstream.log", app);
            str += app;

            Clipboard.Clear();
            Clipboard.SetText(str);
            MessageBox.Show(
                "Capture will begin when you press OK; please open a media player and start listening to some music.\r\n\r\n" +
                "While you wait, the following text is on your clipboard... Paste it in irc (Ctrl-V)\r\n\r\n" + str);

            wdt_v = File.OpenWrite("Loopstream.raw");
            wdt_writer = new NAudio.Wave.WaveFileWriter("Loopstream.wav", wdt_waveIn.WaveFormat);
            wdt_waveIn.DataAvailable += wdt_OnDataAvailable;
            wdt_waveIn.StartRecording();
            while (true)
            {
                if (val < 0) break;
                gMic.level = val;
                Application.DoEvents();
                System.Threading.Thread.Sleep(110);
                val += 2;
            }
            gMic.level = 0;
            if (DialogResult.Yes == MessageBox.Show("Test finished! The soundclip has been recorded to Loopstream.wav in the " +
                    "same folder as this .exe, more specifically here:\r\n\r\n" + Application.StartupPath + "\r\n\r\n" +
                    "Could you uploading this to pomf.se? Thanks!", "Open browser?", MessageBoxButtons.YesNo))
            {
                System.Diagnostics.Process.Start("http://pomf.se/");
            }
        }
        int val = 0;
        NAudio.Wave.IWaveIn wdt_waveIn;
        NAudio.Wave.WaveFileWriter wdt_writer;
        Stream wdt_v;

        void wdt_OnDataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            //Console.Write('.');
            wdt_v.Write(e.Buffer, 0, e.BytesRecorded);
            wdt_writer.Write(e.Buffer, 0, e.BytesRecorded);
            double secondsRecorded = wdt_writer.Length / wdt_writer.WaveFormat.AverageBytesPerSecond;
            int nval = (int)(secondsRecorded * 25.5);
            if (nval > val) val = nval;
            if (secondsRecorded >= 10)
            {
                val = -100;
                wdt_waveIn.StopRecording();
            }
        }

        private void Home_Move(object sender, EventArgs e)
        {
            if (!Screen.AllScreens.Any(s => s.WorkingArea.Contains(this.Bounds)))
            {
                invalOnNext = true;
            }
            else if (invalOnNext)
            {
                invalOnNext = false;
                inval();
            }
            if (invalOnNext)
            {
                inval();
            }
        }

        private void gGit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://github.com/9001/loopstream");
        }

        void tTitle_Tick(object sender, EventArgs e)
        {
            if (tag == null) return;
            this.Text = string.Format("{0:0.00} // {1:0.00} // {2}",
                Math.Round(settings.mp3.FIXME_kbps, 2),
                Math.Round(settings.ogg.FIXME_kbps, 2),
                tag.tag);
        }
    }
}
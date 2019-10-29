using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HookEx;
using System.Diagnostics;
using Microsoft.Expression.Encoder;
using Microsoft.Expression.Encoder.Devices;
using Microsoft.Expression.Encoder.ScreenCapture;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private ScreenCaptureJob screenCaptureJob;
        private Collection<EncoderDevice> audioDevices;
        private MediaItem mediaItem;
        private WindowsMediaOutputFormat windowsMediaOutputFormat;
        private Bitmap image;

        private Rectangle capturedRectangle;
        private Size workingArea;
        private Point MouseDownLocation;

        private Boolean windowsSoundEnabled = false;
        private Boolean microphoneSoundEnabled = false;
        private String videoPath = @"D:\Bitmap\Videos\";
        private String imagePath = @"D:\Bitmap\Images\";
        private String mediaPath = @"D:\Bitmap\";

        public Form1()
        {
            InitializeComponent();

            this.Visible = false;
            this.MouseWheel += Form1_MouseWheel;

            notifyIcon1.Visible = true;
            notifyIcon1.Icon = SystemIcons.Application;
            notifyIcon1.Text = "Screen Shot";

            if (!Directory.Exists(mediaPath))
                Directory.CreateDirectory(mediaPath);
            if (!Directory.Exists(videoPath))
                Directory.CreateDirectory(videoPath);
            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            UserActivityHook hook = new UserActivityHook();
            hook.KeyUp += (s, e) =>
            {
                if (e.KeyData.ToString() == "PrintScreen")
                {
                    this.Focus();
                    image = new Bitmap(Clipboard.GetImage());
                    Graphics graphic = Graphics.FromImage(image);
                    graphic.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                    String date = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss");
                    String filename = date + ".png";
                    image.Save(imagePath + filename);
                    pictureBox.Image = image;
                    this.Visible = true;
                }
            };
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                if ((pictureBox.Width < this.Width * 5) && (pictureBox.Height < this.Height * 5))
                {
                    pictureBox.Width = (int)(pictureBox.Width * 1.25);
                    pictureBox.Height = (int)(pictureBox.Height * 1.25);

                    pictureBox.Top = (int)(e.Y - 1.25 * (e.Y - pictureBox.Top));
                    pictureBox.Left = (int)(e.X - 1.25 * (e.X - pictureBox.Left));
                }
            }
            else
            {
                if ((pictureBox.Width > this.Width) && (pictureBox.Height > this.Height))
                {
                    pictureBox.Width = (int)(pictureBox.Width / 1.25) + 1;
                    pictureBox.Height = (int)(pictureBox.Height / 1.25) + 1;

                    pictureBox.Top = (int)(e.Y - 0.80 * (e.Y - pictureBox.Top));
                    pictureBox.Left = (int)(e.X - 0.80 * (e.X - pictureBox.Left));
                }
                else
                {
                    this.pictureBox.Size = new Size(this.Width - 16, this.Height - 39);
                    this.pictureBox.Location = new Point(((this.Width - 16) / 2) - (pictureBox.Width / 2), ((this.Height - 39) / 2) - (pictureBox.Height / 2));
                }

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (screenCaptureJob != null)
            {
                if (screenCaptureJob.Status == RecordStatus.Running)
                    screenCaptureJob.Stop();

                screenCaptureJob.Dispose();
            }
            Environment.Exit(1);
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(mediaPath);
        }

        private void SelectOutputFolder()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
                videoPath = @"" + folderBrowserDialog.SelectedPath;
        }

        private void SetRecordPreferences()
        {
            audioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);

            if (microphoneSoundEnabled && !windowsSoundEnabled)
            {
                try
                {
                    for (int i = 0; i <= audioDevices.Count - 1; i++)
                    {
                        if (audioDevices[i].Category == EncoderDeviceCategory.Capture)
                            screenCaptureJob.AddAudioDeviceSource(audioDevices[i]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            else if (!microphoneSoundEnabled && windowsSoundEnabled)
            {
                try
                {
                    for (int i = 0; i <= audioDevices.Count - 1; i++)
                    {
                        if (audioDevices[i].Category == EncoderDeviceCategory.Playback)
                            screenCaptureJob.AddAudioDeviceSource(audioDevices[i]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }
            else if (microphoneSoundEnabled && windowsSoundEnabled)
            {
                try
                {
                    for (int i = 0; i <= audioDevices.Count - 1; i++)
                    {
                        if (audioDevices[i].Category == EncoderDeviceCategory.Capture || audioDevices[i].Category == EncoderDeviceCategory.Playback)
                            screenCaptureJob.AddAudioDeviceSource(audioDevices[i]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            screenCaptureJob.ShowFlashingBoundary = true;
            screenCaptureJob.ShowCountdown = true;
            screenCaptureJob.CaptureMouseCursor = true;
            screenCaptureJob.OutputPath = videoPath;

            workingArea = SystemInformation.PrimaryMonitorSize;
            capturedRectangle = new Rectangle(0, 0, workingArea.Width, workingArea.Height);
            screenCaptureJob.CaptureRectangle = capturedRectangle;
        }

        private void StartRecording()
        {
            if (videoPath == null)
                MessageBox.Show("Output path undefined!");
            else
            {
                screenCaptureJob = new ScreenCaptureJob();
                SetRecordPreferences();
                screenCaptureJob.Start();
            }
        }

        private void Encode()
        {
            using (Job job = new Job())
            {
                string[] file = System.IO.Directory.GetFiles(videoPath, "*.xesc");
                mediaItem = new MediaItem(file[0]);
                Size size = mediaItem.OriginalVideoSize;
                windowsMediaOutputFormat = new WindowsMediaOutputFormat();
                windowsMediaOutputFormat.VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile();
                windowsMediaOutputFormat.AudioProfile = new Microsoft.Expression.Encoder.Profiles.WmaAudioProfile();
                windowsMediaOutputFormat.VideoProfile.AspectRatio = new System.Windows.Size(16, 9);
                windowsMediaOutputFormat.VideoProfile.AutoFit = true;

                if (size.Width >= 1920 && size.Height >= 1080)
                {
                    windowsMediaOutputFormat.VideoProfile.Size = new Size(1920, 1080);
                    windowsMediaOutputFormat.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(6000);
                }
                else if (size.Width >= 1280 && size.Height >= 720)
                {
                    windowsMediaOutputFormat.VideoProfile.Size = new Size(1280, 720);
                    windowsMediaOutputFormat.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(4000);
                }
                else
                {
                    windowsMediaOutputFormat.VideoProfile.Size = new Size(size.Height, size.Width);
                    windowsMediaOutputFormat.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(2000);
                }

                mediaItem.VideoResizeMode = VideoResizeMode.Letterbox;
                mediaItem.OutputFormat = windowsMediaOutputFormat;

                job.MediaItems.Add(mediaItem);
                job.CreateSubfolder = false;
                job.OutputDirectory = videoPath;
                job.EncodeProgress += new EventHandler<EncodeProgressEventArgs>(JobEncodeProgress);
                job.Encode();

                System.IO.File.Delete(file[0]);
            }
        }

        private void JobEncodeProgress(object sender, EncodeProgressEventArgs e)
        {
            string status = string.Format("Encoding progress: {0:f1}%", e.Progress);
        }

        private void openDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(videoPath);
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartRecording();
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
            if (screenCaptureJob.Status == RecordStatus.Running)
                screenCaptureJob.Stop();
            Encode();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            pictureBox.Size = new Size(this.Width - 16, this.Height - 39);
            this.pictureBox.Location = new Point(((this.Width - 16) / 2) - (pictureBox.Width / 2), ((this.Height - 39) / 2) - (pictureBox.Height / 2));
        }

        private void pictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            pictureBox.Size = new Size(this.Width - 16, this.Height - 39);
            this.pictureBox.Location = new Point(((this.Width - 16) / 2) - (pictureBox.Width / 2), ((this.Height - 39) / 2) - (pictureBox.Height / 2));
        }   

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            this.Text = "X : " + pictureBox.PointToClient(Cursor.Position).X + ", Y : " + pictureBox.PointToClient(Cursor.Position).Y;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                pictureBox.Left = e.X + pictureBox.Left - MouseDownLocation.X;
                pictureBox.Top = e.Y + pictureBox.Top - MouseDownLocation.Y;
            }
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                MouseDownLocation = e.Location;
        }

        private void ContextMenuStrip1_Click(object sender, EventArgs e)
        {
            this.Visible = true;
        }

        private void SystemDisabledToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            windowsSoundEnabled = !windowsSoundEnabled;
            if (windowsSoundEnabled)
            {
                systemDisabledToolStripMenuItem1.Text = "System : enabled";
                systemDisabledToolStripMenuItem1.Checked = true;
            }
            else
            {
                systemDisabledToolStripMenuItem1.Text = "System : disabled";
                systemDisabledToolStripMenuItem1.Checked = false;
            }
        }

        private void MicrophoneDisabledToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            microphoneSoundEnabled = !microphoneSoundEnabled;
            if (microphoneSoundEnabled)
            {
                microphoneDisabledToolStripMenuItem1.Text = "Microphone : enabled";
                microphoneDisabledToolStripMenuItem1.Checked = true;
            }
            else
            {
                microphoneDisabledToolStripMenuItem1.Text = "Microphone : disabled";
                microphoneDisabledToolStripMenuItem1.Checked = false;
            }
        }

        private void SelectRecordsPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectOutputFolder();
        }
    }
}

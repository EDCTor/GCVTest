using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Google.Cloud.Vision.V1;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

namespace GCV_Tester
{
    public partial class Form1 : Form
    {
        private Bitmap working_bm = null;
        private Google.Cloud.Vision.V1.Image working_gbm = null;
        private string[] files = null;
        private int currentFile = -1;
        private static readonly object refresh_lock = new object();

        public Form1()
        {
            InitializeComponent();
        }

        public void DisposeBitmaps()
        {
            if (this.working_bm != null) { this.working_bm.Dispose(); }
        }

        private void btnSelectDirectory_Click(object sender, EventArgs e)
        {
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && 
                !string.IsNullOrWhiteSpace(fbd.SelectedPath) &&
                Directory.Exists(fbd.SelectedPath)
                )
            {
                this.files = Directory.GetFiles(fbd.SelectedPath);                
                txtDirectoryPath.Text = fbd.SelectedPath;
                lblFileCount.Text = this.files.Length.ToString() + " file(s) found";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string directory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\Resources\\pictures\\";
            if (Directory.Exists(directory))
            {
                txtDirectoryPath.Text = directory;
                this.files = Directory.GetFiles(directory);
                lblFileCount.Text = this.files.Length.ToString() + " file(s) found";
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            lock (refresh_lock)
            {
                if (this.files == null ||
                this.files.Length == 0)
                {
                    // error cases, no files
                    return;
                }

                if (this.files.Length == this.currentFile)
                {
                    // if we are at the last file, then reset to the first file
                    this.currentFile = -1;
                }

                // increment the pointer to which file we are looking at
                this.currentFile += 1;

                // load the file
                if (File.Exists(this.files[this.currentFile]))
                {
                    this.working_bm = new Bitmap(this.files[this.currentFile]);
                    pictureBox1.Image = this.working_bm;

                    // Load an image from a local file.
                    this.working_gbm = Google.Cloud.Vision.V1.Image.FromFile(this.files[this.currentFile]);
                    var client = ImageAnnotatorClient.Create();
                    var response = client.DetectText(this.working_gbm);

                    // keep track of debug info we are going to print out
                    string s = string.Empty;

                    foreach (var annotation in response)
                    {
                        if (annotation.BoundingPoly != null)
                        {
                            int w = annotation.BoundingPoly.Vertices[1].X - annotation.BoundingPoly.Vertices[0].X;
                            int h = annotation.BoundingPoly.Vertices[2].Y - annotation.BoundingPoly.Vertices[1].Y;

                            using (Graphics g = Graphics.FromImage(this.working_bm))
                            {

                                Pen p;
                                if (((decimal)h / (decimal)w >= 0.2m) && ((decimal)h / (decimal)w <= 0.5m))
                                {
                                    p = new Pen(Color.Red, 5);

                                    if (!string.IsNullOrEmpty(annotation.Description))
                                    {
                                        s = s + "Good Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                        s = s + annotation.Description + System.Environment.NewLine;
                                    }
                                }
                                else
                                {
                                    p = new Pen(Color.Blue, 5);

                                    if (!string.IsNullOrEmpty(annotation.Description))
                                    {
                                        s = s + "Bad Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                        s = s + annotation.Description + System.Environment.NewLine;
                                    }
                                }
                                
                                g.DrawRectangle(p,
                                    annotation.BoundingPoly.Vertices[0].X,
                                    annotation.BoundingPoly.Vertices[0].Y,
                                    w, h
                                    );
                                pictureBox1.Image = this.working_bm;
                            }
                        }
                    }
                    rtbResults.Text = s;
                }

            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            lock (refresh_lock)
            {
                if (this.files == null ||
                this.files.Length == 0)
                {
                    // error cases, no files
                    return;
                }

                if (this.currentFile < 0)
                {
                    // if we are at the first file, then reset to the last file
                    this.currentFile = this.files.Length;
                }

                // increment the pointer to which file we are looking at
                this.currentFile -= 1;

                // load the file
                if (File.Exists(this.files[this.currentFile]))
                {
                    this.working_bm = new Bitmap(this.files[this.currentFile]);
                    pictureBox1.Image = this.working_bm;

                    // Load an image from a local file.
                    this.working_gbm = Google.Cloud.Vision.V1.Image.FromFile(this.files[this.currentFile]);
                    var client = ImageAnnotatorClient.Create();
                    var response = client.DetectText(this.working_gbm);

                    // keep track of debug info we are going to print out
                    string s = string.Empty;

                    foreach (var annotation in response)
                    {
                        if (annotation.BoundingPoly != null)
                        {
                            int w = annotation.BoundingPoly.Vertices[1].X - annotation.BoundingPoly.Vertices[0].X;
                            int h = annotation.BoundingPoly.Vertices[2].Y - annotation.BoundingPoly.Vertices[1].Y;

                            using (Graphics g = Graphics.FromImage(this.working_bm))
                            {

                                Pen p;
                                if (((decimal)h / (decimal)w >= 0.2m) && ((decimal)h / (decimal)w <= 0.5m))
                                {
                                    p = new Pen(Color.Red, 5);

                                    if (!string.IsNullOrEmpty(annotation.Description))
                                    {
                                        s = s + "Good Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                        s = s + annotation.Description + System.Environment.NewLine;
                                    }
                                }
                                else
                                {
                                    p = new Pen(Color.Blue, 5);

                                    if (!string.IsNullOrEmpty(annotation.Description))
                                    {
                                        s = s + "Bad Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                        s = s + annotation.Description + System.Environment.NewLine;
                                    }
                                }

                                g.DrawRectangle(p,
                                    annotation.BoundingPoly.Vertices[0].X,
                                    annotation.BoundingPoly.Vertices[0].Y,
                                    w, h
                                    );
                                pictureBox1.Image = this.working_bm;
                            }
                        }
                    }
                    rtbResults.Text = s;
                }

            }
        }
    }
}

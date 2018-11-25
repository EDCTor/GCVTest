using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Google.Cloud.Vision.V1;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using System.Text.RegularExpressions;

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

        private void RefreshCanvas()
        {
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
                string sGood = string.Empty;
                string sBad = string.Empty;
                string bestAttempt = string.Empty;

                foreach (EntityAnnotation annotation in response)
                {
                    //
                    // basic error checking in the results, in order to be considered a good translation 
                    // we are expecting a bounding poly and some sort of description
                    //
                    if ((annotation.BoundingPoly != null) && (!string.IsNullOrEmpty(annotation.Description)))
                    {
                        int w = annotation.BoundingPoly.Vertices[1].X - annotation.BoundingPoly.Vertices[0].X;
                        int h = annotation.BoundingPoly.Vertices[2].Y - annotation.BoundingPoly.Vertices[1].Y;

                        // start with the description returned from the API
                        string d = annotation.Description.Trim(); 
                        
                        // remove characters that are not plate numbers
                        d = EDCRemoveInvalidKeyCharacters(d);
                        
                        // make it all upper
                        d = d.ToUpper();

                        //  break it up by newline character (because the API returns multiple lines)
                        string[] a = d.Split(
                             new[] { "\r\n", "\r", "\n" },
                             StringSplitOptions.None
                         );

                        // check each individual item for a possible plate match
                        List<string> l = new List<string>();
                        foreach(string s in a)
                        {
                            if (s.Length <= 8)
                            {
                                l.Add(s);
                            }
                        }

                        // if we dont have a best attempt, lets try to find one
                        if (string.IsNullOrEmpty(bestAttempt))
                        {
                            bestAttempt = FindBestPlate(l.ToArray());
                        }

                        using (Graphics g = Graphics.FromImage(this.working_bm))
                        {
                            Pen p;

                            //
                            // a bit more intelligent error detection
                            // its in a bounding box that approximates the ratio of a plate (3" x 10")
                            // the data returned has length that could be a plate
                            // 
                            if (
                                (((decimal)h / (decimal)w >= 0.2m) && ((decimal)h / (decimal)w <= 0.5m)) &&
                                (l.Count > 0)
                                )
                            {

                                p = new Pen(Color.Red, 3);

                                if (!string.IsNullOrEmpty(annotation.Description))
                                {
                                    sGood = sGood + "Good Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                    foreach(string s in l.ToArray())
                                    {
                                        sGood = sGood + s + System.Environment.NewLine;
                                    }
                                }
                            }
                            else
                            {
                                p = new Pen(Color.Blue, 3);

                                if (!string.IsNullOrEmpty(annotation.Description))
                                {
                                    sBad = sBad + "Bad Ratio h/w:" + ((decimal)h / (decimal)w).ToString() + System.Environment.NewLine;
                                    sBad = sBad + d + System.Environment.NewLine;
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
                rtbGoodResults.Text = sGood;
                rtbBadResults.Text = sBad;
                lblBestGuess.Text = bestAttempt;
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

                RefreshCanvas();
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

                if (this.currentFile <= 0)
                {
                    // if we are at the first file, then reset to the last file
                    this.currentFile = this.files.Length;
                }

                // increment the pointer to which file we are looking at
                this.currentFile -= 1;

                RefreshCanvas();
            }
        }

        public string EDCRemoveInvalidKeyCharacters(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                char[] a = s.ToCharArray();
                a = Array.FindAll<char>(a, (c => (char.IsLetterOrDigit(c) || c == (char)13 || c == (char)10)));
                s = new string(a);
            }

            return s;
        }

        private string FindBestPlate(string[] list)
        {
            string best = string.Empty;

            // find based on NY 7 character rule (3 alpha, 4 number)
            Regex ny7 = new Regex(@"^[A-Z][A-Z][A-Z]\d{4}$");
            foreach (string s in list)
            {
                if (ny7.IsMatch(s))
                {
                    best = s;
                    break;
                }
            }

            // find based on common 6 character rule (3 alpha, 3 number)
            if (string.IsNullOrEmpty(best))
            {
                Regex ny6 = new Regex(@"^[A-Z][A-Z][A-Z]\d{3}$");
                foreach (string s in list)
                {
                    if (ny6.IsMatch(s))
                    {
                        best = s;
                        break;
                    }
                }
            }

            // find based on common 6 charater rule (3 number, 3 alpha)
            if (string.IsNullOrEmpty(best))
            {
                Regex ny6r = new Regex(@"^\d{3}[A-Z][A-Z][A-Z]$");
                foreach (string s in list)
                {
                    if (ny6r.IsMatch(s))
                    {
                        best = s;
                        break;
                    }
                }
            }

            // take the plate between 5-8 characters
            if (string.IsNullOrEmpty(best))
            {
                foreach(string s in list)
                {
                    if (s.Length >= 4 && s.Length <= 8)
                    {
                        best = s;
                        break;
                    }
                }
            }

            // return, the best we found or empty string
            return best;
        }

    }
}

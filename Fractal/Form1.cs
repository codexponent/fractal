using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Fractal {
    public partial class Form1 : Form {
        private Bitmap canvas;
        private const int WIDTH = 640;
        private const int HEIGHT = 480;
        private const int MAX = 256;
        private const double SX = -2.025; // start value real
        private const double SY = -1.125; // start value imaginary
        private const double EX = 0.6;    // end value real
        private const double EY = 1.125;  // end value imaginary
        private static double xstart, ystart, xende, yende, xzoom, yzoom;
        private static int x1, y1, xs, ys, xe, ye;
        private static Boolean action, rectangle, finished, mouseDown;

        private Boolean isAnimatedPalette = false;
        private Boolean isStop = false;
        private static float xy;
        private Point ORIGIN = new Point(0, 0);
        FormState state = new FormState();

        private void restartToolStripMenuItem_Click(object sender, EventArgs e) {
            //Restarts the Form
            Start(true);
            Invalidate();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            //Reloading the Form
            this.Hide();
            System.Threading.Thread.Sleep(1000);
            Start(true);
            this.Show();
        }

        private void Form1_Load(object sender, EventArgs e) {
            //Loading the Serializable Values from FormState
            if (File.Exists("config.xml")) {
                XmlSerializer ser = new XmlSerializer(typeof(FormState));
                using (FileStream fs = File.OpenRead("config.xml")) {
                    state = (FormState)ser.Deserialize(fs);
                    xstart = state.xstart;
                    ystart = state.ystart;
                    xende = state.xende;
                    yende = state.yende;
                    xzoom = state.xzoom;
                    yzoom = state.yzoom;
                    x1 = state.x1;
                    y1 = state.y1;
                    xs = state.xs;
                    ys = state.ys;
                    xe = state.xe;
                    ye = state.ye;
                }
                Start(false);
            } else {
                Start(true);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) { 
            //Serializeing the FormState Object
            state.xstart = xstart;
            state.ystart = ystart;
            state.xende = xende;
            state.yende = yende;
            state.xzoom = xzoom;
            state.yzoom = yzoom;
            state.x1 = x1;
            state.y1 = y1;
            state.xs = xs;
            state.ys = ys;
            state.xe = xe;
            state.ye = ye;
            using (StreamWriter sw = new StreamWriter("config.xml")) {
                XmlSerializer ser = new XmlSerializer(typeof(FormState));
                ser.Serialize(sw, state);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            //Saving the Image on PNG Format
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Png Image|*.png";
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "") {
                canvas.Save(saveFileDialog1.FileName, ImageFormat.Png);
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            //Unlocking the Thread at 16ms = 60fps
            if (isAnimatedPalette) {
                ColorCycle();
            }
            Invalidate();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e) {
            mouseDown = true;
            if (action) {
                xs = e.X;
                ys = e.Y;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) {
            if (action && mouseDown) {
                xe = e.X;
                ye = e.Y;
                rectangle = true;
                Invalidate();
            }            
            //On Status Bar
            toolStripStatusLabel1.Text = "(" + e.X + ", " + e.Y + ") " + canvas.GetPixel(e.X, e.Y);
            statusStrip1.Refresh();
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e) {
            mouseDown = false;
            int z, w;
            if (action) {
                xe = e.X;
                ye = e.Y;
                if (xs > xe) {
                    z = xs;
                    xs = xe;
                    xe = z;
                }
                if (ys > ye) {
                    z = ys;
                    ys = ye;
                    ye = z;
                }
                w = (xe - xs);
                z = (ye - ys);
                if ((w < 2) && (z < 2)) InitValues();   //Small Zoom restarts
                else {
                    if (((float)w > (float)z * xy)) ye = (int)((float)ys + (float)w / xy);
                    else xe = (int)((float)xs + (float)z * xy);
                    xende = xstart + xzoom * (double)xe;
                    yende = ystart + yzoom * (double)ye;
                    xstart += xzoom * (double)xs;
                    ystart += yzoom * (double)ys;
                }
                xzoom = (xende - xstart) / (double)WIDTH;
                yzoom = (yende - ystart) / (double)HEIGHT;
                DrawMandelbrot();
                rectangle = false;
                Invalidate();
            }
        }

        private void InitValues() // reset start values
        {
            xstart = SX;
            ystart = SY;
            xende = EX;
            yende = EY;
            //Actual mandelbort  aspect ratio
            if ((float)((xende - xstart) / (yende - ystart)) != xy)         //If aspect ratio not same, make it same
                xstart = xende - (yende - ystart) * (double)xy;
        }

        public Form1() {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            Init();
        }

        private void Init() {
            finished = false;
            xy = (float)WIDTH / (float)HEIGHT;
            canvas = new Bitmap(WIDTH, HEIGHT, PixelFormat.Format8bppIndexed);
            RetrieveOriginalPalette();
        }

        private void originalToolStripMenuItem_Click(object sender, EventArgs e) {
            //Original Menu Clicked
            RetrieveOriginalPalette();
        }

        private void grayScaleToolStripMenuItem_Click(object sender, EventArgs e) {
            //Grey Menu Clicked
            GreyScalePalette();
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e) {
            //Red Menu Clicked
            RedScalePalette();
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e) {
            //Green Menu Clicked
            GreenScalePalette();
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e) {
            //Blue Menu Clicked
            BlueScalePalette();
        }

        private void yellowToolStripMenuItem_Click(object sender, EventArgs e) {
            //Yellow Menu Clicked
            YellowScalePalette();
        }

        private void cyanToolStripMenuItem_Click(object sender, EventArgs e) {
            //Cyan Menu Clicked
            CyanScalePalette();
        }

        private void magentaToolStripMenuItem_Click(object sender, EventArgs e) {
            //Magenta Menu Clicked
            MagentaScalePalette();
        }

        private void animateToolStripMenuItem_Click(object sender, EventArgs e) {
            //Helps to Animate the Color Palette
            if (sender == animateToolStripMenuItem) {
                animateToolStripMenuItem.Checked = !animateToolStripMenuItem.Checked;
                if (animateToolStripMenuItem.Checked) {
                    isAnimatedPalette = true;
                } else {
                    isAnimatedPalette = false;
                }
            }
        }

        private void cloneToolStripMenuItem_Click(object sender, EventArgs e) {
            //Clones the Current Form
            Form1_FormClosing(null, null);      //Preserving the State
            Form form2 = new Form1();
            form2.Show();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e) {
            //Stops Drawing the Mandelbort
            isStop = true;
            Start(true);
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e) {
            //Starts Drawning the Mandelbort
            isStop = false;
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e) {
            //Opens the Info Form as a Dialog Box
            Form info = new Info();
            info.ShowDialog(this);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e) {
            //Quits the Application
            Application.Exit();
        }

        private void PrintPage(object o, PrintPageEventArgs e) {
            //Retrieves the Image
            Point loc = new Point(100, 100);
            e.Graphics.DrawImage(canvas, loc);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e) {
            //Opens the Print Dialog Box
            PrintDialog PrintDialog1 = new PrintDialog();
            PrintDocument pd = new PrintDocument();
            PrintDialog1.Document = pd;
            pd.PrintPage += PrintPage;
            if (PrintDialog1.ShowDialog() == DialogResult.OK) {
                pd.Print();
            }
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e) {
            //Opens the Properties Form as a Dialog Box
            Form properties = new Props();
            properties.ShowDialog(this);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e) {
            //Closes the Form
            this.Close();
        }

        public void GreyScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Grey Color in Color Palette
                pal.Entries[i] = Color.FromArgb(i, i, i);
            }
            canvas.Palette = pal;
        }

        public void RedScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Red Color in Color Palette
                pal.Entries[i] = Color.FromArgb(i, 0, 0);
            }
            canvas.Palette = pal;
        }

        public void GreenScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Green Color in Color Palette
                pal.Entries[i] = Color.FromArgb(0, i, 0);
            }
            canvas.Palette = pal;
        }

        public void BlueScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Blue Color in Color Palette
                pal.Entries[i] = Color.FromArgb(0, 0, i);
            }
            canvas.Palette = pal;
        }

        public void YellowScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Yellow Color in Color Palette
                pal.Entries[i] = Color.FromArgb(i, i, 0);
            }
            canvas.Palette = pal;
        }

        public void CyanScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Cyan Color in Color Palette
                pal.Entries[i] = Color.FromArgb(0, i, i);
            }
            canvas.Palette = pal;
        }

        public void MagentaScalePalette() {
            ColorPalette pal = canvas.Palette;
            for (int i = 0; i <= 255; i++) {
                // Create Shades of Magenta Color in Color Palette
                pal.Entries[i] = Color.FromArgb(i, 0, i);
            }
            canvas.Palette = pal;
        }

        public void RetrieveOriginalPalette(){
            //Gives the Original Palette According to the HSB Model
            ColorPalette pal = canvas.Palette;
            float saturation = 0.8f;
            float hue, brightness;
            HSB hsb = new HSB();
            for (int i = 0; i <= 255; i++) {
                hue = (float)i / (float)MAX;
                brightness = 1.0f - hue * hue;
                hsb.FromHSB(hue, saturation, brightness);
                pal.Entries[i] = Color.FromArgb(hsb.rChan, hsb.gChan, hsb.bChan);
            }
            canvas.Palette = pal;
        }

        private void ColorCycle() {
            //Revolves the Values on the Color Palette Table
            ColorPalette pal = canvas.Palette;
            Color first = Color.FromArgb(pal.Entries[0].R, pal.Entries[0].G, pal.Entries[0].B);
            for(int i = 1; i <= 255; i++) {
                pal.Entries[i - 1] = pal.Entries[i];
            }
            pal.Entries[255] = first;
            canvas.Palette = pal;
        }

        private void Start(Boolean firstTime) {
            //Takes boolean value and compares
            //If false, starts from loading the config.xml
            //If true, starts from scratch
            action = false;
            rectangle = false;
            if (firstTime) {
               InitValues();
            }
            xzoom = (xende - xstart) / (double)WIDTH; //scaling real pixel to mandelbort
            yzoom = (yende - ystart) / (double)HEIGHT;
            DrawMandelbrot();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent) {
        }//Stops Flickring

        private void Form1_Paint(object sender, PaintEventArgs e) {
            //Painting the Mandelbrot on Form
            if (!isStop) {
                Graphics g = e.Graphics;
                g.DrawImage(canvas, ORIGIN);
                if (rectangle) {
                    Pen pLine = new Pen(Color.White, 1);
                    if (xs < xe) {
                        if (ys < ye) g.DrawRectangle(pLine, xs, ys, (xe - xs), (ye - ys));
                        else g.DrawRectangle(pLine, xs, ye, (xe - xs), (ys - ye));
                    } else {
                        if (ys < ye) g.DrawRectangle(pLine, xe, ys, (xs - xe), (ye - ys));
                        else g.DrawRectangle(pLine, xe, ye, (xs - xe), (ys - ye));
                    }
                }
            }
        }
        
        private void DrawMandelbrot() {
            action = false;
            for (int x = 0; x < WIDTH; x++) {
                for (int y = 0; y < HEIGHT; y++) {
                    float result = PointColour(xstart + xzoom * (double)x, ystart + yzoom * (double)y);
                    //Allow Every Pixel to take value from the Color Palette Ranged from 0-255
                    BitmapData bmpData = canvas.LockBits(new Rectangle(x, y, 1, 1),
                                    ImageLockMode.ReadOnly,
                                    canvas.PixelFormat);
                    Marshal.WriteByte(bmpData.Scan0, (byte)(result * 255));
                    canvas.UnlockBits(bmpData);
                }
            }
            action = true;
        }
        private float PointColour(double xwert, double ywert){
            //Actual Mandelbort Formulae
            double r = 0.0, i = 0.0, m = 0.0;
            int j = 0;
            while ((j < MAX) && (m < 4.0)) {
                j++;
                m = r * r - i * i;
                i = 2.0 * r * i + ywert;
                r = m + xwert;
            }
            return (float)j / (float)MAX;
        }
    }
}
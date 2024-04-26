using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NAudio.Wave;
using Accord.Math;

namespace fft_time_scope
{
    public partial class Form1 : Form
    {
        // MICROPHONE ANALYSIS SETTINGS
        private int RATE = 44100; // sample rate of the sound card
        private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2

        // prepare class objects
        public BufferedWaveProvider bwp;

        public Form1()
        {
            InitializeComponent();
            SetupGraphLabels();
            StartListeningToMicrophone();
        }

        void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        public void SetupGraphLabels()
        {
            //scottPlotUC1.plt.Title("Microphone PCM Data");
            scottPlotUC1.plt.YLabel("Amplitude (PCM)");
            scottPlotUC1.plt.XLabel("Time (ms)");
            scottPlotUC1.plt.Style(style: ScottPlot.Style.Black);

            //scottPlotUC2.plt.Title("Microphone FFT Data");
            scottPlotUC2.plt.YLabel("Power (raw)");
            scottPlotUC2.plt.XLabel("Frequency (kHz)");
            scottPlotUC2.plt.Style(style: ScottPlot.Style.Black);
        }

        public void StartListeningToMicrophone(int audioDeviceNumber = 0)
        {
            WaveIn wi = new WaveIn();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * 2;
            bwp.DiscardOnBufferOverflow = true;
            try
            {
                wi.StartRecording();
            }
            catch
            {
                string msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                MessageBox.Show(msg, "ERROR");
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {           
            PlotLatestData();                  
        }       
        public void PlotLatestData()
        {                      
            int frameSize = BUFFERSIZE;
            var audioBytes = new byte[frameSize];
            bwp.Read(audioBytes, 0, frameSize);
           
            if (audioBytes.Length == 0)
                return;
            if (audioBytes[frameSize - 2] == 0)
                return;

            int BYTES_PER_POINT = 4;
            
            int graphPointCount = audioBytes.Length / BYTES_PER_POINT;

            double[] pcm = new double[graphPointCount];
            double[] fft = new double[graphPointCount];
            double[] fftReal = new double[graphPointCount/2];
           
            for (int i = 0; i < graphPointCount; i++)
            {              
                Int16 val = BitConverter.ToInt16(audioBytes, i * 2);

                pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
            }

            fft = FFT(pcm);
           
            double pcmPointSpacingMs = RATE / 1000;
            double fftMaxFreq = RATE / 2;
            double fftPointSpacingHz = fftMaxFreq / graphPointCount;

            // just keep the real half (the other half imaginary)
            Array.Copy(fft, fftReal, fftReal.Length);

            scottPlotUC1.plt.Clear();
            scottPlotUC2.plt.Clear();
            
            scottPlotUC1.plt.PlotSignal(pcm, pcmPointSpacingMs);
            scottPlotUC1.plt.Axis(0, 10, -10, 10);
            //scottPlotUC1.plt.AxisAutoY();
            scottPlotUC1.Render();
         
            scottPlotUC2.plt.PlotSignal(fftReal, fftPointSpacingHz);
            scottPlotUC2.plt.Axis(0, 5, 0, 3);
            //scottPlotUC2.plt.AxisAutoY();
            scottPlotUC2.Render();
                   
            Application.DoEvents();
        }

        public double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length; i++)
                fft[i] = fftComplex[i].Magnitude;
            return fft;
        }
    }
}

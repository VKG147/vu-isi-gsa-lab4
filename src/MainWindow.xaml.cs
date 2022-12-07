using Microsoft.Win32;
using NAudio.Wave;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Lab4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum TimeUnit
        {
            Milliseconds = 0,
            Seconds = 1,
            Minutes = 2
        }

        private const int MaxAudioFileBytes = 44100 * 60 * 60; // 1 hour of 44100 bitrate

        private uint? _from = null;
        private uint? _length = null;

        private IPlottable? _channel1Plot = null;

        private double[] _channel1Xs = new double[] { };

        private string _wavAudioPath = string.Empty;

        private readonly System.Drawing.Color Channe1PlotColor = System.Drawing.Color.Blue;

        public MainWindow()
        {
            InitializeComponent();

            //audioPlot.Plot.XLabel("X (minutes)");
            //audioPlot.Plot.YLabel("Y");

            loadedFileLabel.Content = "";
        }

        private void CreateAudioPlot(string wavPath)
        {
            using WaveFileReader reader = new WaveFileReader(wavPath);

            if (reader.Length > MaxAudioFileBytes)
            {
                return;
            }
            if (reader.WaveFormat.Channels > 2)
            {
                return;
            }

            List<double> channel1Ys = new();
            List<double> channel2Ys = new();
            int maxAudioPlotYValue = (int)Math.Round(Math.Pow(2.0, reader.WaveFormat.BitsPerSample - 1));

            while (reader.Position < reader.Length)
            {
                float[] sampleFrame = reader.ReadNextSampleFrame();
                channel1Ys.Add(sampleFrame[0] * maxAudioPlotYValue);

                if (sampleFrame.Length > 1)
                {
                    channel2Ys.Add(sampleFrame[1] * maxAudioPlotYValue);
                }
            }

            double totalTimeMultiplier = 0;
            if (reader.TotalTime.TotalSeconds < 1)
            {
                totalTimeMultiplier = reader.TotalTime.TotalMilliseconds;
                //audioPlot.Plot.XLabel("X (milliseconds)");
                //audioPlot.Plot.YLabel("Y");
            }
            else if (reader.TotalTime.TotalSeconds < 60)
            {
                totalTimeMultiplier = reader.TotalTime.TotalSeconds;
                //audioPlot.Plot.XLabel("X (seconds)");
                //audioPlot.Plot.YLabel("Y");
            }
            else
            {
                totalTimeMultiplier = reader.TotalTime.TotalMinutes;
                //audioPlot.Plot.XLabel("X (minutes)");
                //audioPlot.Plot.YLabel("Y");
            }

            _channel1Xs = new double[channel1Ys.Count];
            for (int i = 0; i < _channel1Xs.Length; i++)
            {
                _channel1Xs[i] = (double)i / _channel1Xs.Length * totalTimeMultiplier;
            }
            _channel1Plot = audioPlot.Plot.AddSignalXY(_channel1Xs, channel1Ys.ToArray(), color: Channe1PlotColor, label: "channel1");

            

            audioPlot.Refresh();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "WAV|*.wav",
                Multiselect = false,
                InitialDirectory = Environment.CurrentDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                audioPlot.Reset();

                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);

                loadedFileLabel.Content = fileName;

                using WaveFileReader reader = new WaveFileReader(filePath);
                _wavAudioPath = filePath;

                if (reader.WaveFormat.Channels == 1)
                {
                    _wavAudioPath = CreateStereoEffect(_wavAudioPath, 10);
                }

                CreateAudioPlot(_wavAudioPath);
            }
        }

        private string CreateStereoEffect(string sourceWavPath, double offsetInMs)
        {
            string? dir = Path.GetDirectoryName(sourceWavPath);
            string fileName = Path.GetFileNameWithoutExtension(sourceWavPath);
            string ext = Path.GetExtension(sourceWavPath);

            string outputPath = $"{dir}/{fileName}_{offsetInMs}ms{ext}";

            using WaveFileReader reader = new WaveFileReader(sourceWavPath);

            float[] channel1Samples = new float[reader.SampleCount];
            for (int i = 0; i < channel1Samples.Length; ++i)
                channel1Samples[i] = (reader.ReadNextSampleFrame())[0];

            using WaveFileWriter writer = new WaveFileWriter(
                outputPath, 
                WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, 2));

            int offset = (int)((offsetInMs / reader.TotalTime.TotalMilliseconds) * reader.SampleCount);

            int i_channel1 = 0;
            int i_channel2 = 0;

            if (offset < 0)
            {
                for (; i_channel2 < Math.Abs(offset); ++i_channel2)
                {
                    writer.WriteSample(0);
                    writer.WriteSample(channel1Samples[i_channel2]);
                }
            }
            else if (offset >= 0)
            {
                for (; i_channel1 < offset; ++i_channel1)
                {
                    writer.WriteSample(channel1Samples[i_channel1]);
                    writer.WriteSample(0);
                }
            }

            while (i_channel1 < channel1Samples.Length || i_channel2 < channel1Samples.Length)
            {
                writer.WriteSample(i_channel1 < channel1Samples.Length ? channel1Samples[i_channel1] : 0);
                writer.WriteSample(i_channel2 < channel1Samples.Length ? channel1Samples[i_channel2] : 0);
                i_channel1++;
                i_channel2++;
            }

            writer.Flush();
            
            return outputPath;
        }

        private void txtboxIntervalFrom_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!uint.TryParse(txtboxIntervalFrom.Text, out uint val))
                return;

            _from = val;

            DrawSpectreDiagram();
        }

        private void txtboxIntervalLength_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!uint.TryParse(txtboxIntervalLength.Text, out uint val))
                return;

            _length = val;

            DrawSpectreDiagram();
        }

        private void DrawSpectreDiagram()
        {
            if (_from is null || _length is null || !_channel1Xs.Any())
                return;

            double[] spectreYs = SpectreDiagramUtils
                .GetSpectreDiagram(_channel1Xs, _from ?? 0, _length ?? 0);

            double[] spectreXs = new double[spectreYs.Length];
            for (int i = 0; i < spectreXs.Length; ++i)
            {
                spectreXs[i] = i;
            }    

            spectrePlot.Plot.AddSignalXY(spectreXs, spectreYs, color: Channe1PlotColor, label: "spectre");

            audioPlot.Refresh();
        }

        //private double ConvertTimeUnit(double time, TimeUnit from, TimeUnit to)
        //{
        //    if (from == TimeUnit.Milliseconds && to == TimeUnit.Seconds)
        //        return TimeSpan.FromMilliseconds(time).TotalSeconds;
        //    else if (from == TimeUnit.Milliseconds && to == TimeUnit.Minutes)
        //        return TimeSpan.FromMilliseconds(time).TotalMinutes;
        //    else if (from == TimeUnit.Seconds && to == TimeUnit.Milliseconds)
        //        return TimeSpan.FromSeconds(time).TotalMilliseconds;
        //    else if (from == TimeUnit.Seconds && to == TimeUnit.Minutes)
        //        return TimeSpan.FromSeconds(time).TotalMinutes;
        //    else if (from == TimeUnit.Minutes && to == TimeUnit.Milliseconds)
        //        return TimeSpan.FromMinutes(time).TotalMilliseconds;
        //    else if (from == TimeUnit.Minutes && to == TimeUnit.Seconds)
        //        return TimeSpan.FromMinutes(time).TotalSeconds;

        //    return time;
        //}
    }
}

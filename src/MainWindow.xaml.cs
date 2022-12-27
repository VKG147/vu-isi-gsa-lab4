using Microsoft.Win32;
using NAudio.Wave;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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
        private uint? _sampleRate = null;

        private IPlottable? _channel1Plot = null;

        private double[] _channel1Xs = new double[] { };
        private double[] _channel1Ys = new double[] { };

        private string _wavAudioPath = string.Empty;

        private readonly System.Drawing.Color Channe1PlotColor = System.Drawing.Color.Blue;

        public MainWindow()
        {
            InitializeComponent();

            spectrePlot.Plot.XLabel("Frequency (Hz)");
            spectrePlot.Plot.YLabel("Amplitude value");

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
            _channel1Ys = channel1Ys.ToArray();

            double totalTimeMultiplier = 0;
            if (reader.TotalTime.TotalSeconds < 1)
            {
                totalTimeMultiplier = reader.TotalTime.TotalMilliseconds;
                audioPlot.Plot.XLabel("Time (milliseconds)");
                audioPlot.Plot.YLabel("Value");

                cutAudioPlot.Plot.XLabel("Time (milliseconds)");
                cutAudioPlot.Plot.YLabel("Value");
            }
            else if (reader.TotalTime.TotalSeconds < 60)
            {
                totalTimeMultiplier = reader.TotalTime.TotalSeconds;
                audioPlot.Plot.XLabel("Time (seconds)");
                audioPlot.Plot.YLabel("Value");

                cutAudioPlot.Plot.XLabel("Time (seconds)");
                cutAudioPlot.Plot.YLabel("Value");
            }
            else
            {
                totalTimeMultiplier = reader.TotalTime.TotalMinutes;
                audioPlot.Plot.XLabel("Time (minutes)");
                audioPlot.Plot.YLabel("Value");

                cutAudioPlot.Plot.XLabel("Time (minutes)");
                cutAudioPlot.Plot.YLabel("Value");
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
                _sampleRate = (uint)reader.WaveFormat.SampleRate;

                CreateAudioPlot(_wavAudioPath);
            }
        }

        private void txtboxIntervalFrom_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!double.TryParse(txtboxIntervalFrom.Text, out double val))
                return;
            if (val < 0)
                return;

            _from = (uint)(val * _sampleRate ?? 0); // In seconds

            DrawCutAudioPlot();
            DrawSpectreDiagram();
        }

        private void txtboxIntervalLength_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!double.TryParse(txtboxIntervalLength.Text, out double val))
                return;
            if (val <= 0)
                return;
            
            _length = (uint)((val * _sampleRate ?? 0) / 1000); // In milliseconds

            _length = _length < 10 ? 10 : _length;
            _length = _length > 1000 ? 1000 : _length;

            DrawCutAudioPlot();
            DrawSpectreDiagram();
        }

        private void DrawCutAudioPlot()
        {
            if (_from is null || _length is null || !_channel1Xs.Any() || _sampleRate is null)
                return;
            if (_from + _length > _channel1Xs.Length)
                return;

            double[] xs = new double[(int)_length];
            double[] ys = new double[(int)_length];

            Array.Copy(_channel1Xs, (int)_from, xs, 0, (int)_length);
            Array.Copy(_channel1Ys, (int)_from, ys, 0, (int)_length);

            cutAudioPlot.Plot.Clear();
            cutAudioPlot.Plot.AddSignalXY(xs, ys, color: Channe1PlotColor);

            cutAudioPlot.Refresh();
        }

        private void DrawSpectreDiagram()
        {
            if (_from is null || _length is null || !_channel1Xs.Any() || _sampleRate is null)
                return;
            if (_from + _length > _channel1Xs.Length)
                return;

            (double[] spectreXs, double[] spectreYs) = SpectreDiagramUtils
                .GetSpectreDiagram(_channel1Ys, _from ?? 0, _length ?? 0, _sampleRate ?? 0);

            spectrePlot.Plot.Clear();
            spectrePlot.Plot.AddSignalXY(spectreXs, spectreYs, color: Channe1PlotColor, label: "spectre");

            audioPlot.Refresh();
            spectrePlot.Refresh();
        }
    }
}

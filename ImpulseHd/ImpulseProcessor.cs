﻿using LowProfile.Fourier.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class ImpulseProcessor
	{
		private readonly ImpulseConfig config;
		private readonly Complex[] fftSignal;
		private readonly Transform fftTransform;

		private readonly int sampleCount;
		private readonly double samplerate;

		public ImpulseProcessor(ImpulseConfig config)
		{
			this.config = config;
			var wavData = AudioLib.WaveFiles.ReadWaveFile(config.FilePath);
			sampleCount = config.SampleSize;
			samplerate = config.Samplerate;

			var wav = wavData[0].ToList();
			while (wav.Count < sampleCount)
				wav.Add(0.0);

			this.fftTransform = new Transform(sampleCount);
			var input = wav.Select(x => (Complex)x).ToArray();

			fftSignal = new Complex[input.Length];
			fftTransform.FFT(input, fftSignal);
		}

		public Complex[] FftSignal => fftSignal;

		public double[] TimeSignal
		{
			get
			{
				var outputFinal = new Complex[sampleCount];
				fftTransform.IFFT(fftSignal, outputFinal);
				return outputFinal.Select(x => x.Real).ToArray();
			}
		}

		public SpectrumStage[] Stages => config.SpectrumStages.ToArray();

		public void ProcessStage(SpectrumStage stage)
		{
			if (!stage.IsEnabled)
				return;

			var nyquist = samplerate / 2;
			var absMaxIndex = fftSignal.Length / 2;
			var minIndex = (int)Math.Round(stage.MinFreq / (double)nyquist * absMaxIndex);
			var maxIndex = (int)Math.Round(stage.MaxFreq / (double)nyquist * absMaxIndex);

			if (minIndex < 1) minIndex = 1;
			if (maxIndex < 1) maxIndex = 1;
			if (minIndex >= absMaxIndex) minIndex = absMaxIndex;
			if (maxIndex >= absMaxIndex) maxIndex = absMaxIndex;

			for (int i = minIndex; i <= maxIndex; i++)
			{
				var newVal = fftSignal[i] * Complex.CExp(-2 * Math.PI * i * stage.DelaySamples / (double)config.SampleSize);
				fftSignal[i] = newVal;
				fftSignal[fftSignal.Length - i].Arg = -newVal.Arg;
			}
		}
	}
}
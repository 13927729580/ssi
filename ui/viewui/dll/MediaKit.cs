﻿using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Windows;
using WPFMediaKit.DirectShow.Controls;

namespace ssi
{
    public class MediaKit : MediaUriElement, IMedia
    {
        private string filepath;

        public string GetFilepath()
        {
            return filepath;
        }

        public string GetFolderepath()
        {
            return filepath.Substring(0, filepath.LastIndexOf("\\") + 1);
        }

        public void SetVolume(double volume)
        {
            this.Volume = volume;
        }

        public MediaKit(string filepath, double pos_in_seconds)
        {
            this.LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
            this.UnloadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;

            this.BeginInit();
            this.Source = new Uri(filepath);
            this.EndInit();
            // if ScrubbingEnabled is true move correctly shows selected frame, but cursor won't work any more...
            //  this.ScrubbingEnabled = true;
            this.Volume = 1.0;
            this.Pause();
            this.filepath = filepath;
        }

        public void Move(double to_in_seconds)
        {
            this.MediaPosition = (long)(to_in_seconds * 10000000.0);
        }

        public double GetPosition()
        {
            return this.MediaPosition / 10000000.0;
        }

        public bool IsVideo()
        {
            return this.HasVideo;
        }

        public double GetSampleRate()
        {
            var inputFile = new MediaFile { Filename = this.filepath };
            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
            }

            if (this.HasVideo)
                return inputFile.Metadata.VideoData.Fps;
            else return double.Parse(inputFile.Metadata.AudioData.SampleRate);
        }

        public UIElement GetView()
        {
            return this;
        }

        public double GetLength()
        {
            return this.MediaDuration / 10000000.0;
        }

        public void Clear()
        {
            this.Close();
        }

        public void zoomOut(double factor, double width, double height)
        {
            this.Width = width / factor;
            this.Height = height / factor;
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            //    this.Stretch = System.Windows.Media.Stretch.Fill;
        }

        public void zoomIn(double factor, double width, double height)
        {
            this.Width = width * factor;
            this.Height = height * factor;
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            //  this.Stretch = System.Windows.Media.Stretch.Fill;
        }
    }
}
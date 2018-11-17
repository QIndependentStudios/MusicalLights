using QIndependentStudios.MusicalLights.Core;
using QIndependentStudios.MusicalLights.Uwp.App.SequencePlayback;
using System;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace QIndependentStudios.MusicalLights.Uwp.App
{
    public sealed partial class MainPage : Page
    {
        private const long Interval = TimeSpan.TicksPerMillisecond * 50;

        public MainPage()
        {
            InitializeComponent();
            DoStuffAsync();
        }

        private async void DoStuffAsync()
        {
            var player = new IotSequencePlayer();

            var sequenceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SequenceData/Wizards In Winter.json"));
            var sequence = Sequence.FromJson(await FileIO.ReadTextAsync(sequenceFile));

            await player.LoadAsync(MediaSource.CreateFromUri(new Uri($"ms-appx:///Media/{sequence.Audio}")), sequence);
            player.Play();
        }
    }
}

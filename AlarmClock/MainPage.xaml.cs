using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AlarmClock
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private List<string> labels;
        private string expectedEmotion;
        private string detectedEmotion = string.Empty;
        private VideoFrame lastFrame;
        private Timer timer = new Timer(5000);
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            labels = new List<string>()
    {
        "Neutral",
        "Happiness",
        "Surprise",
        "Sadness",
        "Anger",
        "Disgust",
        "Fear",
        "Contempt"
    };
            await camera.StartAsync();
            camera.CameraHelper.FrameArrived += Preview_FrameArrived;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }
        private async void Preview_FrameArrived(object sender, FrameEventArgs e)
        {
            lastFrame = e.VideoFrame;
        }

        private async Task AnalyzeFrame()
        {
            // Analyze the last frame
            try
            {
                detectedEmotion = await DetectEmotionWithCognitiveServices();
            }
            catch
            {
                return;
            }
        }
        private async Task<string> DetectEmotionWithCognitiveServices()
        {
            var originalBitmap = lastFrame.SoftwareBitmap;
            if (originalBitmap == null)
                return "No frame captured";

            // Set correct subscriptionKey and API Url.
            string subscriptionKey = "e98007d99d3a4cc292c62d38663f78eb";
            string apiBaseUrl = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

            using (InMemoryRandomAccessStream imageStream = new InMemoryRandomAccessStream())
            {
                SoftwareBitmap bitmap = SoftwareBitmap.Convert(originalBitmap, BitmapPixelFormat.Rgba16);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, imageStream);
                encoder.SetSoftwareBitmap(bitmap);
                var ratio = bitmap.PixelHeight / 200;
                encoder.BitmapTransform.ScaledHeight = (uint)Math.Round((double)bitmap.PixelHeight / ratio);
                encoder.BitmapTransform.ScaledWidth = (uint)Math.Round((double)bitmap.PixelWidth / ratio);
                await encoder.FlushAsync();

                imageStream.Seek(0);

                var faceServiceClient = new FaceServiceClient(subscriptionKey, apiBaseUrl);
                var detectedEmotion = string.Empty;

                try
                {
                    Face[] faces = await faceServiceClient.DetectAsync(imageStream.AsStream(), false, true, new FaceAttributeType[] { FaceAttributeType.Emotion });
                    var detectedFace = faces?.FirstOrDefault();
                    detectedEmotion = detectedFace == null ? "Nothing" : detectedFace.FaceAttributes.Emotion.ToRankedList().FirstOrDefault().Key;
                }
                catch (FaceAPIException e)
                {
                    detectedEmotion = "API error. Check the values of subscriptionKey and apiBaseUrl";
                }

                return detectedEmotion;
            }
        }
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await AnalyzeFrame();

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                DetectedEmotion.Text = string.Format("Detected {0} at {1}", detectedEmotion, DateTime.Now.ToLongTimeString());
            }
            );
        }
    }
}

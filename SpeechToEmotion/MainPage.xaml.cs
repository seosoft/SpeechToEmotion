using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;

namespace SpeechToEmotion
{
    public sealed partial class MainPage
    {
        // SpeechToTextのキーおよびリージョン
        private const string SpeechSubscriptionKey = "<Speechのサブスクリプションキー>";
        private const string SpeechRegion = "<リージョン : japaneastとか>";

        // TextAnalyzeのキーおよびエンドポイント
        private const string TextSubscriptionKey = "<Textのサブスクリプションキー>";
        private const string TextEndPoint = "https://<Textのエンドポイント>.cognitiveservices.azure.com/";

        // 最新の感情分析として何件使うか
        private const int LatestCount = 3;

        // Cognitive Services
        private ApiKeyServiceClientCredentials _credentials;
        private SpeechRecognizer _recognizer;
        private TextAnalyticsClient _client;

        private bool _isRecognizing;

        private readonly List<Sentence> _recognizedContents = new List<Sentence>();
        private string _lastContent;

        public MainPage()
        {
            InitializeComponent();
            EmotionTextBox.Text = MessageBuilder.GetInitialEmotionScoreString(LatestCount);

            InitSpeechRecognize();
            InitTextAnalyticsClient();
        }

        private void InitSpeechRecognize()
        {
            var config = SpeechConfig.FromSubscription(SpeechSubscriptionKey, SpeechRegion);
            config.SpeechRecognitionLanguage = "ja-jp";

            _recognizer = new SpeechRecognizer(config);
            _recognizer.Recognizing += Recognizer_Recognizing;
            _recognizer.Recognized += Recognizer_Recognized;
        }

        private void InitTextAnalyticsClient()
        {
            _credentials = new ApiKeyServiceClientCredentials(TextSubscriptionKey);
            _client = new TextAnalyticsClient(_credentials) { Endpoint = TextEndPoint };
        }

        /// <summary>
        /// 音声認識で文の途中と判断した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e) => await MicOutput(e.Result.Text, false);

        /// <summary>
        /// 音声認識で行末と判断した
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e) => await MicOutput(e.Result.Text, true);

        /// <summary>
        /// テキスト化された結果が返ってきた
        /// </summary>
        /// <param name="text">返ってきたテキスト</param>
        /// <param name="isCompleted">行末かどうか</param>
        /// <returns></returns>
        private async Task MicOutput(string text, bool isCompleted)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (isCompleted)
            {
                var result = await _client.SentimentAsync(text, "ja");
                _recognizedContents.Add(new Sentence
                {
                    Text = _lastContent,
                    Score = result.Score
                });
            }

            _lastContent = isCompleted ? "" : text;

            // 結果表示用のテキストを作成
            var recognizedText = MessageBuilder.BuildRecognizedText(_recognizedContents, _lastContent);
            var totalScore = _recognizedContents.Where(c => c.Score.HasValue).Select(c => c.Score.Value).ToList();

            // スレッドを変えて画面を更新
            await RefreshDisplayAsync(totalScore, recognizedText);
        }

        /// <summary>
        /// 結果表示を更新
        /// </summary>
        /// <param name="totalScore">全文の感情スコア</param>
        /// <param name="recognizedText">分析済みの全文</param>
        private async Task RefreshDisplayAsync(IReadOnlyCollection<double> totalScore, string recognizedText)
        {
            if (!totalScore.Any())
                return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RecognizedTextBox.Text = recognizedText;
                EmotionTextBox.Text = MessageBuilder.BuildEmotionScoreString(totalScore.Average(), totalScore.TakeLast(LatestCount).Average(), LatestCount);

                // TextBoxを最下行まで自動スクロール
                var grid = (Grid)VisualTreeHelper.GetChild(RecognizedTextBox, 0);
                for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
                {
                    var obj = VisualTreeHelper.GetChild(grid, i);
                    if (!(obj is ScrollViewer))
                        continue;

                    ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f, true);
                    break;
                }
            });
        }

        private async void RecognizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isRecognizing = !_isRecognizing;

            if (_isRecognizing)
            {
                StateLabel.Text = "Recognizing";
                RecognizeButton.Content = "Stop to recognize";
                await _recognizer.StartContinuousRecognitionAsync();
            }
            else
            {
                StateLabel.Text = "Stopped";
                RecognizeButton.Content = "Start to recognize";
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            _recognizedContents.Clear();
            _lastContent = "";

            RecognizedTextBox.Text = "";
            EmotionTextBox.Text = MessageBuilder.GetInitialEmotionScoreString(LatestCount);
        }
    }
}

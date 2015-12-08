using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BunnyConc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ResultEntries = new ObservableCollection<ResultEntry>();
            ResultDataGrid.DataContext = ResultEntries;
            stopWatch = new Stopwatch();
            words = new string[0];
            sentences = new string[0];
        }

        string inputString;
        string searchString;
        ObservableCollection<ResultEntry> ResultEntries;
        Stopwatch stopWatch;
        string[] words;
        string[] sentences;

        private void OpenFileDialogButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBlock.Text = openFileDialog.FileName;
                InputTextBox.Text = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputString = InputTextBox.Text;
            if (inputString.Length < 1)
                inputString = null;
        }

        private void GetWords()
        {
            if (inputString != null)
            {
                // average words length
                char[] wordsDelimiterChars = { '~', '!', '@', '#', '$', '%', '^', '&', '*', '\\', '(', ')', '+', '-', '/', '|', '<', '>', '{', '}', '[', ']', ':', ';', ',', '.', '!', '?', '"', '`', ' ' };
                words = inputString.Split(wordsDelimiterChars, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private void GetSentences()
        {
            if (inputString != null)
            {
                /*
                Split the article to sentences and give punctuations back.
                */
                char[] delimiterChars = { '.', ';', '?', '!' };
                MatchCollection punctuationPositions = Regex.Matches(inputString, @"[.;?!]");
                sentences = inputString.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                ///TODO: "...", "???", "!!" not match
                if (punctuationPositions.Count == sentences.Length)
                {
                    for (int i = 0; i < punctuationPositions.Count; i++)
                    {
                        sentences[i] = sentences[i].Trim() + punctuationPositions[i].Value;
                    }
                }
            }
        }

        private void Statistics()
        {
            GetWords();
            GetSentences();

            // average word length
            if (words.Length > 0)
            {
                float wordsCount = 0;
                float charactersCount = 0;
                foreach (string word in words)
                {
                    if (word.Length > 0)
                    {
                        charactersCount += word.Length;
                        wordsCount += 1;
                    }
                }
                float averageWordLength = charactersCount / wordsCount;
                averageWordLengthTextBlock.Text = Math.Round(averageWordLength, 3).ToString();
            }
            else averageWordLengthTextBlock.Text = "N/A";

            // average sentence length
            if (sentences.Length > 0)
            {
                float sentenceLengthCount = 0;
                float sentenceCount = 0;
                foreach (string sentence in sentences)
                {
                    if (sentence.Length > 0)
                    {
                        float sentenceLength = sentence.Count(x => x == ' ') + 1; // word count = space + 1
                        sentenceLengthCount += sentenceLength;
                        sentenceCount += 1;
                    }
                }
                float averageSentenceLength = sentenceLengthCount / sentenceCount;
                averageSentenceLengthTextBlock.Text = Math.Round(averageSentenceLength, 3).ToString();
            }
            else averageSentenceLengthTextBlock.Text = "N/A";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchString = SearchTextBox.Text;
            if (searchString.Length < 1)
                searchString = null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (inputString != null & searchString != null)
            {
                StartTimer();
                Search(inputString, searchString);
                StopTimer();
                TimerTextBlock.Text = stopWatch.Elapsed.TotalSeconds.ToString() + " s";
            }
            else
                MessageBox.Show("Please open a file and input the keyword.");
        }

        private void StopTimer()
        {
            stopWatch.Stop();
        }

        private void StartTimer()
        {
            stopWatch.Restart();
        }

        private void Search(string inputString, string searchString)
        {
            GetSentences();
            /*
            Search keyword in each sentences.
            */
            ResultEntries.Clear();
            foreach (string sentence in sentences)
            {
                MatchCollection results = Regex.Matches(sentence.ToUpper(), string.Format(@"\b{0}\b", Regex.Escape(searchString.ToUpper())));
                if (results.Count > 0)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        ResultEntry entry = new ResultEntry();
                        entry.ID = (ResultEntries.Count + 1).ToString();
                        entry.keyWord = searchString;
                        entry.sentence = sentence;
                        entry.fileName = FilePathTextBlock.Text;
                        ResultEntries.Add(entry);
                    }
                }
            }
            //Jump tp concordancing Tab
            ConcordancingTab.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton_Click(null, null);
        }

        private void StatisticsTab_GotFocus(object sender, RoutedEventArgs e)
        {
            //When the tab is clicked, do Statistics.
            Statistics();
        }
    }

    public class ResultEntry
    {
        public string ID { get; set; }
        public string keyWord { get; set; }
        public string sentence { get; set; }
        public string fileName { get; set; }
    }
}

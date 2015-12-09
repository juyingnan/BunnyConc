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
        int LeftWordCount;
        int RightWordCount;
        int maxLeftRightCount = 3;
        char[] wordsDelimiterChars = { '~', '!', '@', '#', '$', '%', '^', '&', '*', '\\', '(', ')', '+', '-', '/', '|', '<', '>', '{', '}', '[', ']', ':', ';', ',', '.', '!', '?', '"', '`', ' ', '\n', '\t' };

        private void OpenFileDialogButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBlock.Text = openFileDialog.FileName;
                //StringBuilder mySB = new StringBuilder();
                //mySB.Append(File.ReadAllText(openFileDialog.FileName));
                //InputTextBox.Text = mySB.ToString();
                ////InputTextBox.AppendText(File.ReadAllText(openFileDialog.FileName));
                InputTextBox.Text = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputString = InputTextBox.Text;
            if (inputString.Length < 1)
                inputString = "";
        }

        private void GetWords()
        {
            if (inputString != null)
            {
                // average words length
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
            // Get ready
            //InputTextBox_TextChanged(null, null);
            GetWords();
            GetSentences();
            float wordsCount = 0;
            float charactersCount = 0;

            // average word length
            if (words.Length > 0)
            {

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

            // word count
            wordCountTextBlock.Text = wordsCount.ToString();
            allCharactersTextBlock.Text = InputTextBox.Text.Length.ToString();
            allPureCharactersTextBlock.Text = charactersCount.ToString();
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
            //do search
            ResultEntries.Clear();
            int sentenceIndex = 0;
            foreach (string sentence in sentences)
            {
                MatchCollection results = Regex.Matches(sentence.ToUpper(), string.Format(@"\b{0}\b", Regex.Escape(searchString.ToUpper())));
                if (results.Count > 0)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        // Extend Left & Right Part
                        String LPart = sentence.Substring(0, results[i].Index);
                        String RPart = sentence.Substring(results[i].Index + results[i].Value.Length);
                        if (sentenceIndex > 0) LPart = sentences[sentenceIndex - 1] + " " + LPart;
                        if (sentenceIndex + 1 < sentences.Length) RPart = RPart + " " + sentences[sentenceIndex + 1];
                        // Get Left & Right Words
                        string[] LWords = new string[LeftWordCount];
                        for (int j = 0; j < LeftWordCount; j++)
                        {
                            LWords[j] = GetLeftWord(ref LPart);
                            LWords[j] = LWords[j].Replace('\n', ' ');
                            LWords[j] = LWords[j].Replace('\r', ' ');
                        }
                        string[] RWords = new string[RightWordCount];
                        for (int j = 0; j < RightWordCount; j++)
                        {
                            RWords[j] = GetRightWord(ref RPart);
                            RWords[j] = RWords[j].Replace('\n', ' ');
                            RWords[j] = RWords[j].Replace('\r', ' ');
                        }

                        //LN RN length CUT
                        int LRPartLength = 30;
                        if (LPart.Length > LRPartLength) LPart = LPart.Substring(LPart.Length - LRPartLength);
                        if (RPart.Length > LRPartLength) RPart = RPart.Substring(0, LRPartLength);
                        LPart = LPart.Replace('\n', ' ');
                        RPart = RPart.Replace('\n', ' ');
                        LPart = LPart.Replace('\r', ' ');
                        RPart = RPart.Replace('\r', ' ');
                        //int LRPartLength = 3;
                        //int LPartWordsCount = LPart.Split(wordsDelimiterChars).Length - 1;
                        //if (LPartWordsCount >= LRPartLength)
                        //{
                        //    for (int j = 0; j < LPartWordsCount - LRPartLength; j++)
                        //    {
                        //        GetRightWord(ref LPart);
                        //    }
                        //}
                        //int RPartWordsCount = RPart.Count(x => x == ' ');
                        //if (RPartWordsCount >= LRPartLength)
                        //{
                        //    for (int j = 0; j < RPartWordsCount - LRPartLength; j++)
                        //    {
                        //        GetLeftWord(ref RPart);
                        //    }
                        //}

                        // Build data source
                        ResultEntry entry = new ResultEntry(maxLeftRightCount);
                        entry.ID = (ResultEntries.Count + 1).ToString();
                        entry.keyWord = searchString;
                        entry.sentence = sentence;
                        entry.fileName = FilePathTextBlock.Text;
                        for (int j = 0; j < LeftWordCount; j++)
                        {
                            entry.L[j] = LWords[j];
                        }
                        for (int j = 0; j < RightWordCount; j++)
                        {
                            entry.R[j] = RWords[j];
                        }
                        entry.LN = LPart;
                        entry.RN = RPart;
                        ResultEntries.Add(entry);
                    }
                }
                sentenceIndex += 1;
            }
            // Adjust datagrid column width
            foreach (DataGridColumn c in ResultDataGrid.Columns)
                c.Width = DataGridLength.SizeToCells;
            ResultDataGrid.Columns[0].Width = DataGridLength.SizeToHeader;
            // Jump tp concordancing Tab
            ConcordancingTab.Focus();
            if (ResultDataGrid.HasItems)
            {
                ResultDataGrid.ScrollIntoView(ResultDataGrid.Items[0], ResultDataGrid.Columns[8]);
            }
        }

        private string GetRightWord(ref string rPart)
        {
            rPart = rPart.TrimStart();
            int firstSpaceIndex = rPart.IndexOfAny(wordsDelimiterChars);
            string rightWord;
            if (firstSpaceIndex > -1)
            {
                if (firstSpaceIndex == 0)
                {
                    rightWord = rPart.Substring(0, 1);
                    rPart = rPart.Substring(1);
                }
                else
                {
                    rightWord = " " + rPart.Substring(0, firstSpaceIndex);
                    rPart = rPart.Substring(firstSpaceIndex);
                }
            }
            else
            {
                rightWord = rPart;
                rPart = "";
            }
            return rightWord;
        }

        private string GetLeftWord(ref string lPart)
        {
            lPart = lPart.TrimEnd();
            int lastSpaceIndex = lPart.LastIndexOfAny(wordsDelimiterChars);
            string leftWord;
            if (lastSpaceIndex > -1)    // handle the punctuations
            {
                leftWord = lPart.Substring(lastSpaceIndex) + " ";
                lPart = lPart.Substring(0, lastSpaceIndex) + " ";
            }
            else
            {
                leftWord = lPart;
                lPart = "";
            }
            return leftWord;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton_Click(null, null);
        }

        private void LeftCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LeftWordCount = Int32.Parse((LeftCountComboBox.SelectedItem as ComboBoxItem).Content.ToString());
        }

        private void RightCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RightWordCount = Int32.Parse((RightCountComboBox.SelectedItem as ComboBoxItem).Content.ToString());
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchButton_Click(sender, e);
        }

        public class ResultEntry : INotifyPropertyChanged
        {
            public ResultEntry(int leftRightCount)
            {
                L = new string[leftRightCount];
                R = new string[leftRightCount];
            }

            public string ID { get; set; }
            public string keyWord { get; set; }
            public string sentence { get; set; }
            public string fileName { get; set; }
            public string LN { get; set; }
            public string[] L { get; set; }
            public string RN { get; set; }
            public string[] R { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (StatisticsTab !=null & StatisticsTab.IsSelected)
                {
                    Statistics();
                }
            }
        }
    }
}

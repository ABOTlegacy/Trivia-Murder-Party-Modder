﻿using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TriviaMurderPartyModder.Data;

namespace TriviaMurderPartyModder {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        readonly ObservableCollection<Question> questionList = new ObservableCollection<Question>();

        string questionFile = null;

        void MoveRight(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && e.OriginalSource is UIElement source) {
                e.Handled = true;
                source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        public MainWindow() {
            InitializeComponent();
            questions.ItemsSource = questionList;
        }

        string GetTextEntry(ref string source, int from) {
            while (source[from++] != '\"') ;
            int to = from;
            while (!(source[to] == '\"' && source[to - 1] != '\\')) ++to;
            return source.Substring(from, to - from).Replace("\\\"", "\"");
        }

        string MakeTextCompatible(string source) => source.Replace('ő', 'ö').Replace('ű', 'ü').Replace("\"", "\\\"");

        void Import(bool clear) {
            OpenFileDialog opener = new OpenFileDialog {
                Filter = "Trivia Murder Party database (*.jet)|*.jet"
            };
            if (opener.ShowDialog() == true) {
                if (clear)
                    questionList.Clear();
                string contents = File.ReadAllText(questionFile = opener.FileName);
                int position = 0;
                while ((position = contents.IndexOf("\"x\"", position) + 3) != 2) {
                    int id = contents.IndexOf("\"id\"", position) + 4;
                    int text = contents.IndexOf("\"text\"", position) + 6;
                    int choices = contents.IndexOf("\"choices\"", position) + 9;
                    if (id == -1 || text == -1 || choices == -1)
                        continue;
                    id = contents.IndexOf(':', id) + 1;
                    Question imported = new Question {
                        ID = int.Parse(contents.Substring(id, contents.IndexOf(',', id) - id).Trim()),
                        Text = GetTextEntry(ref contents, contents.IndexOf(':', text) + 1)
                    };
                    choices = contents.IndexOf('[', choices) + 1;
                    int correct = contents.IndexOf("\"correct\"", choices);
                    for (int answer = 1; answer <= 4; ++answer) {
                        choices = contents.IndexOf('{', choices) + 1;
                        if (choices <= correct)
                            imported.Correct = answer;
                        imported[answer] = GetTextEntry(ref contents, contents.IndexOf("\"text\"", choices) + 6);
                        choices = contents.IndexOf('}', choices) + 1;
                    }
                    questionList.Add(imported);
                }
            }
        }

        void QuestionImport(object sender, RoutedEventArgs e) => Import(true);

        void QuestionMerge(object sender, RoutedEventArgs e) => Import(false);

        void QuestionIssue(string text) => MessageBox.Show(text, "Question issue", MessageBoxButton.OK, MessageBoxImage.Error);

        void QuestionExport(object sender, RoutedEventArgs e) {
            SaveFileDialog saver = new SaveFileDialog {
                Filter = "Trivia Murder Party database (*.jet)|*.jet"
            };
            if (saver.ShowDialog() == true) {
                StringBuilder output = new StringBuilder("{\"episodeid\":1244,\"content\":[");
                for (int i = 0, end = questionList.Count; i < end; ++i) {
                    Question q = questionList[i];
                    output.Append("{\"x\":false,\"id\":").Append(q.ID);
                    if (q.Text == null) {
                        QuestionIssue(string.Format("No text given for question ID {0}.", q.ID));
                        return;
                    }
                    output.Append(",\"text\":\"").Append(MakeTextCompatible(q.Text)).Append("\",\"pic\": false,\"choices\":[");
                    if (q.Correct < 1 || q.Correct > 4) {
                        QuestionIssue(string.Format("No correct answer set for question \"{0}\".", q.Text));
                        return;
                    }
                    for (int answer = 1; answer <= 4; ++answer) {
                        if (answer != 1)
                            output.Append("},{");
                        else
                            output.Append("{");
                        if (answer == q.Correct)
                            output.Append("\"correct\":true,");
                        if (q[answer] == null) {
                            QuestionIssue(string.Format("No answer {0} for question \"{1}\".", answer, q.Text));
                            return;
                        }
                        output.Append("\"text\":\"").Append(MakeTextCompatible(q[answer])).Append("\"");
                    }
                    if (i != end - 1)
                        output.Append("}]},");
                    else
                        output.Append("}]}]}");
                }
                File.WriteAllText(questionFile = saver.FileName, output.ToString());
            }
        }

        void QuestionImportAudio(object sender, RoutedEventArgs e) {
            if (questions.SelectedItem == null) {
                QuestionIssue("Select the question to import the audio of.");
                return;
            }
            if (questionFile == null) {
                QuestionIssue("The question file has to exist first. Export your work or import an existing question file.");
                return;
            }
            OpenFileDialog opener = new OpenFileDialog {
                Filter = "Ogg Vorbis Audio (*.ogg)|*.ogg"
            };
            if (opener.ShowDialog() == true) {
                Question q = questions.SelectedItem as Question;
                string folder = Path.Combine(Path.GetDirectoryName(questionFile), "TDQuestion", q.ID.ToString());
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "data.jet"),
                    "{\"fields\":[{\"t\":\"B\",\"v\":\"false\",\"n\":\"HasIntro\"},{\"t\":\"B\",\"v\":\"false\",\"n\":\"HasPic\"}," +
                    "{\"t\":\"B\",\"v\":\"false\",\"n\":\"HasVamp\"},{\"t\":\"B\",\"v\":\"false\",\"n\":\"HasChoices\"}," +
                    "{\"t\":\"A\",\"v\":\"aud\",\"n\":\"Q\"},{\"t\":\"A\",\"n\":\"Intro\"},{\"t\":\"A\",\"n\":\"Choices\"}," +
                    "{\"t\":\"A\",\"n\":\"Vamp\"},{\"t\":\"G\",\"n\":\"Pic\"}]}");
                File.Copy(opener.FileName, Path.Combine(folder, "aud.ogg"));
            }
        }

        void QuestionRemove(object sender, RoutedEventArgs e) {
            if (questions.SelectedItem == null) {
                QuestionIssue("Select the question to remove.");
                return;
            }
            questionList.Remove((Question)questions.SelectedItem);
        }
    }
}
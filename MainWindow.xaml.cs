using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> methods = new List<string> { "Парабол", "Трапеций", "Монте-Карло" };
        private const string appId = "WV2386-Y9QKAW7GXR";
        private Parameters parameters = new Parameters();
        private bool testType = true;
        private List<List<double>> coeffsArray = new List<List<double>>();
        public MainWindow()
        {
            InitializeComponent();
            Method.ItemsSource = methods;
            Method.SelectedIndex = 0;
            parameters.Quantity = 4;
            parameters.Eps = 0.01;
            parameters.Step = 0.001;
            parameters.LeftBorder = 1;
            parameters.RightBorder = 4;
            this.DataContext = parameters;
            ExportBtn.IsEnabled = false;
            StartBtn.IsEnabled = false;
            SaveTests.IsEnabled = false;
        }

        private double CallWolfram(List<double> coeffs, double leftBorder, double rightBorder)
        {
            double answer = 0;
            string json;
            string input = "integrate";
            for (int i = 0; i < coeffs.Count; i++)
            {
                // To remove "plus" from last member
                if (i < coeffs.Count - 1)
                {
                    input += $"+{coeffs[i]}*x^{i}+%2b";
                }
                else
                {
                    input += $"+{coeffs[i]}*x^{i}";
                }
            }
            input += $"+from+{leftBorder}+to+{rightBorder}";
            input = input.Replace(',', '.');
            using (var client = new HttpClient())
            {
                var endpoint = new Uri($"https://api.wolframalpha.com/v2/query?input={input}&format=plaintext&output=JSON&appid={appId}&includepodid=Input");
                var res = client.GetAsync(endpoint).Result;
                json = res.Content.ReadAsStringAsync().Result;
            }
            answer = AnswerParser(json);
            //return Math.Round(answer, 5);
            return answer;
        }

        private double AnswerParser(string input)
        {
            double result = 0;
            Regex filter = new Regex(@"plaintext.+[0-9]");
            var match = filter.Match(input);
            string answer = match.Value;
            try
            {
                result = Double.Parse(answer.Split('=')[1].Trim().Replace('.', ','));
            }
            catch
            {
                Regex approxSign = new Regex("≈");
                if (approxSign.IsMatch(input))
                {
                    answer = answer.Split('≈')[1].Trim().Replace('.', ',');
                }
                answer = answer.Split('=')[1].Trim().Replace('.', ',');
                var expNum = answer.Split('×');
                string mantissa = expNum[0].Trim();
                string order = expNum[1].Trim().Split('^')[1];
                result = Double.Parse($"{mantissa}e{order}");
            }
            return result;
        }



        //TODO: Сохранение исходных данных, выполнение тест-кейсов не рандомными, генерация раздельно с исполнением, выводить в отчёт степень полинома,
        // генерировать коэффициенты один раз по максимальной размерности и использовать их


        private void PositiveTest(int quantity, double leftBorder, double rightBorder, double step, int method, double accuracy)
        {
            String tests = testsCases.Text;
            string tempStr = tests;

            Regex scriptFilter = new Regex(@"X = .+[0-9]");
            Regex YEFilter = new Regex(@"YE: S = .+[0 - 9]");

            while (tests.Contains("\n\n"))
            {
                string editText = string.Empty;
                var match = scriptFilter.Match(tests);
                string script = match.Value;
                script = script.Split("=")[1].Trim();
                double YF = Double.Parse(Integral3xCall(script).Split('=')[1].Trim().Replace('.', ','));
                editText += "\nYF: S = " + YF + "\n";

                match = YEFilter.Match(tests);
                string result = match.Value;
                result = result.Split("=")[1].Trim();

                double eps = Math.Abs(double.Parse(result) - YF);
                if (eps < accuracy)
                {
                    editText += "|SYE - SYF| = " + eps + " < EPS" + "\n";
                    editText += "Passed" + "\n";
                }
                else
                {
                    editText += "|SYE - SYF| = " + eps + " > EPS" + "\n";
                    editText += "Not passed" + "\n";
                }
                editText += "\n";

                Regex reg = new Regex("\n\n");
                tests = reg.Replace(tests, editText, 1);
                Result.Text += tests.Remove(tests.IndexOf("\n\n") + 2);
                tests = tests.Remove(tests.IndexOf("Test"), tests.IndexOf("\n\n") + 2);
            }
        }
        private void NegativeTest(int quantity, double leftBorder, double rightBorder, double step, int method, double accuracy)
        {
            String tests = testsCases.Text;
            string tempStr = tests;

            Regex scriptFilter = new Regex(@"X = .+[0-9]");

            while (tests.Contains("\n\n"))
            {
                string editText = string.Empty;
                var match = scriptFilter.Match(tests);
                string script = match.Value;
                script = script.Split("=")[1].Trim();

                if (Double.TryParse(Integral3xCall(script).Split('=')[1].Trim().Replace('.', ','), out double YF))
                {

                    editText += "\nYF: S = " + YF + "\n";
                    editText += "Not passed" + "\n";
                }
                else
                {
                    editText += "\nPassed" + "\n";
                }
                editText += "\n";

                Regex reg = new Regex("\n\n");
                tests = reg.Replace(tests, editText, 1);
                Result.Text += tests.Remove(tests.IndexOf("\n\n") + 2);
                tests = tests.Remove(tests.IndexOf("Test"), tests.IndexOf("\n\n") + 2);
            }
        }

        private string Integral3xCall(string script)
        {
            var process = new Process();

            process.StartInfo.FileName = @"Integral3x.exe";

            process.StartInfo.Arguments = script;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string result = process.StandardOutput.ReadLine();
            process.StandardInput.Write(Key.Enter);
            process.WaitForExit();
            return result;
        }
        private List<string> ScriptGenerator(double leftBorder, double rightBorder, double step, int method, List<double> coeffs, int number, string type)
        {
            string inputCoeffs = "";
            foreach (var coeff in coeffs)
            {
                inputCoeffs += $" {coeff.ToString()}";
            }
            string inputLine = $"{leftBorder} {rightBorder} {step} {method}{inputCoeffs}";
            string inputScriptNumber = $"f.WriteLine(\"TEST {number} {type}\")";
            List<string> comands = new List<string> { inputLine, inputScriptNumber };
            return comands;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            int quantity = parameters.Quantity;
            double leftBorder = parameters.LeftBorder;
            double rightBorder = parameters.RightBorder;
            double step = parameters.Step;
            int method = Method.SelectedIndex + 1;
            double accuracy = parameters.Eps;
            Result.Text = "";

            if (RadioPos.IsChecked == true)
            {
                PositiveTest(quantity, leftBorder, rightBorder, step, method, accuracy);
            }
            if (RadioNeg.IsChecked == true)
            {
                NegativeTest(quantity, leftBorder, rightBorder, step, method, accuracy);
            }
            ExportBtn.IsEnabled = true;
        }

        private void ValidationError(object sender, ValidationErrorEventArgs e)
        {
            foreach (TextBox tb in FindVisualChildren<TextBox>(this))
            {
                if (Validation.GetHasError(tb))
                {
                    StartBtn.IsEnabled = false;
                    return;
                }
            }
            StartBtn.IsEnabled = true;
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null)
                    continue;
                if (ithChild is T t)
                    yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild))
                    yield return childOfChild;
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("report.txt", Result.Text);
        }

        private void RadioPos_Click(object sender, RoutedEventArgs e)
        {
            CasesQuantity.Text = 15.ToString();
            parameters.Quantity = 15;
            testType = true;
        }

        private void RadioNeg_Click(object sender, RoutedEventArgs e)
        {
            CasesQuantity.Text = 64.ToString();
            testType = false;
            parameters.Quantity = 64;
        }

        private void GenerateCoefs_Click(object sender, RoutedEventArgs e)
        {
            if (testType)
            {
                for (int i = 1; i < 16; i++)
                {
                    List<double> coeffs = GenerateCoeffs(i);
                    PolyCoefs.Text += String.Join(" ", coeffs.ToArray());
                    PolyCoefs.Text += " ";
                }
                int coefCount = 0;
                for (int k = 1; k <= parameters.Quantity; k++)
                {
                    //List<double> coeffs = GenerateCoeffs(random.Next(min, max));
                    List<double> coeffs = new List<double>();
                    String ab = PolyCoefs.Text;
                    var arr = ab.Split(" ");
                    for (int j = coefCount; j < coefCount + k; j++)
                    {
                        coeffs.Add(double.Parse(arr[j]));
                    }
                    coefCount += k;
                    double wolframResult = CallWolfram(coeffs, parameters.LeftBorder, parameters.RightBorder);
                    //Вызов Integral3x, сравнение результатов, генерация отчёта
                    List<string> integral3xResult = ScriptGenerator(parameters.LeftBorder, parameters.RightBorder, parameters.Step, Method.SelectedIndex + 1, coeffs, k, "P");
                    //List<string> integral3xResult = Integral3xParser();

                    testsCases.Text += $"Test {k} P" + "\n";
                    testsCases.Text += $"Длина полинома = {coeffs.Count()}" + "\n";
                    testsCases.Text += "X = " + integral3xResult[0] + "\n";
                    testsCases.Text += "YE: S = " + wolframResult + "\n";
                    testsCases.Text += "\n";
                }

            }
            else
            {
                for (int i = 16; i <= 1024; i += 16)
                {
                    List<double> coeffs = GenerateCoeffs(i);
                    PolyCoefs.Text += String.Join(" ", coeffs.ToArray());
                    PolyCoefs.Text += " ";
                }
                int coefCount = 0;
                for (int i = 1; i <= parameters.Quantity; i++)
                {
                    List<double> coeffs = new List<double>();
                    String ab = PolyCoefs.Text;
                    var arr = ab.Split(" ");
                    for (int j = coefCount; j < coefCount + 16 * i; j++)
                    {
                        coeffs.Add(double.Parse(arr[j]));
                    }
                    coefCount += 16 * i;
                    //Вызов Integral3x, сравнение результатов, генерация отчёта
                    List<string> integral3xResult = ScriptGenerator(parameters.LeftBorder, parameters.RightBorder, parameters.Step, Method.SelectedIndex + 1, coeffs, i, "N");
                    //List<string> integral3xResult = Integral3xParser();

                    testsCases.Text += $"Test {i} N" + "\n";
                    testsCases.Text += $"Длина полинома = {coeffs.Count()}" + "\n";
                    testsCases.Text += "X = " + integral3xResult[0] + "\n";
                    testsCases.Text += "YE: S = " + "Сообщение о превышении степени полинома" + "\n";
                    testsCases.Text += "\n";
                }
            }
            StartBtn.IsEnabled = true;
            SaveTests.IsEnabled = true;
        }

        private List<double> GenerateCoeffs(int quantity)
        {
            List<double> doubles = new List<double>();
            const double min = -100.0;
            const double max = 100.0;
            Random random = new Random();
            for (int i = 0; i < quantity; i++)
            {
                double value = random.NextDouble() * (max - min) + min;
                switch (quantity > 10)
                {
                    case true:
                        {
                            doubles.Add(Math.Round(value, 1));
                            break;
                        }
                    default:
                        {
                            doubles.Add(Math.Round(value, 5));
                            break;
                        }
                }
            }
            return doubles;
        }

        private void InputTests_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == false) return;
            string path = openFileDialog.FileName;
            testsCases.Text = File.ReadAllText(path);
            StartBtn.IsEnabled = true;
            InputFileParser();
        }

        private void InputFileParser()
        {
            try
            {
                Regex reg = new Regex(@"Test+.[0-8]+.[N,P]");
                var match = reg.Match(testsCases.Text);
                string type = match.Value;
                type = type.Split("1")[1].Trim();
                if (type == "P")
                {
                    CasesQuantity.Text = 15.ToString();
                    parameters.Quantity = 15;
                    RadioPos.IsChecked = true;
                }
                else
                {
                    CasesQuantity.Text = 64.ToString();
                    parameters.Quantity = 64;
                    RadioNeg.IsChecked = true;
                }
            }
            catch { }
        }
        private void SaveTests_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == false) return;
            string path = saveFileDialog.FileName;
            File.WriteAllText(path + ".txt", testsCases.Text);
        }
    }
}

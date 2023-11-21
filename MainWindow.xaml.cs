using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tester {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private List<string> methods = new List<string> { "Парабол", "Трапеций", "Монте-Карло" };
		private const string appId = "WV2386-Y9QKAW7GXR";
		public MainWindow() {
			InitializeComponent();
			Method.ItemsSource = methods;
			Method.SelectedIndex = 0;
			Parameters parameters = new Parameters();
			parameters.Quantity = 4;
			parameters.Eps = 0.01;
			parameters.Step = 0.001;
			parameters.LeftBorder = 1;
			parameters.RightBorder = 4;
			this.DataContext = parameters;
		}

		private double CallWolfram(List<double> coeffs, double leftBorder, double rightBorder) {
			double answer = 0;
			string json;
			string input = "integrate";
			for (int i = 0; i < coeffs.Count; i++) {
				// To remove "plus" from last member
				if (i < coeffs.Count - 1) {
					input += $"+{coeffs[i]}*x^{i}+%2b";
				} else {
					input += $"+{coeffs[i]}*x^{i}";
				}
			}
			input += $"+from+{leftBorder}+to+{rightBorder}";
			input = input.Replace(',', '.');
			using (var client = new HttpClient()) {
				var endpoint = new Uri($"https://api.wolframalpha.com/v2/query?input={input}&format=plaintext&output=JSON&appid={appId}&includepodid=Input");
				var res = client.GetAsync(endpoint).Result;
				json = res.Content.ReadAsStringAsync().Result;
			}
			answer = AnswerParser(json);
			//return Math.Round(answer, 5);
			return answer;
		}

		private double AnswerParser(string input) {
			double result = 0;
			Regex filter = new Regex(@"plaintext.+[0-9]");
			var match = filter.Match(input);
			string answer = match.Value;
			try {
				result = Double.Parse(answer.Split('=')[1].Trim().Replace('.', ','));
			}
			catch {
				Regex approxSign = new Regex("≈");
				if (approxSign.IsMatch(input)) {
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

		private List<double> GenerateCoeffs(int quantity) {
			List<double> doubles = new List<double>();
			const double min = -100.0;
			const double max = 100.0;
			Random random = new Random();
			for (int i = 0; i < quantity; i++) {
				double value = random.NextDouble() * (max - min) + min;
				switch (quantity > 10) {
					case true: {
							doubles.Add(Math.Round(value, 1));
							break;
						}
					default: {
							doubles.Add(Math.Round(value, 5));
							break;
						}
				}
			}
			return doubles;
		}
		private void PositiveTest(int quantity, double leftBorder, double rightBorder, double step, int method, double accuracy) {
			const int min = 1;
			const int max = 15;
			Random random = new Random();
			for (int i = 0; i < quantity; i++) {
				List<double> coeffs = GenerateCoeffs(random.Next(min, max));
				double wolframResult = CallWolfram(coeffs, leftBorder, rightBorder);
				//Вызов Integral3x, сравнение результатов, генерация отчёта
				Integral3xCall(leftBorder, rightBorder, step, method, coeffs, i, "P");
				List<string> integral3xResult = Integral3xParser();

				Result.Text += integral3xResult[0] + "\n";
				Result.Text += integral3xResult[1] + "\n";
				double YF = Double.Parse(integral3xResult[2].Split('=')[1].Trim().Replace('.', ','));
				Result.Text += "YE: S = " + wolframResult + "\n";
				Result.Text += "YF: S = " + YF + "\n";
				double eps = Math.Abs(wolframResult - YF);
				if (eps < accuracy) {
					Result.Text += "|SYE - SYF| = " + eps + "< EPS" + "\n";
					Result.Text += "Passed" + "\n";
				} else {
					Result.Text += "|SYE - SYF| = " + eps + "> EPS" + "\n";
					Result.Text += "Not passed" + "\n";
				}
				Result.Text += "\n";
			}
		}

		private void NegativeTest(int quantity, double leftBorder, double rightBorder, double step, int method, double accuracy) {
			for (int i = 1; i <= quantity; i++) {
				List<double> coeffs = GenerateCoeffs(i*16);
				//Вызов Integral3x, сравнение результатов, генерация отчёта
				Integral3xCall(leftBorder, rightBorder, step, method, coeffs, i, "N");
				List<string> integral3xResult = Integral3xParser();

				Result.Text += integral3xResult[0] + "\n";
				Result.Text += integral3xResult[1] + "\n";
				Result.Text += "YE: S = " + "Сообщение о превышении степени полинома" + "\n";
				if (double.TryParse(integral3xResult[2].Split('=')[1].Trim().Replace('.', ','), out double number)) {
					Result.Text += "YF: S = " + number + "\n";
					Result.Text += "Not passed" + "\n";
				} else {
					Result.Text += "YF: " + integral3xResult[2] + "\n";
					Result.Text += "Passed" + "\n";
				}
				Result.Text += "\n";
			}
		}
		private List<string> Integral3xParser() {
			List<string> res = new List<string>();
			foreach (string line in File.ReadLines("report.txt")) {
				res.Add(line);
			}
			File.Delete("report.txt");
			return res;
			//string resualt = File.ReadLines("report.txt").ElementAtOrDefault(2);
		}
		private void Integral3xCall(double leftBorder, double rightBorder, double step, int method, List<double> coeffs, int number, string type) {
			List<string> commands = ScriptGenerator(leftBorder, rightBorder, step, method, coeffs, number, type);
			string script = "";
			foreach (string line in File.ReadLines("Script.TXT")) {
				if (line == "X") {
					script += commands[0] + "\n";
				} else {
					if (line == "N T") {
						script += commands[1] + "\n";
					} else {
						script += line + "\n";
					}
				}
			}
			File.WriteAllText(Path.Combine(Path.GetTempPath(), "test.vbs"), script);
			Process vbs = Process.Start(new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "test.vbs")) { UseShellExecute = true });
			vbs.WaitForExit();
			File.Delete(Path.Combine(Path.GetTempPath(), "test.vbs"));
		}

		private List<string> ScriptGenerator(double leftBorder, double rightBorder, double step, int method, List<double> coeffs, int number, string type) {
			string inputCoeffs = "";
			foreach (var coeff in coeffs) {
				inputCoeffs += $" {coeff.ToString()}";
			}
			string inputLine = $"X = \"{leftBorder} {rightBorder} {step} {method}{inputCoeffs}\"";
			string inputScriptNumber = $"f.WriteLine(\"TEST {number} {type}\")";
			List<string> comands = new List<string> { inputLine, inputScriptNumber };
			return comands;
		}

		private void StartBtn_Click(object sender, RoutedEventArgs e) {
			int quantity = int.Parse(CasesQuantity.Text);
			double leftBorder = double.Parse(LeftBorder.Text);
			double rightBorder = double.Parse(RightBorder.Text);
			double step = double.Parse(Step.Text);
			int method = Method.SelectedIndex + 1;
			double accuracy = double.Parse(Accuracy.Text);
			Result.Text = "";

			if (RadioPos.IsChecked == true) {
				PositiveTest(quantity, leftBorder, rightBorder, step, method, accuracy);
			}
			if (RadioNeg.IsChecked == true) {
				NegativeTest(quantity, leftBorder, rightBorder, step, method, accuracy);
			}
		}

		private void ValidationError(object sender, ValidationErrorEventArgs e) {
			foreach (TextBox tb in FindVisualChildren<TextBox>(this)) {
				if (Validation.GetHasError(tb)) {
					StartBtn.IsEnabled = false;
					return;
				}
			}
			StartBtn.IsEnabled = true;
		}
		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
			if (depObj == null)
				yield return (T)Enumerable.Empty<T>();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
				DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
				if (ithChild == null)
					continue;
				if (ithChild is T t)
					yield return t;
				foreach (T childOfChild in FindVisualChildren<T>(ithChild))
					yield return childOfChild;
			}
		}
	}
}

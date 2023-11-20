using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
		}

		private double CallWolfram(List<double> coeffs, double leftBorder, double rightBorder) {
			double answer = 0;
			string json;
			string input = "integrate";
			for (int i = 0; i < coeffs.Count; i++) {
				// To remove "plus" from last member
				if (i < coeffs.Count - 1) {
					input += $"+{coeffs[i]}*x^{i}+plus";
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
			return Math.Round(answer, 5);
		}

		private double AnswerParser(string input) {
			double result = 0;
			Regex filter = new Regex(@"plaintext.+[0-9]");
			var match = filter.Match(input);
			string answer = match.Value;
			result = Double.Parse(answer.Split('=')[1]);
			return result;
		}

		private List<double> GenerateCoeffs(int quantity) {
			List<double> doubles = new List<double>();
			const double min = -100.0;
			const double max = 100.0;
			Random random = new Random();
			for (int i = 0; i < quantity; i++) {
				doubles.Add(random.NextDouble() * (max - min) + min);
			}
			return doubles;
		}
	}
}

using System.Collections.Generic;
using System.ComponentModel;

namespace Tester {
	public class Parameters : IDataErrorInfo {
		public int Quantity { get; set; }
		public double LeftBorder { get; set; }
		public double RightBorder { get; set; }
		public double Step { get; set; }
		public double Eps { get; set; }
		public bool IsPositive {  get; set; }
		public bool IsNegative {  get; set; }
		public string Error {
			get { throw new System.NotImplementedException(); }
		}

		public string this[string columnName] {
			get {
				string error = string.Empty;
				switch (columnName) {
					case "Quantity": {
							if (Quantity <= 0) {
								error = "Количество должно быть больше 0";
							}
							if (IsPositive & Quantity > 15) {
								error = "Количество позитивных тест-кейсов не может быть больше 15";
							}
							if (IsNegative & Quantity > 64) {
								error = "Количество негативных тест-кейсов не может быть больше 64";
							}
							break;

					}
					case "Eps": {
							if (Eps <= 0) {
								error = "Погрешность должна быть больше 0";
							}
							break;
					}
				}
				return error;
			}
	}

		public Parameters() {
			IsPositive = true;
		}
	}
}

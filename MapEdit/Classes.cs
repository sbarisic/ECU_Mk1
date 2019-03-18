using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid;
using unvell.ReoGrid.Graphics;

namespace MapEdit {
	public static class Utils {
		public static double Lerp(double A, double B, float X) {
			if (X <= 0)
				return A;

			if (X >= 1)
				return B;

			return A + ((B - A) * X);
		}

		public static byte Lerp(byte A, byte B, float X) {
			if (X <= 0)
				return A;

			if (X >= 1)
				return B;

			return (byte)(A + ((double)(B - A) * X));
		}

		public static SolidColor Lerp(SolidColor A, SolidColor B, float X) {
			return new SolidColor(Lerp(A.R, B.R, X), Lerp(A.G, B.G, X), Lerp(A.B, B.B, X));
		}

		public static SolidColor Lerp(SolidColor A, SolidColor B, SolidColor C, float X) {
			if (X < 1)
				return Lerp(A, B, X);

			return Lerp(B, C, X - 1);
		}

		public static SolidColor Lerp(SolidColor A, SolidColor B, SolidColor C, float MinVal, float MaxVal, float X, float Center = float.PositiveInfinity) {
			float Middle = MinVal + ((MaxVal - MinVal) / 2);

			if (!float.IsInfinity(Center))
				Middle = Center;

			if (X < Middle)
				return Lerp(A, B, (X - MinVal) / (Middle - MinVal));

			return Lerp(B, C, (X - Middle) / (MaxVal - Middle));
		}

		public static double Clamp(double Val, double Min, double Max) {
			if (Val < Min)
				return Min;

			if (Val > Max)
				return Max;

			return Val;
		}
	}

	public enum EditMode {
		Grid,
		Property
	}

	[DesignerCategory("Misc")]
	public class EditableData {
		public EditMode EditMode;

		public string XName;
		public string YName;
		public string ValueName;
		public object DefaultValue;
		public Worksheet Worksheet;

		[Browsable(false)]
		public virtual bool DataEnabled {
			get {
				return true;
			}
		}

		public EditableData(EditMode EditMode) {
			this.EditMode = EditMode;
			XName = "X Axis";
			YName = "Y Axis";
			ValueName = "Value";
			Worksheet = null;
		}

		protected void GenerateXAxis(Worksheet Sheet, int Count, Func<int, string> GenName) {
			Sheet.ColumnCount = Count;
			Sheet.SetColumnsWidth(0, Count, 40);

			for (int i = 0; i < Count; i++)
				Sheet.ColumnHeaders[i].Text = GenName(i);
		}

		protected void GenerateYAxis(Worksheet Sheet, int Count, Func<int, string> GenName) {
			Sheet.RowCount = Count;

			for (int i = 0; i < Count; i++)
				Sheet.RowHeaders[i].Text = GenName(i);
		}

		public virtual void PopulateSheet(Worksheet Sheet) {
			for (int y = 0; y < Sheet.RowCount; y++)
				for (int x = 0; x < Sheet.ColumnCount; x++) {

					Sheet.Cells[y, x].Data = GetDefaultValue(x, y, Sheet.ColumnCount, Sheet.RowCount, (double)x / Sheet.ColumnCount, (double)y / Sheet.RowCount);
				}
		}

		public virtual object GetDefaultValue(int X, int Y, int W, int H, double WP, double HP) {
			return DefaultValue;
		}

		public virtual void ColorCell(int X, int Y, object Value, ref Cell C) {
		}
	}

	[DesignerCategory("Engine"), DisplayName("Engine data")]
	public class EngineData : EditableData {
		public EngineData() : base(EditMode.Property) {
		}

		[Description("Number of cylinders"), Category("Basic")]
		public int Cylinders { get; set; } = 1;

		[Display(Order = 1)]
		[Description("Engine displacement [cm^3]"), Category("Basic")]
		public int Displacement { get; set; } = 200;

		// Rev limiter

		[Description("Enable limiter"), Category("Rev limiter")]
		public bool EnableRevLimit { get; set; } = true;

		[Description("RPM above which the rev limiter activates [RPM]"), Category("Rev limiter")]
		public int RevLimit { get; set; } = 11000;

		[Description("RPM below which the rev limiter deactivates [RPM]"), Category("Rev limiter")]
		public int RevLimitStop { get; set; } = 10950;

		// Lambda

		[Description("Enable lambda sensor. If disabled, open loop fuel injection map is used."), Category("Lambda")]
		public bool LambdaEnabled { get; set; } = false;

		[Description("Sensor min voltage"), Category("Lambda")]
		public float LambdaStart { get; set; } = 0.1f;

		[Description("Sensor max voltage"), Category("Lambda")]
		public float LambdaEnd { get; set; } = 0.9f;

		[Description("Sensor min A/F ratio"), Category("Lambda")]
		public float LambdaRatioBottom { get; set; } = 6.0f;

		[Description("Sensor max A/F ratio"), Category("Lambda")]
		public float LambdaRatioTop { get; set; } = 17.0f;

		// Fuel injector

		[Description("Minimum allowed injector pulse width [ms]"), Category("Fuel injector")]
		public float MinPulseWidth { get; set; } = 1.0f;

		[Description("Maximum allowed injector pulse width [ms]"), Category("Fuel injector")]
		public float MaxPulseWidth { get; set; } = 5.0f;

		[Description("Calculated injector pulse range [ms]"), Category("Fuel injector")]
		public float PulseRange {
			get {
				return MaxPulseWidth - MinPulseWidth;
			}
		}
	}

	[DesignerCategory("Maps"), DisplayName("Fuel injection map")]
	public class InjectionMap : EditableData {
		EngineData EngineData;

		public override bool DataEnabled => !EngineData.LambdaEnabled;

		public InjectionMap(EngineData EngineData) : base(EditMode.Grid) {
			this.EngineData = EngineData;

			XName = "Engine speed [RPM]";
			YName = "Engine load [%]";
			ValueName = "Injector time [ms]";
			DefaultValue = 10.0;
		}

		public override void PopulateSheet(Worksheet Sheet) {
			GenerateXAxis(Sheet, 26, (i) => string.Format("{0}", i * 500));
			GenerateYAxis(Sheet, 21, (i) => string.Format("{0} %", i * 5));
			base.PopulateSheet(Sheet);
		}

		public override void ColorCell(int X, int Y, object Value, ref Cell C) {
			C.Style.BackColor = SolidColor.Transparent;

			if (Value is double Num)
				C.Style.BackColor = Utils.Lerp(new SolidColor(104, 162, 255), SolidColor.Green, new SolidColor(255, 70, 61), EngineData.MinPulseWidth, EngineData.MaxPulseWidth, (float)Num);
		}

		public override object GetDefaultValue(int X, int Y, int W, int H, double WP, double HP) {
			WP += 0.25;
			HP += 0.3;
			double Val = Utils.Lerp(EngineData.MinPulseWidth, EngineData.MaxPulseWidth, (float)(WP * HP * 0.7));

			Val = Utils.Clamp(Val, EngineData.MinPulseWidth, EngineData.MaxPulseWidth);
			return Math.Round(Val, 2);
		}
	}

	[DesignerCategory("Maps"), DisplayName("Spark advance map")]
	public class AdvanceMap : EditableData {
		EngineData EngineData;

		public AdvanceMap(EngineData EngineData) : base(EditMode.Grid) {
			this.EngineData = EngineData;

			XName = "Engine speed [RPM]";
			YName = "Engine load [%]";
			ValueName = "Spark advance [Deg, 0 at TDC]";
			DefaultValue = -2.0;
		}

		public override void PopulateSheet(Worksheet Sheet) {
			GenerateXAxis(Sheet, 26, (i) => string.Format("{0}", i * 500));
			GenerateYAxis(Sheet, 21, (i) => string.Format("{0} %", i * 5));
			base.PopulateSheet(Sheet);
		}

		public override void ColorCell(int X, int Y, object Value, ref Cell C) {
			C.Style.BackColor = SolidColor.Transparent;

			if (Value is double Num)
				C.Style.BackColor = Utils.Lerp(new SolidColor(66, 134, 244), SolidColor.Green, new SolidColor(255, 158, 89), -20, 5, (float)Num, Center: 0);
		}

		public override object GetDefaultValue(int X, int Y, int W, int H, double WP, double HP) {
			double Val = 5 - 25 * (WP);

			Val = Utils.Clamp(Val, -30, 30);
			return Math.Round(Val, 2);
		}
	}
}

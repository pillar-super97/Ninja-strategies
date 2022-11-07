#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Tak : Strategy
	{
        // STRATEGY INDICATORS
        private RSI rsi;
		private CCI cci;
		private ATR atr;
		private bool firstGreen;
		private bool firstRed;

        public int MorningStart;
        public int MorningEnd;
        public int AfternoonStart;
        public int AfternoonEnd;
        public int EveningStart;
        public int EveningEnd;
        public int Offline;

        public enum MAType
        {
            EMA,
            DEMA,
            HMA,
            KAMA,
            SAM,
            TEMA,
            WMA,
            VWMA,
            VOLMA,
            SMMA
        }

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Tak";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				MaxOpenPos					= 10;
				Breakeven					= true;
				Fast		= 10;
				Slow		= 25;
//				AddPlot(Brushes.Orange, "MovingAverage");
				firstGreen = true;
				firstRed = true;
				backgroundColor = false;

                // Trading Time
                MorningStart = ToTime(1, 00, 00);
                MorningEnd = ToTime(9, 00, 00);
                AfternoonStart = ToTime(9, 00, 00);
                AfternoonEnd = ToTime(17, 00, 00);
                EveningStart = ToTime(13, 00, 00);
                EveningEnd = ToTime(20, 00, 00);
                Offline = ToTime(20, 01, 00);

                tradingSessionMorning = true;
                tradingSessionAfternoon = true;
                tradingSessionEvening = true;
                
                
                switch (maType)
                {
                    case MAType.EMA:
                        break;
                    case MAType.DEMA:
                        break;
                }
            }
			else if (State == State.Configure)
			{
			}
			
			// SHOW INDICATORS
			else if (State == State.DataLoaded)
			{
				if (showMA == true) {
					SMMA smma = SMMA(20);
					AddChartIndicator(smma);
					ChartIndicators[0].Plots[0].Brush = Brushes.LimeGreen;
					ChartIndicators[0].Plots[0].Width = 3;
					

//					AddChartIndicator(SMA(Fast));
//					AddChartIndicator(SMA(Slow));
				}
				
				if (showRSI == true) {
					rsi = RSI(14, 3);
					AddChartIndicator(rsi);
				}
				
				if (showCCI == true) {
					cci = CCI(20);
					AddChartIndicator(cci);
				}
				
				if (showATR == true) {
					atr = ATR(20);
					AddChartIndicator(atr);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			SMMA smma = SMMA(20);
			rsi = RSI(14, 3);
            cci = CCI(20);

			if(backgroundColor) {
	            if (Close[0] > smma[0])
	                BackBrush = Brushes.DarkGreen;
	            else
	                BackBrush = Brushes.DarkRed;
			}

            if (Bars.IsFirstBarOfSession) {
				firstGreen = true;
				firstRed = true;
			}

            // +------------------------------------------------------------+
            // | Holiday Trading                                            |
            // +------------------------------------------------------------+
            if (Time[0].Month == 1 && Time[0].Day == 1 && holiday_1 ||
                Time[0].Month == 1 && Time[0].Day == 17 && holiday_2 ||
                Time[0].Month == 2 && Time[0].Day == 14 && holiday_3 ||
                Time[0].Month == 2 && Time[0].Day == 21 && holiday_4 ||
                Time[0].Month == 4 && Time[0].Day == 15 && holiday_5 ||
                Time[0].Month == 4 && Time[0].Day == 17 && holiday_6 ||
                Time[0].Month == 5 && Time[0].Day == 8 && holiday_7 ||
                Time[0].Month == 5 && Time[0].Day == 30 && holiday_8 ||
                Time[0].Month == 6 && Time[0].Day == 3 && holiday_9 ||
                Time[0].Month == 6 && Time[0].Day == 19 && holiday_10 ||
                Time[0].Month == 7 && Time[0].Day == 4 && holiday_11 ||
                Time[0].Month == 9 && Time[0].Day == 5 && holiday_12 ||
                Time[0].Month == 10 && Time[0].Day == 10 && holiday_13 ||
                Time[0].Month == 10 && Time[0].Day == 31 && holiday_14 ||
                Time[0].Month == 11 && Time[0].Day == 11 && holiday_15 ||
                Time[0].Month == 11 && Time[0].Day == 24 && holiday_16 ||
                Time[0].Month == 12 && Time[0].Day == 25 && holiday_17
                )
                return;

            //if(holiday_18)
            //{
            //    string day = holiday_18_day.Split('.')[0];
            //    string month = holiday_18_day.Split('.')[0];
            //    if (Time[0].Month == Int16.Parse(month) && Time[0].Day == Int16.Parse(day))
            //        return;
            //}

            // +------------------------------------------------------------+
            // | Tradable Hours                                             |
            // +------------------------------------------------------------+
            if ((ToTime(Time[0]) >= MorningStart && ToTime(Time[0]) < MorningEnd && tradingSessionMorning) 
                || (ToTime(Time[0]) >= AfternoonStart && ToTime(Time[0]) < AfternoonEnd && tradingSessionAfternoon
                || (ToTime(Time[0]) >= EveningStart && ToTime(Time[0]) < EveningEnd && tradingSessionEvening)))
            {
                // LONG RULES
                // A - CROSSOVER TRADES
                if (CrossAbove(Close, smma, 1) & CrossAbove(SMMA(Fast), SMMA(Slow), 1) & CrossAbove(rsi, 50, 1) & Close[0] > Open[0] & !firstGreen)
                {
                    EnterLong();
                }

                // B - PULLBACK TRADES:
                if (CrossAbove(Close, smma, 1) & CrossAbove(rsi, 50, 1) & CrossAbove(cci, 100, 1) & Close[0] > Open[0] & !firstGreen)
                {
                    EnterLong();
                }

                // SHORT RULES
                // A - CROSSOVER TRADES:
                if (CrossBelow(Close, smma, 1) & CrossBelow(SMMA(Fast), SMMA(Slow), 1) & CrossBelow(rsi, 50, 1) & Close[0] < Open[0] & !firstRed)
                {
                    EnterShort();
                }

                // B - PULLBACK TRADES:
                if (CrossBelow(Close, smma, 1) & CrossBelow(rsi, 50, 1) & CrossBelow(cci, 100, 1) & Close[0] < Open[0] & !firstRed)
                {
                    EnterShort();
                }

                if (firstGreen & Close[0] > Open[0])
                {
                    firstGreen = false;
                }
                if (firstRed & Close[0] < Open[0])
                {
                    firstRed = false;
                }
            }

            // +------------------------------------------------------------+
            // | 20:00 - 20:01 Close Positions                              |
            // +------------------------------------------------------------+
            //if ((ToTime(Time[0]) >= EveningEnd) && (ToTime(Time[0]) <= Offline))
            //{
            //    // Close any open positions before 20:01
            //    Print("Closing positions " + Time[0].ToString());
            //    ExitLong();
            //    ExitShort();
            //}
        }
		void Volumesize(){
			// Prints the current value VOL
			double value = VOL()[0];
			Print("The current VOL value is " + value.ToString());
		}
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxOpenPos", Description="Maximum number of open position : on/off", Order=1, GroupName="Parameters")]
		public int MaxOpenPos
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Breakeven", Order=2, GroupName="Parameters")]
		public bool Breakeven
		{ get; set; }
		
        [NinjaScriptProperty]
        [Display(Name = "Background color", Order = 3, GroupName = "Parameters")]
        public bool backgroundColor
        { get; set; }


        // +------------------------------------------------------------+
        // | Indicators                                                 |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "MA Type", GroupName = "Indicators", Order = 1)]
        public MAType maType
        { get; set; }

        [NinjaScriptProperty]
		[Display(Name="MA Indicator", Order=2, GroupName="Indicators")]
		public bool showMA
		{ get; set; }

        [NinjaScriptProperty]
		[Display(Name="RSI Indicator", Order=3, GroupName= "Indicators")]
		public bool showRSI
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="CCI Indicator", Order=4, GroupName= "Indicators")]
		public bool showCCI
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ATR Indicator", Order=5, GroupName= "Indicators")]
		public bool showATR
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MovingAverage
		{
			get { return Values[0]; }
		}
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
		public int Fast
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
		public int Slow
		{ get; set; }

        // +------------------------------------------------------------+
        // | Trading sessions                                           |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Trading (01:00 - 09:00)", Order = 0, GroupName = "Session")]
        public bool tradingSessionMorning
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trading (09:00 - 17:00)", Order = 1, GroupName = "Session")]
        public bool tradingSessionAfternoon
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trading (13:00 - 20:00)", Order = 2, GroupName = "Session")]
        public bool tradingSessionEvening
        { get; set; }


        // +------------------------------------------------------------+
        // | Holidays                                                   |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "01.01 New Year's Day", Order = 1, GroupName = "Holiday")]
        public bool holiday_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "17.01 M L King Day", Order = 2, GroupName = "Holiday")]
        public bool holiday_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "14.02 Valentine's Day", Order = 3, GroupName = "Holiday")]
        public bool holiday_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "21.02 Presidents' Day", Order = 4, GroupName = "Holiday")]
        public bool holiday_4
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "15.04 Good Friday", Order = 5, GroupName = "Holiday")]
        public bool holiday_5
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "17.04 Easter Sunday", Order = 6, GroupName = "Holiday")]
        public bool holiday_6
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "08.05 Mother's Day", Order = 7, GroupName = "Holiday")]
        public bool holiday_7
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "30.05 Memorial Day", Order = 8, GroupName = "Holiday")]
        public bool holiday_8
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "03.06 National Donut Day", Order = 9, GroupName = "Holiday")]
        public bool holiday_9
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "19.06 Father's Day", Order = 10, GroupName = "Holiday")]
        public bool holiday_10
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "04.07 Independence Day", Order = 11, GroupName = "Holiday")]
        public bool holiday_11
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "05.09 Labor Day", Order = 12, GroupName = "Holiday")]
        public bool holiday_12
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "10.10 Columbus Day", Order = 13, GroupName = "Holiday")]
        public bool holiday_13
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "31.10 Halloween", Order = 14, GroupName = "Holiday")]
        public bool holiday_14
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "11.11 Veterans Day", Order = 15, GroupName = "Holiday")]
        public bool holiday_15
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "24.11 Thanksgiving Day", Order = 16, GroupName = "Holiday")]
        public bool holiday_16
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "25.12 Christmas", Order = 17, GroupName = "Holiday")]
        public bool holiday_17
        { get; set; }
		
        [NinjaScriptProperty]
        [Display(Name = "Holiday_18", Order = 18, GroupName = "Holiday")]
        public bool holiday_18
        { get; set; }

        //[NinjaScriptProperty]
        //[Display(Name = "Holiday_18_Day_Dot_Month", Order = 19, GroupName = "Holiday")]
        //public string holiday_18_day
        //{ get; set;   


        // +------------------------------------------------------------+
        // | Stop loss / Target Tyoe                                    |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Target Profit", Order = 0, GroupName = "Target Profit")]
        public bool tp
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop Loss", Order = 1, GroupName = "Session")]
        public bool sl
        { get; set; }


        #endregion
    }

}

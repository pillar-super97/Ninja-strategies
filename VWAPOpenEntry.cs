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
	public class VWAPOpenEntry : Strategy
	{
        // flag part 
        public bool flag9;      // for EMA Exit
        public bool flag13;
        public bool flag20;
        public bool flag30;
        private bool breakPoint = false;    // for identify break

        // timeframe part
        public int MorningStart;
        public int MorningEnd;
        public int AfternoonStart;
        public int AfternoonEnd;
        public int EveningStart;
        public int EveningEnd;
        public int CloseTime;

        // threshold value part
        public double sl;
        public double pt;
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"VWAP Open Entry";
				Name										= "VWAPOpenEntry";
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

                // Common Prameters
                QuantityAmount = 10;
                PositionSize = 1;
                EMA_Exit = false;
                // Entries
                Entry_2 = true;

                // Entry 2 's general parameters
                EMAAboveVWAP            = true;
                EMABelowVWAP            = true;
                EMAAboveVWAP_amt        = 0.5;   // 1D EMA9 is 0.5$ above vwap
                StopLossValue           = 0.00125; // 0.125% of SPY below from vwap
                ProfitTargetValue       = 0.00375;  // 0.375% of SPY above from vwap
                StopLoss                = true;
                ProfitTarget            = true;

                // Trading Time
                MorningStart = ToTime(8, 30, 00);
                MorningEnd = ToTime(11, 00, 00);
                AfternoonStart = ToTime(13, 00, 00);
                AfternoonEnd = ToTime(14, 30, 00);
                EveningStart = ToTime(16, 00, 00);
                EveningEnd = ToTime(19, 00, 00);
                CloseTime = ToTime(11, 30, 00);

                // Trading session
                TradingSessionMorning = true;
                TradingSessionAfternoon = true;
                TradingSessionEvening = true;

                // Indicators
                showVWAP = true;
                showEMA = true;

            }
			else if (State == State.Configure)
			{                
                // Maximum number of entries allowed per direction
                EntriesPerDirection = PositionSize;
                EntryHandling = EntryHandling.AllEntries;				
			}

            else if (State == State.DataLoaded)
            {
                if (showVWAP)
                {
                    VWAP vwap = VWAP();
                    vwap.NumDeviations = 0;

                    AddChartIndicator(vwap);
                    ChartIndicators[0].Plots[0].Brush = Brushes.LimeGreen;
                    ChartIndicators[0].Plots[0].DashStyleHelper = DashStyleHelper.Solid;
                    ChartIndicators[0].Plots[0].Width = 3;
                }

                if (showEMA)
                {
                    EMA EMA9 = EMA(9);   // timeframe is 1 day
                    AddChartIndicator(EMA9);
                }

            }
        }

		protected override void OnBarUpdate()
        {

			
            if ((ToTime(Time[0]) >= MorningStart && ToTime(Time[0]) < MorningEnd && TradingSessionMorning)
                || (ToTime(Time[0]) >= AfternoonStart && ToTime(Time[0]) < AfternoonEnd && TradingSessionAfternoon
                || (ToTime(Time[0]) >= EveningStart && ToTime(Time[0]) < EveningEnd && TradingSessionEvening)))
            {
                if (Entry_2)
                    VWAP_Open_Entry();
            }
		}

        protected void VWAP_Open_Entry()
        {
            VWAP vwap = VWAP();
            // EMA Exit
            EMA ema9 = EMA(9);
            EMA ema13 = EMA(13);
            EMA ema20 = EMA(20);
            EMA ema30 = EMA(30);
               
			/*
			// Grid color line to identify the values above vwap on the available timeframe
            if (
				   Close[0] > vwap[0] 
				&& (  (ToTime(Time[0]) >= MorningStart && ToTime(Time[0]) < MorningEnd && TradingSessionMorning) 
				    || (ToTime(Time[0]) >= AfternoonStart && ToTime(Time[0]) < AfternoonEnd && TradingSessionAfternoon) 
				    || (ToTime(Time[0]) >= EveningStart && ToTime(Time[0]) < EveningEnd && TradingSessionEvening) )
				)
                BackBrush = Brushes.Green;
            else if(
				   Close[0] < vwap[0] 
				&& (  (ToTime(Time[0]) >= MorningStart && ToTime(Time[0]) < MorningEnd && TradingSessionMorning) 
				    || (ToTime(Time[0]) >= AfternoonStart && ToTime(Time[0]) < AfternoonEnd && TradingSessionAfternoon) 
				    || (ToTime(Time[0]) >= EveningStart && ToTime(Time[0]) < EveningEnd && TradingSessionEvening) )
				)
                BackBrush = Brushes.Orange;
			*/
			
			if (CurrentBar < BarsRequiredToTrade)
				return;


            // Enter conditions  
            if (EMAAboveVWAP && (ema9[0] > (vwap[0] + EMAAboveVWAP_amt))) EnterShort(QuantityAmount);   // default EMAAboveVWAP_amt is 0.5$ , QuantityAmount is 10
            if (EMABelowVWAP && (ema9[0] < vwap[0]) && Close[0] > vwap[0])
            {
                // calculate the value of break
                breakPoint = (((Open[1] > Close[1]) && (Open[0] > Close[0]) && (Low[1] > Open[0])) || ((Open[1] < Close[1]) && (Open[0] < Close[0]) && (High[1] < Open[0])));
                if (breakPoint) EnterLong(QuantityAmount);
            }


            // Exit condition

            // common threshold
            sl = Math.Ceiling(StopLossValue * Close[0] * 20) / 20;
            pt = Math.Ceiling(ProfitTargetValue * Close[0] * 20) / 20;
            if (ProfitTarget) SetProfitTarget(CalculationMode.Price, pt);
            if (StopLoss) SetStopLoss(CalculationMode.Price, sl);

            // Ema Exit
            if (EMA_Exit) {
                // EMA Exit condition - origin
                if (flag13 == true || flag20 == true || flag30 == true) return;
                else if (Close[0] > ema9[0] && Close[1] > ema9[0] && Close[2] > ema9[0]){
                    flag9 = true;
                }

                if (flag9 == true || flag20 == true || flag30 == true) return;
                else if (Close[0] > ema13[0] && Close[1] > ema13[0] && Close[2] > ema13[0]){
                    flag13 = true;
                }

                if (flag13 == true || flag9 == true || flag30 == true) return;
                else if (Close[0] > ema20[0] && Close[1] > ema20[0] && Close[2] > ema20[0]){
                    flag20 = true;
                }
                
                if (flag13 == true || flag20 == true || flag9 == true) return;
                else if (Close[0] > ema30[0] && Close[1] > ema30[0] && Close[2] > ema30[0]){
                    flag30 = true;
                }

                if(flag9 == true && Close[0] < ema9[0])  { Print("EMA Exit"); ExitShort(); ExitLong(); flag9 = false; } 
                if(flag13 == true && Close[0] < ema13[0]) { Print("EMA Exit"); ExitShort(); ExitLong(); flag13 = false; } 
                if(flag20 == true && Close[0] < ema20[0]) { Print("EMA Exit"); ExitShort(); ExitLong(); flag20 = false; } 
                if(flag30 == true && Close[0] < ema30[0]) { Print("EMA Exit"); ExitShort(); ExitLong(); flag30 = false; } 
            }


            // +------------------------------------------------------------+
            // | Close position by 11:30 AM                    
            // +------------------------------------------------------------+
            if (ToTime(Time[0]) == CloseTime)
            {
                Print("Closing positions " + Time[0].ToString());

				if (Position.MarketPosition == MarketPosition.Long)
				ExitLong();
				if (Position.MarketPosition == MarketPosition.Short)
				ExitShort();
				
            }
        }

        #region Properties

        // +------------------------------------------------------------+
        // | Common parameters                                          |
        // +------------------------------------------------------------+

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Position Size", Description = "Number of contracts or shares", Order = 1, GroupName = "Parameters")]
        public int PositionSize
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Quanty Amount", Description = "Number of contracts or shares in each trade", Order = 2, GroupName = "Parameters")]
        public int QuantityAmount
        { get; set; }

        [Range(0, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Order Offset", Description = "Limit order offset", Order = 3, GroupName = "Parameters")]
        public double OrderOffset
        { get; set; }

        // +------------------------------------------------------------+
        // | Entry 2
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "EMA above VWAP (Short Position)", Description = "1D EMA9 above VWAP", Order = 1, GroupName = "Parameters - Entry 2")]
        public bool EMAAboveVWAP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA below VWAP (Long Position)", Description = "1D EMA9 below VWAP", Order = 2, GroupName = "Parameters - Entry 2")]
        public bool EMABelowVWAP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above amount", Description = "1D EMA9 above VWAP as 0.5$ default", Order = 3, GroupName = "Parameters - Entry 2")]
        public double EMAAboveVWAP_amt
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "StopLoss", Description = "StopLoss enable/disable", Order = 4, GroupName = "Parameters - Entry 2")]
        public bool StopLoss
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop Loss Percentage", Description = "common stoploss percent value - 0.125 % as default", Order = 5, GroupName = "Parameters - Entry 2")]
        public double StopLossValue
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ProfitTarget", Description = "Profit Target enable/disable", Order = 6, GroupName = "Parameters - Entry 2")]
        public bool ProfitTarget
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Profit Target Percentage", Description = "common profit target percent value - 0.375 % as default", Order = 7, GroupName = "Parameters - Entry 2")]
        public double ProfitTargetValue
        { get; set; }

        // +------------------------------------------------------------+
        // | Trading sessions                                           |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Trading (08:30 - 11:00)", Order = 1, GroupName = "Session")]
        public bool TradingSessionMorning
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trading (13:00 - 14:30)", Order = 2, GroupName = "Session")]
        public bool TradingSessionAfternoon
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trading (16:00 - 19:00)", Order = 3, GroupName = "Session")]
        public bool TradingSessionEvening
        { get; set; }

        // +------------------------------------------------------------+
        // | Entries                                                   
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "VWAP Magnet Entry", Order = 1, GroupName = "Entries")]
        public bool Entry_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Open Entry", Order = 2, GroupName = "Entries")]
        public bool Entry_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Cross Entry", Order = 3, GroupName = "Entries")]
        public bool Entry_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Bounce Entry", Order = 4, GroupName = "Entries")]
        public bool Entry_4
        { get; set; }

        // +------------------------------------------------------------+
        // | Indicators                                                 |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "VWAP Indicator", Order = 1, GroupName = "Indicators")]
        public bool showVWAP
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA Indicator", Order = 2, GroupName = "Indicators")]
        public bool showEMA
        { get; set; }

        // +------------------------------------------------------------+
        // | EMA Exit                                                   |
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "EMA Exit", Order = 1, GroupName = "EMA Exit")]
        public bool EMA_Exit
        { get; set; }

        // +------------------------------------------------------------+
        // | Above option                                                 
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Above VWAP", Order = 1, GroupName = "Above")]
        public bool AboveVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 2 min EMA 200", Order = 2, GroupName = "Above")]
        public bool Above_2min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 5 min EMA 200", Order = 3, GroupName = "Above")]
        public bool Above_5min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 15 min EMA 200", Order = 4, GroupName = "Above")]
        public bool Above_15min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 2 min EMA 100", Order = 5, GroupName = "Above")]
        public bool Above_2min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 5 min EMA 100", Order = 6, GroupName = "Above")]
        public bool Above_5min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 15 min EMA 100", Order = 7, GroupName = "Above")]
        public bool Above_15min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA 100 be above EMA 200", Order = 8, GroupName = "Above")]
        public bool Above_EMA100_EMA200
        { get; set; }
        #endregion
    }
}

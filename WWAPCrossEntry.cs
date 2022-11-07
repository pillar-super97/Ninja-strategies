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

    [Gui.CategoryOrder("Setup", 0)]
    [Gui.CategoryOrder("Entries", 1)]
    [Gui.CategoryOrder("Session", 2)]
    [Gui.CategoryOrder("Common Parameters", 3)]
    [Gui.CategoryOrder("Above", 4)]
    [Gui.CategoryOrder("ADX above", 5)]
    [Gui.CategoryOrder("Parameters for Cross Entry", 6)]

    public class VWAPCrossEntry : Strategy
    {
		
		// flag part
        private bool breakPoint1 = false;    // flag for identify break 1
		private bool breakPoint2 = false;
		
        private int EmaPeriod = -1;
		public int cnt=0;
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"VWAP Cross Entry";
                Name = "VWAPCrossEntry";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
				
                // Maximum number of entries allowed per direction
                EntriesPerDirection = 2;
                EntryHandling = EntryHandling.AllEntries;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                QuantityAmount = 2;

                // Entries
                Entry_3 = true;

                // Trading Time
                TimeStart_1 = new TimeSpan(8, 30, 0);
                TimeEnd_1 = new TimeSpan(11, 00, 0);
                TimeStart_2 = new TimeSpan(13, 00, 0);
                TimeEnd_2 = new TimeSpan(14, 30, 0);
                TimeStart_3 = new TimeSpan(16, 00, 0);
                TimeEnd_3 = new TimeSpan(19, 00, 0);
                CloseTime = new TimeSpan(11, 30, 0);

                // Trading session
                TradingSession_1 = true;
                TradingSession_2 = true;
                TradingSession_3 = true;

                // Indicators
                ShowVwap = true;
                ShowAtr = false;
				ShowEma = true;                
                AdxPeriod = 14;

                // Entry 3
				ATRsBelowVwap				= 75;		// 75% ATR below VWAP
				WithinXBars					= 3;
				MinATRsAboveVwap			= 150;		// 150%
				MaxATRsAboveVwap			= 300;		// 300%
				ToIdentifyMinATRsAboveVwap	= 0;
				ToIdentifyMaxATRsAboveVwap	= 100;      // 100%
				StopLossTick				= 1;
				ProfitTargetPercent1		= 150;		// 150%
				ProfitTargetPercent2		= 300;		// 300%
				ExitPositionPercentageAtFirst = 50;	// 50%
				LongPosOption1 				= true;
				LongPosOption2				= false;
				
                // ADX > var for underlying equity or index
                ADX_lim = 20;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Minute, 2); //add 2 minute data series for calculating the EMA
                AddDataSeries(Data.BarsPeriodType.Minute, 5); //add 5 minute data series for calculating the EMA
                AddDataSeries(Data.BarsPeriodType.Minute, 15); //add 15 minute data series for calculating the EMA

            }

            else if (State == State.DataLoaded)
            {
				// Show VWAP Indicator
                if (ShowVwap)
                {
                    VWAP vwap = VWAP();
                    vwap.NumDeviations = 0;

                    AddChartIndicator(vwap);
                    ChartIndicators[0].Plots[0].Brush = Brushes.LimeGreen;
                    ChartIndicators[0].Plots[0].DashStyleHelper = DashStyleHelper.Solid;
                    ChartIndicators[0].Plots[0].Width = 3;
                }

                // Show EMA Indicators
                if (ShowEma)
                {
                    AddChartIndicator(EMA(9));
                    ChartIndicators[1].Plots[0].Brush = Brushes.Red;

                    AddChartIndicator(EMA(13));
                    ChartIndicators[2].Plots[0].Brush = Brushes.DarkOrange;

                    AddChartIndicator(EMA(20));
                    ChartIndicators[3].Plots[0].Brush = Brushes.Yellow;

                    AddChartIndicator(EMA(30));
                    ChartIndicators[4].Plots[0].Brush = Brushes.Green;

                    AddChartIndicator(EMA(100));
                    ChartIndicators[5].Plots[0].Brush = Brushes.Blue;

                    AddChartIndicator(EMA(200));
                    ChartIndicators[6].Plots[0].Brush = Brushes.White;
                }

				// Show RSI Indicator
                if (ShowAtr)
                {
                    ATR atr = ATR(14);
                    AddChartIndicator(atr);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            Bars.TradingHours.TimeZone = "Central Standard Time";
            bool timing = false;
            if (TradingSession_1)
            {
                if (TimeStart_1 <= TimeEnd_1)
                {
                    if (Time[0].TimeOfDay >= TimeStart_1 && Time[0].TimeOfDay < TimeEnd_1) timing = true;
                }
                else
                {
                    if (Time[0].TimeOfDay >= TimeStart_1 || Time[0].TimeOfDay < TimeEnd_1) timing = true;
                }
            }
            if (TradingSession_2)
            {
                if (TimeStart_2 <= TimeEnd_2)
                {
                    if (Time[0].TimeOfDay >= TimeStart_2 && Time[0].TimeOfDay < TimeEnd_2) timing = true;
                }
                else
                {
                    if (Time[0].TimeOfDay >= TimeStart_2 || Time[0].TimeOfDay < TimeEnd_2) timing = true;
                }
            }
            if (TradingSession_3)
            {
                if (TimeStart_3 <= TimeEnd_3)
                {
                    if (Time[0].TimeOfDay >= TimeStart_3 && Time[0].TimeOfDay < TimeEnd_3) timing = true;
                }
                else
                {
                    if (Time[0].TimeOfDay >= TimeStart_3 || Time[0].TimeOfDay < TimeEnd_3) timing = true;
                }
            }


            if (timing)
            {
                if (Entry_3)
                    VWAP_Cross_Entry();
            }
        }

        protected bool Check_Above()
        {
            EMA ema100B0 = EMA(100);                     // 1 min bars obeject & 100 period // current main bar
            EMA ema100B1 = EMA(BarsArray[1], 100);       // 2 min bars obeject & 100 period
            EMA ema100B2 = EMA(BarsArray[2], 100);       // 5 min bars obeject & 100 period
            EMA ema100B3 = EMA(BarsArray[3], 100);       // 15 min bars obeject & 100 period

            EMA ema200B0 = EMA(200);                     // 1 min bars obeject & 200 period // current main bar
            EMA ema200B1 = EMA(BarsArray[1], 200);       // 2 min bars obeject & 200 period
            EMA ema200B2 = EMA(BarsArray[2], 200);       // 5 min bars obeject & 200 period
            EMA ema200B3 = EMA(BarsArray[3], 200);       // 15 min bars obeject & 200 period

            bool aboveEMA2_2 = true;
            bool aboveEMA2_5 = true;
            bool aboveEMA2_15 = true;
            bool aboveEMA1_2 = true;
            bool aboveEMA1_5 = true;
            bool aboveEMA1_15 = true;

            bool EMAsRelation = true;
            bool aboveVwap = true;
            VWAP vwap = VWAP();

            if (Entry_1 || Entry_2) aboveVwap = true;
            else if (Open[0] < vwap[0]) aboveVwap = false;
            
            aboveEMA2_2 = CrossAbove(Open, EMA(BarsArray[1], 200), 1) && Above_2min_EMA200;
            aboveEMA2_5 = CrossAbove(Open, EMA(BarsArray[2], 200), 1) && Above_5min_EMA200;
            aboveEMA2_15 = CrossAbove(Open, EMA(BarsArray[3], 200), 1) && Above_15min_EMA200;
            aboveEMA1_2 = CrossAbove(Open, EMA(BarsArray[1], 100), 1) && Above_2min_EMA100;
            aboveEMA1_5 = CrossAbove(Open, EMA(BarsArray[2], 100), 1) && Above_5min_EMA100;
            aboveEMA1_15 = CrossAbove(Open, EMA(BarsArray[3], 100), 1) && Above_15min_EMA100;

            EMAsRelation = Above_EMA100_EMA200 && (CrossAbove(ema100B1, ema200B1, 1) && Above_2min_EMA100 ||
                                            CrossAbove(ema100B2, ema200B2, 1) && Above_5min_EMA100 ||
                                            CrossAbove(ema100B3, ema200B3, 1) && Above_15min_EMA100);

            if (aboveVwap && aboveEMA2_2 && aboveEMA2_5 && aboveEMA2_15 &&
                aboveEMA1_2 && aboveEMA1_5 && aboveEMA1_15 && EMAsRelation) return true;
            else return false;
        }

        protected bool CheckEMA(bool position, bool option1_5, bool option5_15) // position = true : long, position = false : short
        {
            // For long entry, price > current timeframe EMA 200>5m EMA 200. There is an option for 5 minute EMA200>15m EMA 200. 
            // For short entry, price < current timeframe EMA 200<5m EMA 200. There is an option for 5 minute EMA200<15m EMA 200.
            EMA EMA_1 = EMA(BarsArray[0], 200);
            EMA EMA_5 = EMA(BarsArray[2], 200);
            EMA EMA_15 = EMA(BarsArray[3], 200);

            if (CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
                return true;

            // long entry
            if (position == true)
            {
                if (option1_5 == true)
                    if (Close[0] < EMA_1[0] | EMA_1[0] < EMA_5[0]) return false;
                if (option5_15 == true && EMA_5[0] < EMA_15[0]) return false;
            }

            // short entry
            if (position == false)
            {
                if (option1_5 == true)
                    if (Close[0] > EMA_1[0] || EMA_1[0] > EMA_5[0]) return false;
                if (option5_15 == true && EMA_5[0] > EMA_15[0]) return false;
            }
            return true;
        }

        protected bool CheckADX()
        {

            if (CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
                return true;

            ADX ADX_1 = ADX(14);
            ADX ADX_2 = ADX(BarsArray[1], AdxPeriod);
            ADX ADX_5 = ADX(BarsArray[2], AdxPeriod);
            ADX ADX_15 = ADX(BarsArray[3], AdxPeriod);

            if (ADX_cur & ADX_1[0] <= ADX_lim) return false;
            if (ADX_2min & ADX_2[0] <= ADX_lim) return false;
            if (ADX_5min & ADX_5[0] <= ADX_lim) return false;
            if (ADX_15min & ADX_15[0] <= ADX_lim) return false;

            return true;
        }


        protected void VWAP_Cross_Entry()
        {
            VWAP vwap = VWAP();
            ATR atr = ATR(14);
            ADX adx = ADX(AdxPeriod);
			bool arrived = false;  // When price to reach between **150% ATR to **300% ATR above VWAP. A bar may or may not close in this range, as long as price reaches this range. 
			double StopLoss;
			double ProfitTarget1;
            double AtrsBelowVWAP;
          		
			// +------------------------------------------------------------+
		    // | Enter Condition (Only Long position - because this strategy doesn't contain Short Pos.)
		    // +------------------------------------------------------------+
			
			if (CurrentBar < BarsRequiredToTrade) return;

			if (Position.MarketPosition == MarketPosition.Flat) {
                // calculate the value of break 
                breakPoint1 = ((Math.Max(Open[0], Close[0]) > Math.Max(Open[1], Close[1])) && (Math.Min(Open[0], Close[0]) > Math.Min(Open[1], Close[1])) || 
                               (Math.Max(Open[0], Close[0]) < Math.Max(Open[1], Close[1])) && (Math.Min(Open[0], Close[0]) < Math.Min(Open[1], Close[1])) );
                breakPoint2 = ((Math.Max(Open[2], Close[0]) > Math.Max(Open[1], Close[1])) && (Math.Min(Open[2], Close[2]) > Math.Min(Open[1], Close[1])) || 
                               (Math.Max(Open[2], Close[0]) < Math.Max(Open[1], Close[1])) && (Math.Min(Open[2], Close[2]) < Math.Min(Open[1], Close[1])) );
                
    
                // 1. At each new bar, look for price >X% (default:75%) ATR below VWAP
                    AtrsBelowVWAP = vwap[0] - ATRsBelowVwap / 100 * atr[0];
                if (High[0] > AtrsBelowVWAP && Low[0] < AtrsBelowVWAP) {
                    cnt = cnt + 1;	
                }
                
                if (cnt != 0 && cnt <= WithinXBars) {
                    // 4. Look for price to reach between **150% ATR to **300% ATR above VWAP. 
                    // A bar may or may not close in this range, as long as price reaches this range. 
                    if (High[0] > MinATRsAboveVwap / 100 * atr[0] + vwap[0] && High[0] < MaxATRsAboveVwap / 100 * atr[0] + vwap[0]) {
                        arrived = true;
                    }
                    
                    if (arrived) {
                        // Check EMA relationship & above option
                        if (CheckEMA(true, EMAcomp1minV5min, EMAcomp5minV15min)  & CheckADX() & Check_Above())
                        {
                            // 6. Identify each new bar that closes between **0% ATR to **100% ATR above VWAP. 
                            if (Close[0] > ToIdentifyMinATRsAboveVwap / 100 * atr[0] + vwap[0] && Close[0] < ToIdentifyMaxATRsAboveVwap / 100 * atr[0] + vwap[0]) {
                                // Open a long position when there is a first break above.
                                if (LongPosOption1 && !LongPosOption2) {
                                        if (breakPoint1) {
                                            Print ("Buy");									
                                            EnterLongLimit((int)(QuantityAmount * ExitPositionPercentageAtFirst / 100), GetCurrentAsk() + TickSize * OrderOffset, "Long_1");
                                            EnterLongLimit((int)(QuantityAmount - QuantityAmount * (1 - ExitPositionPercentageAtFirst / 100)), GetCurrentAsk() + TickSize * OrderOffset, "Long_2");
                                            arrived = false;
											cnt = 0;
                                        }
                                }
                                
                                // Open a long position when there is a two consecutive breaks above.
                                if (LongPosOption2 && !LongPosOption1) {
                                        if (breakPoint1 && breakPoint2) {
                                            Print ("Buy");									
                                            EnterLongLimit((int)(QuantityAmount * ExitPositionPercentageAtFirst / 100), GetCurrentAsk() + TickSize * OrderOffset, "Long_1");
                                            EnterLongLimit((int)(QuantityAmount - QuantityAmount * (1 - ExitPositionPercentageAtFirst / 100)), GetCurrentAsk() + TickSize * OrderOffset, "Long_2");       
                                            arrived = false;
											cnt = 0;
                                        }
                                }
                            }
                        }
                    }
                }
                else { cnt = 0; }
            }
			
			// +------------------------------------------------------------+
		    // | Exit Condition (Only Long position - because this strategy doesn't contain Short Pos.)
		    // +------------------------------------------------------------+
			if (Position.MarketPosition == MarketPosition.Long) {
                Print ("Buy closed");
                StopLoss = vwap[0] - TickSize * StopLossTick;
                ProfitTarget1 = Close[0] + ProfitTargetPercent1 / 100 * (Close[0] - StopLoss);
                
                SetStopLoss("Long_1", CalculationMode.Price, StopLoss, false);
                SetProfitTarget("Long_1", CalculationMode.Price, ProfitTarget1 );
                
                if (Close[0] >= ProfitTarget1) {
                    SetStopLoss("Long_2", CalculationMode.Price, Close[0], false);
                    SetProfitTarget("Long_2", CalculationMode.Price, Open[0] + atr[0] * ProfitTargetPercent2 / 100 );
                }
            }

            // +------------------------------------------------------------+
            // | EMA Exit Option              
            // +------------------------------------------------------------+
            if (EmaExit)
            {
                double ema_9 = EMA(9)[0];
                double ema_13 = EMA(13)[0];
                double ema_20 = EMA(20)[0];
                double ema_30 = EMA(30)[0];

                if (EmaPeriod == -1)
                {
                    int period = -1;

                    if (Math.Min(Close[0], Math.Min(Close[1], Close[2])) > ema_30) period = 30;
                    if (Math.Min(Close[0], Math.Min(Close[1], Close[2])) > ema_20) period = 20;
                    if (Math.Min(Close[0], Math.Min(Close[1], Close[2])) > ema_13) period = 13;
                    if (Math.Min(Close[0], Math.Min(Close[1], Close[2])) > ema_9) period = 9;

                    EmaPeriod = period;
                }
                else
                {
                    if (Close[0] < EMA(EmaPeriod)[0])
                    {
                        Print("EMA Exit");
                        ExitLong();
                        EmaPeriod = -1;
                    }
                }
            }

		    // +------------------------------------------------------------+
		    // | Close position by 11:30 AM                    
		    // +------------------------------------------------------------+
		    if (Time[0].TimeOfDay == CloseTime)
		    {
				Print("Closing positions " + Time[0].ToString());
				ExitLong();
				ExitShort();
                EmaPeriod = -1;

		    }
		}

        #region Properties        

        // +------------------------------------------------------------+
        // | Entries  (order: 1)                                                 
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "VWAP Magnet Entry", Order = 1, GroupName = "Entries", Description = "Enable magnet entry")]
        public bool Entry_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Open Entry", Order = 2, GroupName = "Entries", Description = "Enable open entry")]
        public bool Entry_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Cross Entry", Order = 3, GroupName = "Entries", Description = "Enable cross entry")]
        public bool Entry_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Bounce Entry", Order = 4, GroupName = "Entries", Description = "Enable bounce entry")]
        public bool Entry_4
        { get; set; }

        // +------------------------------------------------------------+
        // | Trading sessions (order: 2)                                          
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 1 start", Description = "Start time of first session", Order = 1, GroupName = "Session")]
        public TimeSpan TimeStart_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 1 end", Description = "End time of first session", Order = 2, GroupName = "Session")]
        public TimeSpan TimeEnd_1
        { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Use time 1", Order = 3, GroupName = "Session")]
        public bool TradingSession_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 2 start", Description = "Start time of second session", Order = 4, GroupName = "Session")]
        public TimeSpan TimeStart_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 2 end", Description = "End time of second session", Order = 5, GroupName = "Session")]
        public TimeSpan TimeEnd_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use time 2", Order = 6, GroupName = "Session")]
        public bool TradingSession_2
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 3 start", Description = "Start time of third session", Order = 7, GroupName = "Session")]
        public TimeSpan TimeStart_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Time 3 end", Description = "End time of third session", Order = 8, GroupName = "Session")]
        public TimeSpan TimeEnd_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use time 3", Order = 9, GroupName = "Session")]
        public bool TradingSession_3
        { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Close time", Description = "Close the position if price has not reached stop loss or profit target", Order = 10, GroupName = "Session")]
        public TimeSpan CloseTime
        { get; set; }

        // +------------------------------------------------------------+
        // | Common Parameters (order: 3)                                      
        // +------------------------------------------------------------+
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Quanty Amount", Description = "Order quantity when enter long or short position", Order = 1, GroupName = "Common Parameters")]
        public int QuantityAmount
        { get; set; }

        [Range(0, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Order Offset", Description = "Limit order offset when excute an order to ahead of others and filled earlier\nUnit: unit of shade price(ex: $)", Order = 2, GroupName = "Common Parameters")]
        public int OrderOffset
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP Indicator", Order = 3, GroupName = "Common Parameters", Description = "Show VWAP indicator on chart")]
        public bool ShowVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Indicator", Description = "The average true range (ATR) is a market volatility indicator used in technical analysis. It is typically derived from the 14-day simple moving average of a series of true range indicators. The ATR was originally developed for use in commodities markets but has since been applied to all types of securities.", Order = 4, GroupName = "Common Parameters")]
        public bool ShowAtr
        { get; set; }
		
        [NinjaScriptProperty]
        [Display(Name = "EMA Indicator", Order = 5, GroupName = "Common Parameters", Description = "Show EMA indicator on chart")]
        public bool ShowEma
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "5 min EMA200 > 15m EMA 200", Order = 6, GroupName = "Common Parameters", Description = "Long entry: 5m EMA200 > 15m EMA 20 \nShort entry: 5m EMA200 < 15m EMA 200.")]
        public bool EMAcomp5minV15min
        { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "1 min EMA 200 > 5 min EMA 200", Order = 7, GroupName = "Common Parameters", Description = "Long entry: 1 min EMA 200 > 5 min EMA 200 \nShort entry: 1 min EMA 200 < 5 min EMA 200.")]
        public bool EMAcomp1minV5min
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "ADX Period", Description = "Number of bars used in the ADX calculation", Order = 8, GroupName = "Common Parameters")]
        public int AdxPeriod
        { get; set; }

        // +------------------------------------------------------------+
        // | Above option (order: 4)                                                
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Above VWAP", Order = 1, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above VWAP")]
        public bool AboveVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 2 min EMA 200", Order = 2, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 2 min EMA 200")]
        public bool Above_2min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 5 min EMA 200", Order = 3, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 5 min EMA 200")]
        public bool Above_5min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 15 min EMA 200", Order = 4, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 15 min EMA 200")]
        public bool Above_15min_EMA200
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 2 min EMA 100", Order = 5, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 2 min EMA 100")]
        public bool Above_2min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 5 min EMA 100", Order = 6, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 5 min EMA 100")]
        public bool Above_5min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Above 15 min EMA 100", Order = 7, GroupName = "Above", Description = "Price at the time of entry (Long version) must be above 15 min EMA 100")]
        public bool Above_15min_EMA100
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA 100 be above EMA 200", Order = 8, GroupName = "Above", Description = "All selected 2 minute and 5 minute and 15 minute EMA 100 must be above that same timeframe EMA 200.")]
        public bool Above_EMA100_EMA200
        { get; set; }

        // +------------------------------------------------------------+
        // | ADX > 20 (var) for underlying equity or index that is being traded (order: 5)
        // +------------------------------------------------------------+
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "ADX Limit", Description = "At the time of entry, low value of ADX required for each of four timeframes for the underlying equity or index that is being traded", Order = 0, GroupName = "ADX above")]
        public int ADX_lim
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Current Timeframe", Order = 1, GroupName = "ADX above", Description = "At the time of entry for current timeframe requiring ADX > var for the underlying equity or index that is being traded")]
        public bool ADX_cur
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "2 min Timeframe", Order = 2, GroupName = "ADX above", Description = "At the time of entry for 2 minute timeframe requiring ADX > var for the underlying equity or index that is being traded")]
        public bool ADX_2min
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "5 min Timeframe", Order = 3, GroupName = "ADX above", Description = "At the time of entry for 5 minute timeframe requiring ADX > var for the underlying equity or index that is being traded")]
        public bool ADX_5min
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "15 min Timeframe", Order = 4, GroupName = "ADX above", Description = "At the time of entry for 15 minute timeframe requiring ADX > var for the underlying equity or index that is being traded")]
        public bool ADX_15min
        { get; set; }

        // +------------------------------------------------------------+
        // | Entry 3 (order: 6)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Find bar X% ATR below VWAP", Description = "At each new bar, look for price >X% (default:75%) ATR below VWAP", Order = 1, GroupName = "Parameters for Cross Entry")]
        public double ATRsBelowVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Find break within X more bars", Description = "Look for price within X (default:3) more bars to break 1 tick or 1 cent above VWAP. ", Order = 2, GroupName = "Parameters for Cross Entry")]
        public int WithinXBars
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min ATRs above VWAP", Description = "Look for price to reach between **min(def:150%) ATR to **max(def:300%) ATR above VWAP. ", Order = 3, GroupName = "Parameters for Cross Entry")]
        public double MinATRsAboveVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max ATRs above VWAP", Description = "Look for price to reach between **min(def:150%) ATR to **max(def:300%) ATR above VWAP. ", Order = 4, GroupName = "Parameters for Cross Entry")]
        public double MaxATRsAboveVwap
        { get; set; }

		[NinjaScriptProperty]
        [Display(Name = "To identify Min ATRs above VWAP", Description = "Identify each new bar that closes between **ToIdentifyMin(def:0%) ATR to **ToIdentifyMin(100%) ATR above VWAP. ", Order = 5, GroupName = "Parameters for Cross Entry")]
        public double ToIdentifyMinATRsAboveVwap
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "To identify Max ATRs above VWAP", Description = "Identify each new bar that closes between **ToIdentifyMin(def:0%) ATR to **ToIdentifyMin(100%) ATR above VWAP. ", Order = 6, GroupName = "Parameters for Cross Entry")]
        public double ToIdentifyMaxATRsAboveVwap
        { get; set; }
			
        [NinjaScriptProperty]
        [Display(Name = "Stop Loss Tick", Description = "Stop loss is 1 tick below VWAP.", Order = 7, GroupName = "Parameters for Cross Entry")]
        public int StopLossTick
        { get; set; }
		
        [NinjaScriptProperty]
        [Display(Name = "PT 1 %", Description = "Be careful. This value is not just percentage of Profit Target. \n Calculate PT1 (profit target 1) to be **150% of the distance from the entry to the stop loss.", Order = 8, GroupName = "Parameters for Cross Entry")]
        public double ProfitTargetPercent1
        { get; set; }

		[NinjaScriptProperty]
        [Display(Name = "PT 2 %", Description = "Be careful. This value is not just percentage of Profit Target. \n **X% of ATR from the entry (default 300%)", Order = 9, GroupName = "Parameters for Cross Entry")]
        public double ProfitTargetPercent2
        { get; set; }		
		
		[NinjaScriptProperty]
        [Display(Name = "Exit Position(%) at First PT", Description = "When price reaches PT 1, exit **X% of position (default 50% of position) at that PT1 (default:150%).", Order = 10, GroupName = "Parameters for Cross Entry")]
        public double ExitPositionPercentageAtFirst
        { get; set; }	
		
		[NinjaScriptProperty]
        [Display(Name = "Long Position Option 1", Description = "Open a long position when there is a first break above.", Order = 11, GroupName = "Parameters for Cross Entry")]
        public bool LongPosOption1
        { get; set; }
        
		[NinjaScriptProperty]
        [Display(Name = "Long Position Option 2", Description = "Open a long position when there is a second break above.", Order = 12, GroupName = "Parameters for Cross Entry")]
        public bool LongPosOption2
        { get; set; }
		
        // +------------------------------------------------------------+
        // | EMA Exit (order: 7)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Exit on EMA", Description = "Option for exit 100% of the position when meet EMA exit condition", Order = 1, GroupName = "EMA Exit")]
        public bool EmaExit
        { get; set; }
        #endregion
    }
}

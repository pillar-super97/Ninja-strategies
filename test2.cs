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
    [Gui.CategoryOrder("Parameters for Magnet Entry", 6)]

    public class test2 : Strategy
    {
        private int EmaPeriod = -1;
        private double sl;
        private double pt1;
        private double sl1;
        private double sl2;

        // flag part
        public bool sideway = false;
        public bool immedprebarShort = false; // immediately previous bar on the border of sideway.
        public bool immedprebarLong = false; // immediately previous bar on the border of sideway.
        public bool overwayShort = false;
        public bool overwayLong = false;
        public bool firstbreakShort = false;
        public bool firstbreakLong = false;

        public int IPBar;
        public int CBar;


        public VWAP vwap;
        public ATR atr;
        public VWAPPTR uplimit;
        public VWAPPTR downlimit;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"VWAP Magnet Entry";
                Name = "test2";
                Calculate = Calculate.OnPriceChange;
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

                EntriesPerDirection = 2;
                EntryHandling = EntryHandling.AllEntries;

                IsInstantiatedOnEachOptimizationIteration = true;
                QuantityAmount = 2;

                // Entries
                Entry_1 = true;

                // Trading Time
                TimeStart_1 = new TimeSpan(08, 30, 00);
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
                ShowRsi = false;
                ShowEma = false;
                AdxPeriod = 14;
                RsiPeriod = 14;
                RsiSmooth = 3;

                // Entry 1
                SidewayLimitATRs = 110; // 110%
                RsiMinForSideway = 40;
                RsiMaxForSideway = 60;
                AdxMaxForSideway = 18;
                StopLossPercent = 100; // 50%
                ExitProfitTarget_1 = true;
                ExitProfitTarget_2 = false;
                ProfitTargetPercent = 100; // 50%
                EmaExit = false;

                // ADX > var for underlying equity or index
                ADX_lim = 20;


            }

            else if (State == State.Configure)
            {
                if (ExitProfitTarget_2 == true)
                    ExitProfitTarget_1 = false;

                AddDataSeries(Data.BarsPeriodType.Minute, 2); //add 2 minute data series for calculating the EMA
                AddDataSeries(Data.BarsPeriodType.Minute, 5); //add 5 minute data series for calculating the EMA
                AddDataSeries(Data.BarsPeriodType.Minute, 15); //add 15 minute data series for calculating the EMA

            }

            else if (State == State.DataLoaded)
            {
                vwap = VWAP();
                atr = ATR(14);
                uplimit = VWAPPTR(14, SidewayLimitATRs);
                downlimit = VWAPPTR(14, -SidewayLimitATRs);

                vwap.NumDeviations = 0;

                // Show VWAP Indicator
                if (ShowVwap)
                {
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

                AddChartIndicator(uplimit);
                AddChartIndicator(downlimit);


                // Show RSI Indicator
                if (ShowAtr)
                {
                    AddChartIndicator(atr);
                }

                // Show RSI Indicator
                if (ShowRsi)
                {
                    RSI rsi = RSI(RsiPeriod, RsiSmooth);
                    AddChartIndicator(rsi);
                }
            }
        }


        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade) return;
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
                if (Entry_1)
                    VWAP_Magnet_Entry();
            }
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


        protected void VWAP_Magnet_Entry()
        {
            if (Time[0].Day == 1 &&
                Time[0].TimeOfDay == new TimeSpan(14, 19, 00)
                //||Time[0].TimeOfDay == new TimeSpan(18, 05, 00) 
                //|| Time[0].TimeOfDay == new TimeSpan(21, 06, 00)
                )
            {
                int a = 1;
            }
            //RSI rsi = RSI(RsiPeriod, RsiSmooth);
            //ADX adx = ADX(AdxPeriod);
            double currentPrice = Close[0];

            // +------------------------------------------------------------+
            // |  Enter Condition             
            // +------------------------------------------------------------+
            if (Position.MarketPosition == MarketPosition.Flat)
            {

                if (immedprebarShort & Low[1] > uplimit[0] & currentPrice < Low[1] & IPBar != CBar)
                {
                    firstbreakShort = true;
                    CBar = CurrentBar;
                }

                if (immedprebarLong & High[1] < downlimit[0] & currentPrice > High[1] & IPBar != CBar)
                {
                    firstbreakLong = true;
                    CBar = CurrentBar;
                }

                if (sideway == true & Low[0] < uplimit[0] & uplimit[0] < High[0])
                {
                    immedprebarShort = true;
                    IPBar = CurrentBar;

                }

                if (sideway == true & Low[0] < downlimit[0] & downlimit[0] < High[0])
                {
                    immedprebarLong = true;
                    IPBar = CurrentBar;
                }

                // Confirm VWAP is mostly sideways with VWAP RSI, ADX   
                if (Close[0] <= uplimit[0] && Close[0] >= downlimit[0]
                    //&& rsi[0] > RsiMinForSideway &&
                    // rsi[0] < RsiMaxForSideway &&
                    // adx[0] < AdxMaxForSideway
                    )
                    sideway = true;
                else sideway = false;
                /////ok
                //return;


                // short
                if (firstbreakShort == true)
                {

                    // Check EMA relationship
                    if (true)
                    {
                        sideway = false;
                        overwayShort = false;
                        immedprebarShort = false;
                        firstbreakShort = false;
                        overwayLong = false;
                        immedprebarLong = false;
                        firstbreakLong = false;
                        EnterShortLimit(QuantityAmount / 2, GetCurrentBid() - OrderOffset, "Short_1");
                        EnterShortLimit(QuantityAmount - QuantityAmount / 2, GetCurrentBid() - OrderOffset, "Short_2");


                        // Option for exit 100% of position using PT 1
                        if (!ExitProfitTarget_2)
                        {
                            //SetStopLoss(CalculationMode.Price, Close[0] + StopLossPercent / 100 * atr[0]);
                            sl = Close[0] + StopLossPercent / 100 * atr[0];
                            if (ExitProfitTarget_1)
                            {
                                pt1 = vwap[0] - atr[0] * ProfitTargetPercent / 100;
                                //SetProfitTarget(CalculationMode.Price, pt1);
                            }
                            else { pt1 = 10000000; }
                        }

                        // Option for using PT 2 
                        if (!ExitProfitTarget_1 & ExitProfitTarget_2)
                        {
                            sl1 = Close[0] + atr[0] * StopLossPercent / 100;
                            sl2 = Close[0];
                        }

                    }
                }

                // long
                if (firstbreakLong == true)
                {

                    // Check EMA relationship
                    if (true)
                    {
                        sideway = false;
                        overwayShort = false;
                        immedprebarShort = false;
                        firstbreakShort = false;
                        overwayLong = false;
                        immedprebarLong = false;
                        firstbreakLong = false;
                        EnterLongLimit(QuantityAmount / 2, GetCurrentAsk() + OrderOffset, "Long_1");
                        EnterLongLimit(QuantityAmount - QuantityAmount / 2, GetCurrentAsk() + OrderOffset, "Long_2");


                        // Option for exit 100% of position using PT 1
                        if (!ExitProfitTarget_2)
                        {
                            //SetStopLoss(CalculationMode.Price, Close[0] - atr[0] * StopLossPercent / 100);
                            sl = Close[0] - atr[0] * StopLossPercent / 100;
                            if (ExitProfitTarget_1)
                            {
                                pt1 = vwap[0] + atr[0] * ProfitTargetPercent / 100;
                                //SetProfitTarget(CalculationMode.Price, pt1);
                            }
                            else { pt1 = 1000000; }
                        }

                        // Option for using PT 2 
                        if (!ExitProfitTarget_1 & ExitProfitTarget_2)
                        {
                            sl1 = Close[0] - atr[0] * StopLossPercent / 100;
                            sl2 = Close[0];
                        }
                    }

                }

            }
            else
            {
                sideway = false;
                overwayShort = false;
                immedprebarShort = false;
                firstbreakShort = false;
                overwayLong = false;
                immedprebarLong = false;
                firstbreakLong = false;
            }

            //return;
            // +------------------------------------------------------------+
            // |  Exit Condition
            // +------------------------------------------------------------+

            // Option for exit 100% of position using PT 1
            if (!ExitProfitTarget_2)
            {
                // short
                if (Position.MarketPosition == MarketPosition.Short)
                {
                    if (currentPrice > sl)
                    {
                        Print("StopLoss");
                        ExitShort();
                    }
                    if (currentPrice < pt1)
                    {
                        Print("ProfitTarget");
                        ExitShort();
                    }
                }

                // long
                if (Position.MarketPosition == MarketPosition.Long)
                {
                    if (currentPrice < sl)
                    {
                        Print("StopLoss");
                        ExitLong();
                    }
                    if (currentPrice > pt1)
                    {
                        Print("ProfitTarget");
                        ExitLong();
                    }
                }
            }

            // Option for using PT 2 to exit positions seperately
            if (!ExitProfitTarget_1 & ExitProfitTarget_2)
            {
                // short
                if (Position.MarketPosition == MarketPosition.Short)
                {


                    if (Position.Quantity < QuantityAmount)
                    {
                        if (currentPrice >= sl2)
                        {
                            Print("Stop Loss 2 = breakeven");
                            ExitShort();
                        }
                        if (currentPrice > High[1])
                        {
                            Print("Profit target 2");
                            ExitShort();
                        }
                    }

                    else
                    {
                        if (currentPrice <= pt1)
                        {
                            Print("Profit target 1");
                            ExitShort("Short_1");
                        }
                        if (currentPrice >= sl1)
                        {
                            Print("Stop Loss 1 = all closed");
                            ExitShort();
                        }
                    }
                }

                //long
                if (Position.MarketPosition == MarketPosition.Long)
                {
                    if (Position.Quantity < QuantityAmount)
                    {
                        // SetStopLoss(CalculationMode.Price, sl2);
                        if (currentPrice <= sl2)
                        {
                            Print("Stop Loss 2 = breakeven");
                            ExitLong();
                        }
                        if (currentPrice < Low[1])
                        {
                            Print("Profit target 2");
                            ExitLong();
                        }
                    }
                    else
                    {
                        if (currentPrice >= pt1)
                        {
                            Print("Profit target 1");
                            ExitLong("Long_1");
                        }
                        if (currentPrice <= sl1)
                        {
                            Print("Stop Loss 1 = all closed");
                            ExitLong();
                        }
                    }
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
        [Display(ResourceType = typeof(Custom.Resource), Name = "Close time", Description = "Close the all the positions if price has not reached stop loss or profit target", Order = 10, GroupName = "Session")]
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
        [Display(Name = "5 min EMA200 > 15 min EMA 200", Order = 6, GroupName = "Common Parameters", Description = "Long entry: 5 min EMA200 > 15 min EMA 20 \nShort entry: 5 min EMA200 < 15 min EMA 200.")]
        public bool EMAcomp5minV15min
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "1 min EMA 200 > 5 min EMA 200", Order = 7, GroupName = "Common Parameters", Description = "Long entry: 1 min EMA 200 > 5 min EMA 200 \nShort entry: 1 min EMA 200 < 5 min EMA 200.")]
        public bool EMAcomp1minV5min
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
        // | Entry 1 (order: 6)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Sideway Limit by ATR from VWAP", Description = "•	When SPY reaches X% ATR (default 110%)  above or below VWAP then confirm VWAP is mostly sideways with RSI and ADX", Order = 1, GroupName = "Parameters for Magnet Entry")]
        public double SidewayLimitATRs
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Indicator", Order = 2, GroupName = "Parameters for Magnet Entry", Description = "Show RSI indicator on chart for Magnet Entry")]
        public bool ShowRsi
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "RSI Period", Description = "Number of bars used in the RSI calculation", Order = 3, GroupName = "Parameters for Magnet Entry")]
        public int RsiPeriod
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "RSI Smooth", Description = "Smoothing period used in the RSI calculation", Order = 4, GroupName = "Parameters for Magnet Entry")]
        public int RsiSmooth
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Min for Sideway", Description = "Minimum number of RSI to confirm VWAP is mostly sideways", Order = 5, GroupName = "Parameters for Magnet Entry")]
        public double RsiMinForSideway
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Max for Sideway", Description = "Maximum number of RSI to confirm VWAP is mostly sideways", Order = 6, GroupName = "Parameters for Magnet Entry")]
        public double RsiMaxForSideway
        { get; set; }

        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "ADX Period", Description = "Number of bars used in the ADX calculation", Order = 7, GroupName = "Parameters for Magnet Entry")]
        public int AdxPeriod
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "VWAP ADX Max for Sideway", Description = "Maximum number of ADX to confirm VWAP is mostly sideways", Order = 8, GroupName = "Parameters for Magnet Entry")]
        public double AdxMaxForSideway
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop Loss %", Description = "Percentage of ATR away from the entry price for stop loss", Order = 9, GroupName = "Parameters for Magnet Entry")]
        public double StopLossPercent
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PT 1 %", Description = "Percentage of ATR away from the average entry price from VWAP for profit target", Order = 10, GroupName = "Parameters for Magnet Entry")]
        public double ProfitTargetPercent
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Exit on Profit Target 1", Description = "Option for exit 100% of the position using PT 1, where PT 1 is at ProfitTargetPercent of current price from VWAP \nDeselect the PT2 option below to use this option", Order = 11, GroupName = "Parameters for Magnet Entry")]
        public bool ExitProfitTarget_1
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Exit on Profit Target 2", Description = "Option for close half of the position at PT 1, the other at PT 2 \nDeselect the PT1 option above to use this option", Order = 12, GroupName = "Parameters for Magnet Entry")]
        public bool ExitProfitTarget_2
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

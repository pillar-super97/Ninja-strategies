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
    public class FXMATHEUSstrategyv1 : Strategy
    {
        private string longalert;
        private string shortalert;
        private string closelong;
        private string closeshort;
        private double sps;

        EMA ema, ema2;

        bool vold1, vold2, vold3;
        bool volu1, volu2, volu3;

        double ret = 0;
        double pos = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "FXMATHEUSstrategyv1";
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
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;

                // +------------------------------------------------------------+
                // | parameters for MT4/5 Settings
                // +------------------------------------------------------------+
                pc_risk = 2;        // default value : 2 , step : 0.1 , min value : 0
                pc_id = "";         // defval : "" , 
                pc_prefix = "";     // defval : ""

                // +------------------------------------------------------------+
                // | EMA
                // +------------------------------------------------------------+
                ema_src = close;     // defval : close , inline = "1"
                ema_len = 200 ;      // defval: 200 , inline = "1"

                ema2_src = close;    // defval : close , inline = "2"
                ema2_len = 800;      // defval: 200 , inline = "2"

                // +------------------------------------------------------------+
                // | EMA
                // +------------------------------------------------------------+
                Length = 200;        // defval : 200 , minval = "1"
                Multiplier = 3;      // defval: 3 , minval = "0.000001"

                // +------------------------------------------------------------+
                // | Volume Based Coloured Bars
                // +------------------------------------------------------------+
                smaLength = 21;


                string symbol = pc_prefix;
                float usef = pc_risk;

                longalert = pc_id + ",buy," + symbol + ",risk=" + usef.ToString("N2") + "";
                shortalert = pc_id + ",sell," + symbol + ",risk=" + usef.ToString("N2") + "";
                closelong = pc_id + ",closelong," + symbol + "";
                closeshort = pc_id + ",closeshort," + symbol + "";

            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                if (showEma1) {
                    ema = EMA(ema_len);
                    AddChartIndicator(ema);
                    ChartIndicators[0].Plots[0].Brush = Brushes.Purple;
                    ChartIndicators[0].Plots[0].Width = 3;
                }

                if (showEma2) {
                    ema2 = EMA(ema2_len);
                    AddChartIndicator(ema2);
                    ChartIndicators[1].Plots[0].Brush = Brushes.Orange;
                    ChartIndicators[1].Plots[0].Width = 3;
                }
            }
        }
        protected override void OnBarUpdate()
        {
            //Add your custom strategy logic here.
            sps = Position.Quantity;
            VolumeBasedColoredBars();
            Strategy_1();
            Strategy_2();
            Strategy_3();
            Strategy_4();
            Strategy_5();
        }
 
        protected void TrendTraderStrategy()
        {
            int Length = 21;
            float Multiplier = 3;
            WMA avgTR = WMA(ATR(1), Length);
            double hiLimit = High[1] - avgTR[1] * Multiplier;
            double loLimit = Low[1] + avgTR[1] * Multiplier;
            ret = Close[0] > hiLimit & Close[0] > loLimit ? hiLimit : Close[0] < loLimit & Close[0] < hiLimit ? loLimit : 0;
            pos = Close[0] > ret ? 1 : Close[0] < ret ? -1 : 0;

            //if (pos != pos[1] & pos == 1)
            //    alert("Color changed - Buy", alert.freq_once_per_bar_close)
            //if pos != pos[1] & pos == -1
            //    alert("Color changed - Sell", alert.freq_once_per_bar_close)
            //// barcolor(pos == -1 ? color.red : pos == 1 ? color.green : color.blue)
            //plot(ret, color = color.new(color.blue, 0), title = 'Trend Trader Strategy')

        }

        protected void VolumeBasedColoredBars()
        {
            double avrg = SMA(Volume, smaLength)[0];

            vold1 = Volume[0] > avrg * 1.5 & Close[0] < Open[0];
            vold2 = Volume[0] >= avrg * 0.5 & Volume[0] <= avrg * 1.5 & Close[0] < Open[0];
            vold3 = Volume[0] < avrg * 0.5 & Close[0] < Open[0];

            volu1 = Volume[0] > avrg * 1.5 & Close[0] > Open[0];
            volu2 = Volume[0] >= avrg * 0.5 & Volume[0] <= avrg * 1.5 & Close[0] > Open[0];
            volu3 = Volume[0] < avrg * 0.5 & Close[0] > Open[0];

            Brush cold1 = new SolidColorBrush(Color.FromRgb(127, 0, 0));
            Brush cold2 = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            Brush cold3 = Brushes.Orange;
            Brush colu1 = new SolidColorBrush(Color.FromRgb(0, 100, 0));
            Brush colu2 = Brushes.Lime;
            Brush colu3 = new SolidColorBrush(Color.FromRgb(127, 255, 200));
            Brush color_1 = vold1 ? cold1 : vold2 ? cold2 : vold3 ? cold3 : volu1 ? colu1 : volu2 ? colu2 : volu3 ? colu3 : Brushes.White;

            BarBrush = color_1;

        }

        protected void Strategy_1()
        {
            double sps = Position.Quantity;
            bool buy1 = volu1 & CrossAbove(Close, ret, 1) & Close[0] > ema[0] & Close[0] > ema2[0] & sps == 0;
            bool sell1 = vold1 & CrossBelow(Close, ret, 1) & Close[0] < ema[0] & Close[0] < ema2[0] & sps == 0;
            double buy1_sl = 0;
            double sell1_sl = 0;

            if (buy1 & sps== 0)
            {
                EnterLong("Buy1");
                buy1_sl = Low[1];
            }

            if (sell1 & sps== 0)
            {
                EnterShort("Sell1");
                sell1_sl = High[1];
            }

            double shortTrailPerc = longTrailPerc; // input.float(title="Trail Short Loss (%)" , minval=0.0, step=0.1, defval=1) * 0.01

            // Determine trail stop loss prices
            double longStopPrice = 0.0; 
            double shortStopPrice = 0.0;

            longStopPrice = if (strategy.position_size > 0)
                stopValue = close * (1 - longTrailPerc)
                math.max(stopValue, longStopPrice[1], buy1_sl)
            else
                0

            shortStopPrice:= if (strategy.position_size < 0)
                stopValue = close * (1 + shortTrailPerc)
                math.min(stopValue, shortStopPrice[1], sell1_sl)
            else
                999999



            long_sl1 = longStopPrice
            short_sl1 = shortStopPrice
            strategy.exit("Ex-Buy1", "Buy1", stop = long_sl1, alert_message = closelong)
            strategy.exit("Ex-Sell1", "Sell1", stop = short_sl1, alert_message = closeshort)


            plotshape(buy1 ? low : na, title = "Buy1", text = "Buy1", location = location.belowbar, style = shape.labelup, size = size.tiny, color = color.green, textcolor = color.white)
            plotshape(sell1 ? high : na, title = "Sell1", text = "Sell1", location = location.abovebar, style = shape.labeldown, size = size.tiny, color = color.red, textcolor = color.white)


        }

        protected void Strategy_2()
        {

        }
        protected void Strategy_3()
        {

        }
        protected void Strategy_4()
        {

        }
        protected void Strategy_5()
        {

        }

        #region Properties   

        // +------------------------------------------------------------+
        // | MT4/5  (order 1)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Qty", Order = 1, GroupName = "MT4/5 Settings", Description = "Risk")]
        public float pc_risk
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "License ID", Order = 2, GroupName = "MT4/5 Settings", Description = "This is your license ID")]
        public string pc_id
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "MetaTrader Symbol", Order = 3, GroupName = "MT4/5 Settings", Description = "This is your broker's MetaTrader symbol name")]
        public string pc_prefix
        { get; set; }

        // +------------------------------------------------------------+
        // | EMA (order 2)
        // +------------------------------------------------------------+
        
            // EMA 1
        [NinjaScriptProperty]
        [Display(Name = "Show EMA_1 Indicator", Order = 1, GroupName = "EMA", Description = "Show EMA_1 indicator on chart")]
        public bool showEma1
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA1 Src", Order = 2, GroupName = "EMA", Description = "EMA source")]
        public int ema_src
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA1 Len", Order = 3, GroupName = "EMA", Description = "EMA source")]
        public int ema_len
        { get; set; }

            // EMA 2
        [NinjaScriptProperty]
        [Display(Name = "Show EMA_2 Indicator", Order = 4, GroupName = "EMA", Description = "Show EMA_2 indicator on chart")]
        public bool showEma2
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA2 Src", Order = 5, GroupName = "EMA", Description = "EMA source")]
        public int ema2_src
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA2 Len", Order = 6, GroupName = "EMA", Description = "EMA source")]
        public int ema2_len
        { get; set; }

        // +------------------------------------------------------------+
        // | Trend Trader Strategy (grp3)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Length", Order = 1, GroupName = "Trend Trader Strategy", Description = "EMA source")]
        public int Length
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Multiplier", Order = 2, GroupName = "Trend Trader Strategy", Description = "EMA source")]
        public double Multiplier
        { get; set; }

        // +------------------------------------------------------------+
        // | Volume Based Coloured Bars (grp4)
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "length", Order = 1, GroupName = "Volume Based Coloured Bars", Description = "EMA source")]
        public int smaLength
        { get; set; }
        
        // +------------------------------------------------------------+
        // | Strategy 1
        // +------------------------------------------------------------+
        [NinjaScriptProperty]
        [Display(Name = "Trail S.Loss (%)", Order = 4, GroupName = "Strategy 1", Description = "This is your broker's MetaTrader symbol name")]
        public double longTrailPerc
        { get; set; }

        #endregion
    }
}

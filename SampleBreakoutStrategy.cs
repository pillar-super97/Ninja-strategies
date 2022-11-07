// 
// Copyright (C) 2015, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class SampleBreakoutStrategy : Strategy
    {
		private double highestHigh;

        protected override void OnStateChange()
        {
			if(State == State.SetDefaults)
			{
				Description							= @"Sample monitoring for and trading a breakout";
				Name                                = "Sample breakout strategy";
				Calculate                           = Calculate.OnBarClose;
				EntriesPerDirection                 = 1;
				EntryHandling                       = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy 		= true;
          		ExitOnSessionCloseSeconds    		= 30;
				IsFillLimitOnTouch                  = false;
				MaximumBarsLookBack                 = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution                 = OrderFillResolution.Standard;
				Slippage                            = 0;
				StartBehavior                       = StartBehavior.WaitUntilFlat;
				TimeInForce                         = TimeInForce.Gtc;
				TraceOrders                         = false;
				RealtimeErrorHandling               = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling                  = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade                 = 30;
				highestHigh			                = 0;
			}
        }

        protected override void OnBarUpdate()
        {
			// Resets the highest high at the start of every new session
			if (Bars.IsFirstBarOfSession)
				highestHigh = High[0];
			
			// Stores the highest high from the first 30 bars
			if(Bars.BarsSinceNewTradingDay < 30 && High[0] > highestHigh)			
				highestHigh = High[0];
			
			// Entry Condition: Submit a buy stop order one tick above the first 30 bar high. Cancel if not filled within 10 bars.
			if(Bars.BarsSinceNewTradingDay > 30 && Bars.BarsSinceNewTradingDay < 40)
			{
				
				if(Close[0] >= highestHigh + TickSize) 
					return;
				// EnterLongStopMarket() can be used to generate buy stop orders.
				EnterLongStopMarket(highestHigh + TickSize);
			}
			
			// Exit Condition: Close positions after 10 bars have passed
			if (BarsSinceEntryExecution() > 10)
				ExitLong();
        }
    }
}

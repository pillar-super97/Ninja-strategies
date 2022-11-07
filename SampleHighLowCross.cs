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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// Simple strategy that monitors for a breakout from the highets high or the lowest low.
    /// </summary>
    [Description("Simple strategy that monitors for a breakout from the highets high or the lowest low.")]
    public class SampleHighLowCross : Strategy
    {
		private double highestHigh	= 0;
		private double lowestLow	= 0;

        protected override void OnStateChange()
        {
			if(State == State.SetDefaults)
			{						
            	Calculate				= Calculate.OnBarClose;
				BarsRequiredToTrade		= 10;
			}
			
			else if(State == State.Historical)
			{
				// Sets a trail stop of 10 ticks
				SetTrailStop(CalculationMode.Ticks, 10);
			}
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			// Resets the highest high and lowest low at the start of every new session
			if (Bars.IsFirstBarOfSession)
			{
				highestHigh		= High[0];
				lowestLow		= Low[0];
			}
			
			// Stores the highest high and lowest low from the first 15 bars
			if (Bars.BarsSinceNewTradingDay < 15)
			{
				// If current high is greater than current highest high, set highest high to current high
				if (High[0] > highestHigh)
					highestHigh = High[0];
				
				// If current low is lower than current lowest low, set lowest low to current low
				if (Low[0] < lowestLow)
					lowestLow = Low[0];
			}
			
			/* Entry Condition: After the first 15 bars, submit an entry order when price crosses either the highest high or the lowest low
			if you have no market positions open */
			if (Bars.BarsSinceNewTradingDay > 15 && Position.MarketPosition == MarketPosition.Flat)
			{
				// If price crosses above the highest high, enter long
				if (CrossAbove(Close, highestHigh, 1))
					EnterLong();
				
				// If price crosses below the lowest low, enter short
				else if (CrossBelow(Close, lowestLow, 1))
					EnterShort();
			}
        }
    }
}

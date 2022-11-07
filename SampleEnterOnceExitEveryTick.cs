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
	public class SampleEnterOnceExitEveryTick : Strategy
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				/* With Calculate = Calculate.OnEachTick, OnBarUpdate() gets called on every tick.
				   We can then filter for the first tick for entries and let the exit conditions run on every OnBarUpdate(). */
				Calculate						= Calculate.OnEachTick;
				Name                            = "Sample enter once exit every tick";
			}
			else if (State == State.Configure)
			{
				// Add indicators to the chart.
				AddChartIndicator(LinReg(Median, 10));
				AddChartIndicator(SMA(Median, 10));

				// Set the plot colors.
				LinReg(Median, 10).Plots[0].Brush = Brushes.RoyalBlue;
				SMA(Median, 10).Plots[0].Brush = Brushes.Red;
			}
		}

		protected override void OnBarUpdate()
		{
			// Return if historical--this sample utilizes tick information, so is necessary to run in real-time.
			if (State == State.Historical)
				return;
			/* IsFirstTickOfBar specifies that this section of code will only run once per bar, at the close of the bar as indicated by the open of the next bar.
			   NinjaTrader decides a bar is closed when the first tick of the new bar comes in,
			   therefore making the close of one bar and the open of the next bar virtually the same event. */
			if (IsFirstTickOfBar && Position.MarketPosition == MarketPosition.Flat)
			{
				/* Since we're technically running calculations at the open of the bar (with open and close being the same event),
				   we need to shift index values back one value, because the open = close = high = low at the first tick.
				   Shifting the values ensures we are using data from the recently closed bar. */
				if (LinReg(Median, 10)[1] > LinReg(Median, 10)[2] && LinReg(Median, 10)[1] > SMA(Median, 10)[1])
					EnterLong("long entry");
			}

			// Run these calculations (on every tick, because Calculate = Calculate.OnEachTick) only if the current position is long.
			if (Position.MarketPosition == MarketPosition.Long)
			{
				/* This CrossBelow() condition can and will generate intrabar exits (Calculate = Calculate.OnEachTick). Because this logic is run once for
				   every tick received, it will provide the quickest exit for your strategy, and will exit as soon as the LinReg line crosses below the SMA line. */
				if (CrossBelow(LinReg(Median, 10), SMA(Median, 10), 1))
					ExitLong("LinReg cross SMA exit", "long entry");
			}
		}
		#region Properties
		#endregion
	}
}
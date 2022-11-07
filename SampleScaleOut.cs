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
	public class SampleScaleOut : Strategy
	{
		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				Name				= "Sample scale out";
				Calculate 			= Calculate.OnBarClose;
				BarsRequiredToTrade = 20;

				/* The following settings configure your strategy to execute only one entry for each uniquely named entry signal.
				This can be configured in the SetDefaults state or via the Strategy dialog window when running or backtesting a strategy */
				EntriesPerDirection = 1;
				EntryHandling 		= EntryHandling.UniqueEntries;
				
			}
			else if (State == State.Configure)
			{
				AddChartIndicator(Bollinger(2,14));

				/* These Set methods will place Profit Target and Trail Stop orders for our entry orders.
				Notice how the Profit Target order is only tied to our order named 'Long 1a'. This is the crucial step in achieving the following behavior. 
				If the price never reaches our Profit Target, both long positions will be closed via our Trail Stop.
				If the price does hit our Profit Target, half of our position will be closed leaving the remainder to be closed by our Trail Stop. */
				SetProfitTarget("Long 1a", CalculationMode.Ticks, 10);
				SetTrailStop(CalculationMode.Ticks, 8); 
			}
		}

		protected override void OnBarUpdate()
		{
			// Entry Condition: When the Low crosses below the lower bollinger band, enter long
			if (CrossBelow(Low, Bollinger(2, 14).Lower, 1))
			{
				// Only allow entries if we have no current positions open
				if (Position.MarketPosition == MarketPosition.Flat)
				{
					/* Enters two long positions.
					We submit two orders to allow us to be able to scale out half of the position at a time in the future.
					With individual entry names we can differentiate between the first half and the second half of our long position.
					This lets us place a Profit Target order only for the first half and Trail Stops for both. */
					EnterLong("Long 1a");
					EnterLong("Long 1b");
				}
			}
		}

		#region Properties
		#endregion
	}
}

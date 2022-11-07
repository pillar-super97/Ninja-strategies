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
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Cbi;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleIntrabarBacktest : Strategy
	{
		private int	fast;
		private int	slow;

		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{				
				Fast			= 10;
				Slow			= 25;	
				Calculate		= Calculate.OnBarClose;
				Name			= "SampleIntrabarBacktest";
			}
			
			else if(State == State.Configure)
			{
				/* Add a secondary bar series. 
				Very Important: This secondary bar series needs to be smaller than the primary bar series.
				
				Note: The primary bar series is whatever you choose for the strategy at startup. In this example I will
				reference the primary as a 5 min bars series, and we will use a 1 min bars series for our secondary.
				For the greatest intrabar granularity with order fills, a 1 tick data series may be added, with the orders
				submitted to the single tick data series. */
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				//AddDataSeries(Data.BarsPeriodType.Tick, 1);
				
				// Add two EMA indicators to be plotted on the primary bar series
				AddChartIndicator(EMA(Fast));
				AddChartIndicator(EMA(Slow));
				
				/* Adjust the color of the EMA plots.
				For more information on this please see this tip: https://ninjatrader.com/support/helpGuides/nt8/adding_indicators_to_strategie.htm */
				EMA(Fast).Plots[0].Brush = Brushes.Blue;
				EMA(Slow).Plots[0].Brush = Brushes.Green;
			}
        }

		protected override void OnBarUpdate()
		{
			/* When working with multiple bar series objects it is important to understand the sequential order in which the
			OnBarUpdate() method is triggered. The bars will always run with the primary first followed by the secondary and
			so on.
			
			Important: Primary bars will always execute before the secondary bar series.
			If a bar is timestamped as 12:00PM on the 5min bar series, the call order between the equally timestamped 12:00PM
			bar on the 1min bar series is like this:
				12:00PM 5min
				12:00PM 1min
				12:01PM 1min
				12:02PM 1min
				12:03PM 1min
				12:04PM 1min
				12:05PM 5min
				12:05PM 1min 
			
			When the OnBarUpdate() is called from the primary bar series (5min series in this example), do the following */
			if (BarsInProgress == 0)
			{
				// When the fast EMA crosses above the slow EMA, enter long on the secondary (1min) bar series
				if (CrossAbove(EMA(Fast), EMA(Slow), 1))
				{
					/* The entry condition is triggered on the primary bar series, but the order is sent and filled on the
					secondary bar series. The way the bar series is determined is by the first parameter: 0 = primary bars,
					1 = secondary bars, 2 = tertiary bars, etc. */
					EnterLong(1, 1, "Long: 1min");
				}
				// When the fast EMA crosses below the slow EMA, enter short on the secondary (1min) bar series
				else if (CrossBelow(EMA(Fast), EMA(Slow), 1))
				{
					/* The entry condition is triggered on the primary bar series, but the order is sent and filled on the
					secondary bar series. The way the bar series is determined is by the first parameter: 0 = primary bars,
					1 = secondary bars, 2 = tertiary bars, etc. */
					EnterShort(1, 1, "Short: 1min");
				}
			}
			// When the OnBarUpdate() is called from the secondary bar series, do nothing.
			else
			{
				return;
			}
		}

		#region Properties

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Fast
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Slow
		{ get; set; }

		#endregion
	}
}

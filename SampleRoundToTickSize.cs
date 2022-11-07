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
	public class SampleRoundToTickSize : Strategy
	{
		private int		fast		= 10;	// This variable represents the period of the fast moving average.
		private int		slow		= 25;	// This variable represents the period of the slow moving average.
		private double	sMAprice	= 0;	// This variable represents the unrounded value for entries.

		protected override void OnStateChange()
		{
			if(State == State.SetDefaults)
			{
				Name				= "Sample round 2 TickSize";
				Calculate			= Calculate.OnBarClose;
				BarsRequiredToTrade = 20;
				Fast				= 10;
				Slow				= 25;
			}
			else if(State == State.Configure)
			{
				// Plots for visual reference.
				AddChartIndicator(SMA(Fast));
				AddChartIndicator(SMA(Slow));
				
				SMA(Fast).Plots[0].Brush = Brushes.Orange;
				SMA(Slow).Plots[0].Brush = Brushes.Green;
			}
		}

		protected override void OnBarUpdate()
		{
			// The position is flat, do entry calculations and enter a position if necessary.
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				// First, get the fast SMA value.
				sMAprice = SMA(Fast)[0];
								
				// Entry conditions.
				if (SMA(Fast)[0] > SMA(Slow)[0])
				{	
					/* Upon order submission, NinjaTrader automatically rounds prices to the nearest tick value.
					   When debugging, it can be helpful to round values just like NinjaTrader does to see what is happening. */
					Print("Send Limit Order to buy at " + Instrument.MasterInstrument.RoundToTickSize(sMAprice));
					EnterLongLimit(sMAprice, "Long");
				}
				else if (SMA(Fast)[0] < SMA(Slow)[0])
				{
					Print("Send Limit Order to sell at " + Instrument.MasterInstrument.RoundToTickSize(sMAprice));
				    EnterShortLimit(sMAprice, "Short");
				}
			}
			
			// Simple crossover exit conditions.
			if (CrossAbove(SMA(Fast), SMA(Slow), 1))
			{
				ExitShort();
			}
			else if (CrossBelow(SMA(Fast), SMA(Slow), 1))
			{
				ExitLong();
			}
		}

		#region Properties
		[Display(GroupName="Parameters", Description="Period for fast MA")]
		public int Fast
		{
			get { return fast; }
			set { fast = Math.Max(1, value); }
		}

		[Display(GroupName="Parameters", Description="Period for slow MA")]
		public int Slow
		{
			get { return slow; }
			set { slow = Math.Max(1, value); }
		}
		#endregion
	}
}
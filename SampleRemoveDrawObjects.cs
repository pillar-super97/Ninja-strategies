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
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;


#endregion

//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleRemoveDrawObjects : Strategy
	{
		private SMA smaFast;
		private SMA smaSlow;
		private int barNumberOfOrder	=	0;
		private int lineLength			=	0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description	= NinjaTrader.Custom.Resource.NinjaScriptStrategyDescriptionSampleMACrossOver;
 				Name		= "Sample remove draw objects";
				Fast		= 10;
				Slow		= 25;
				BarsRequiredToTrade = 20;
			}
			else if (State == State.Configure)
			{
				smaFast = SMA(Fast);
				smaSlow = SMA(Slow);

				smaFast.Plots[0].Brush = Brushes.Orange;
				smaSlow.Plots[0].Brush = Brushes.Green;

				AddChartIndicator(smaFast);
				AddChartIndicator(smaSlow);
				
				SetProfitTarget("MA cross", CalculationMode.Ticks, 10);
				SetStopLoss("MA cross", CalculationMode.Ticks, 10, false);
			}
		}

		protected override void OnBarUpdate()
		{
		if (State == State.Historical || CurrentBar < Math.Max(Fast, Slow))
				return;			
			
			if (CrossAbove(smaFast, smaSlow, 1))
			{
			    EnterLong("MA cross");
				barNumberOfOrder = CurrentBar;
			}
			else if (CrossBelow(smaFast, smaSlow, 1))
			{
			    EnterShort("MA cross");
				barNumberOfOrder = CurrentBar;
			}
			// If the position is long or short, draw lines at the entry, target, and stop prices.
			if (Position.MarketPosition == MarketPosition.Long)
			{
				/* Calculate the line length by taking the greater of two values (3 and the difference between the current bar and the entry bar).
				The line will always be at least 3 bars long. */
				lineLength = Math.Max(CurrentBar - barNumberOfOrder, 3);
				Draw.Line(this, "Target", false, lineLength, Position.AveragePrice + 4 * TickSize, 0, Position.AveragePrice + 4 * TickSize, Brushes.Green, DashStyleHelper.Solid, 6);
				Draw.Line(this,"Stop", false, lineLength, Position.AveragePrice - 4 * TickSize, 0, Position.AveragePrice - 4 * TickSize, Brushes.Red, DashStyleHelper.Solid, 6);
				Draw.Line(this, "Entry", false, lineLength, Position.AveragePrice, 0, Position.AveragePrice, Brushes.Brown, DashStyleHelper.Solid, 6);
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				lineLength = Math.Max(CurrentBar - barNumberOfOrder, 3);
				Draw.Line (this,"Target", false, lineLength, Position.AveragePrice - 4 * TickSize, 0, Position.AveragePrice - 4 * TickSize, Brushes.Green, DashStyleHelper.Solid, 6);
				Draw.Line(this, "Stop", false, lineLength, Position.AveragePrice + 4 * TickSize, 0, Position.AveragePrice + 4 * TickSize, Brushes.Red, DashStyleHelper.Solid, 6);
				Draw.Line(this, "Entry", false, lineLength, Position.AveragePrice, 0, Position.AveragePrice, Brushes.Brown, DashStyleHelper.Solid, 6);
			}
			// The strategy is now flat, remove all draw objects.
			else if (Position.MarketPosition == MarketPosition.Flat)
			{
				RemoveDrawObject("Target");
				RemoveDrawObject("Stop");
				RemoveDrawObject("Entry");
			}	
		}
		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
		public int Fast
		{ get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
		public int Slow
		{ get; set; }
		#endregion
	}
}
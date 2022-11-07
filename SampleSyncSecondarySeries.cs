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
	public class SampleSyncSecondarySeries : Strategy
	{
		// Declare two DataSeries objects
		private Series<double> primarySeries;
		private Series<double> secondarySeries;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					    = @"Synchronizing a DataSeries object to a secondary time frame";
				Name                            = "SampleSyncSecondarySeries";
				Calculate					    = Calculate.OnBarClose;
				EntriesPerDirection			    = 1;
				EntryHandling				    = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy    = true;
          		ExitOnSessionCloseSeconds       = 30;
				IsFillLimitOnTouch			    = false;
				MaximumBarsLookBack			    = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution			    = OrderFillResolution.Standard;
				Slippage					    = 0;
				StartBehavior				    = StartBehavior.WaitUntilFlat;
				TimeInForce					    = TimeInForce.Gtc;
				TraceOrders					    = false;
				RealtimeErrorHandling		    = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling			    = StopTargetHandling.PerEntryExecution;
			}
			else if (State == State.Configure)
			{
				// Adds a secondary bar object to the strategy.
				AddDataSeries(BarsPeriodType.Minute, 5);
				
				// Stop-loss orders are placed 5 ticks below average entry price
				SetStopLoss(CalculationMode.Ticks, 5);

				// Profit target orders are placed 10 ticks above average entry price
				SetProfitTarget(CalculationMode.Ticks, 10);
			}
			else if (State == State.DataLoaded)
			{
				// Syncs a DataSeries object to the primary bar object
				primarySeries = new Series<double>(this);
				
				/* Syncs another DataSeries object to the secondary bar object.
				We use an arbitrary indicator overloaded with an ISeries<double> input to achieve the sync.
				The indicator can be any indicator. The Series<double> will be synced to whatever the
				BarsArray[] is provided.*/
				secondarySeries = new Series<double>(SMA(BarsArray[1], 50));
			}
		}

		protected override void OnBarUpdate()
		{
			// Executed on primary bar updates only
			if (BarsInProgress == 0)
			{
				// Set DataSeries object to store the trading range of the primary bar
				primarySeries[0] = Close[0] - Open[0];
			}
			else if (BarsInProgress == 1) 	// Executed on secondary bar updates only
			{
				// Set the DataSeries object to store the trading range of the secondary bar
				secondarySeries[0] = Close[0] - Open[0];
			}

			// When both trading ranges of the current bars on both time frames are positive and there is not currently a position, enter long
			if (primarySeries[0] > 0 && secondarySeries[0] > 0 && Position.MarketPosition == MarketPosition.Flat)
				EnterLong();
		}
	}
}
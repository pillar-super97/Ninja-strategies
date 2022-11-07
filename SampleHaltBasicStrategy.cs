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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SampleHaltBasicStrategy : Strategy
	{
		private Order myEntryOrder = null;
		private bool entrySubmit = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Halting a Strategy Once User Defined Conditions Are Met.";
				Name						= "Sample Halt Basic Strategy";
				Calculate					= Calculate.OnBarClose;
				EntriesPerDirection			= 1;
				EntryHandling				= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds	= 30;
				IsFillLimitOnTouch			= false;
				MaximumBarsLookBack			= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution			= OrderFillResolution.Standard;
				Slippage					= 0;
				StartBehavior				= StartBehavior.WaitUntilFlat;
				TimeInForce					= TimeInForce.Gtc;
				TraceOrders					= false;
				RealtimeErrorHandling		= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling			= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade			= 20;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			// After our strategy has a PnL greater than $1000 or less than -$400 we will stop our strategy
			if (SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit > 1000 
				|| SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit < -400)
			{
				/* A custom method designed to close all open positions and cancel all working orders will be called.
				This will ensure we do not have an unmanaged position left after we halt our strategy. */
				StopStrategy();
				
				// Halt further processing of our strategy
				return;
			}
			
			/* This print will show every time the OnBarUpdate() method is called. When strategy is stopped it will
			no longer print. */
			Print("OnBarUpdate(): " + Time[0]);
			
			if (Close[0] > Open[0] && entrySubmit == false)
			{
				/* Submits a Long Limit order at the current bar's low. This order has liveUntilCancelled set to true
				so it does not require resubmission on every new bar to keep it alive. */ 
				EnterLongLimit(0, true, 1, Low[0], "Long Limit");
				
				// Set our bool to true to prevent resubmission of our entry order
				entrySubmit = true;
			}
			
			// After 5 bars have passed since entry, exit the position
			if (BarsSinceEntryExecution() >= 5)
			{
				// Submit our exit order
				ExitLong();
			}
			
			// After our position is closed we can reset our bool to allow for entries again
			if (Position.MarketPosition == MarketPosition.Flat && entrySubmit)
			{
				// Reset our bool to false to allow for submission of our entry order
				entrySubmit = false;
			}
		}

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
        {
            if (myEntryOrder != null && order.Name == "Long limit")
            {
                // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
                // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
                myEntryOrder = order;
            }
        }


        private void StopStrategy()
		{
			// If our Long Limit order is still active we will need to cancel it.
			CancelOrder(myEntryOrder);
			
			// If we have a position we will need to close the position
			if (Position.MarketPosition == MarketPosition.Long)
				ExitLong();			
			else if (Position.MarketPosition == MarketPosition.Short)
				ExitShort();
		}
		
	}
}

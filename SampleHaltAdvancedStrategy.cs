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
	public class SampleHaltAdvancedStrategy : Strategy
	{
		private Order myEntryOrder = null;
		private Order myExitOrder = null;
		private Order stopStrategyLong = null;
		private Order stopStrategyShort = null;
		private bool haltProcessing = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Halting a Strategy Once User Defined Conditions Are Met.";
				Name						= "Sample Halt Advanced Strategy";
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
			
			// When we determine to stop our strategy all logic in the OnBarUpdate() method will cease.
			if (haltProcessing)
				return;
			
			// After our strategy has a PnL greater than $1000 or less than -$400 we will stop our strategy
			if (SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit > 1000 
				|| SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit < -400)
			{
				// A custom method designed to close all open positions and cancel all working orders will be called.
				StopStrategy();
			}
			
			/* This print will show every time the OnBarUpdate() method is called. When strategy is stopped it will
			no longer print. */
			Print("OnBarUpdate(): " + Time[0]);
			
			if (myEntryOrder == null && Close[0] > Open[0])
			{
				/* Submits a Long Limit order at the current bar's low. This order has liveUntilCancelled set to true
				so it does not require resubmission on every new bar to keep it alive. */ 
				EnterLongLimit(0, true, 1, Low[0], "Long Limit");
			}
			
			// After 5 bars have passed since entry, exit the position
			if (BarsSinceEntryExecution() >= 5)
			{
				// Submit our exit order
				myExitOrder = ExitLong("Exit Long Market");
			}
		}
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			/* When we determine to stop our strategy all logic in the OnMarketData() method will cease.
			We need to stop our strategy on every method we use; not just the OnBarUpdate() method. */
			if (haltProcessing)
				return;
			
			/* This print will show every time the OnMarketData() method is called. When the strategy is stopped it
			will no longer print. */
			Print("OnMarketData: " + e.Time + " " + e.Price + " " + e.Volume + " " + e.MarketDataType);
		}
		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// When we determine to stop our strategy all logic in the OnOrderUpdate() method will cease.
			if (haltProcessing)
				return;
			
			/* This print will show every time the OnOrderUpdate() method is called. When the strategy is stopped it
			will no longer print. */
			Print("OnOrderUpdate: " + order.ToString());
			
			/* Handle our entry order here. If our entry order is cancelled or rejected we want to reset myEntryOrder
			so we can enter the next time our entry signal appears. */
			if (myEntryOrder != null && order.Name == "Long limit")
			{
                // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
                // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
                myEntryOrder = order;

                if (order.OrderState == OrderState.Cancelled || order.OrderState == OrderState.Rejected)
					myEntryOrder = null;
			}
			
			// Handle our exit order here.
			if (myExitOrder != null && order.Name == "Exit Long Market")
			{
				// If our exit order is filled we want to reset myEntryOrder so we can submit orders again.
				if (order.OrderState == OrderState.Filled)
					myEntryOrder = null;
				
				// If our exit order encounters some problems we will resubmit the order again.
				else if (order.OrderState == OrderState.Cancelled || order.OrderState == OrderState.Rejected)
					myExitOrder = ExitLong("Exit Long Market");
			}
			
			/* Handle our haltProcessing orders here. We can identify if it is our exit orders calling that called the OnOrderUpdate()
			method by checking our IOrder's tokens. */
			if ((stopStrategyLong != null && order.Name == "Stop Long")
				|| (stopStrategyShort != null && order.Name == "Stop Short"))
			{
				Print(order.ToString());
				
				// Once our exit order is filled we are safe to stop our strategy
				if (order.OrderState == OrderState.Filled)
					haltProcessing = true;
				
				// If our exit order encounters problems, resubmit the order
				else if (order.OrderState == OrderState.Cancelled || order.OrderState == OrderState.Rejected)
				{
					if (Position.MarketPosition == MarketPosition.Long)
						stopStrategyLong = ExitLong("Stop Long");
					else if (Position.MarketPosition == MarketPosition.Short)
						stopStrategyShort = ExitShort("Stop Short");
				}
			}
		}
		
		private void StopStrategy()
		{
			// If our Long Limit order is still active we will need to cancel it.
			CancelOrder(myEntryOrder);
			
			// If we have a position we will need to close the position
			if (Position.MarketPosition == MarketPosition.Long)
			{
				/* We use an IOrder here so we can monitor when our exit order becomes filled in the
				OnOrderUpdate() method. */
				stopStrategyLong = ExitLong("Stop Long");
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				/* We use an IOrder here so we can monitor when our exit order becomes filled in the
				OnOrderUpdate() method. */
				stopStrategyShort = ExitShort("Stop Short");
			}
			else
			{
				/* If we have no more active orders and all our positions are closed, it is now safe to
				stop the strategy. */
				haltProcessing = true;
			}
		}
	}
}

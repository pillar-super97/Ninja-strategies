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
	public class SampleCancelOrder : Strategy
	{
		private Order 	entryOrder 			= null; // This variable holds an object representing our entry order.
		private Order 	stopOrder 			= null; // This variable holds an object representing our stop loss order.
		private Order 	targetOrder 		= null; // This variable holds an object representing our profit target order.
		private Order	marketOrder			= null; // This variable holds an object representing our market EnterLong() order.
		private int 	barNumberOfOrder 	= 0;	// This variable is used to store the entry bar
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Using CancelOrder() method to cancel orders.";
				Name						= "Sample Cancel Order";
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
			
			// First, we need a simple entry. Then entryOrder == null checks to make sure entryOrder does not contain an order yet.
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				// Check IOrder objects for null to ensure there are no working entry orders before submitting new entry order
				if (entryOrder == null && marketOrder == null && Close[0] > Close[1])
				{
					/* Offset 5 ticks below the low to try and make the order not execute to demonstrate the CancelOrder() method. */
					EnterLongLimit(0, true, 1, Low[0] - 5 * TickSize, "long limit entry");
					
					// Here, we assign barNumberOfOrder the CurrentBar, so we can check how many bars pass after our order is placed.
					barNumberOfOrder = CurrentBar;
				}				
				
				// If entryOrder has not been filled within 3 bars, cancel the order.
				else if (entryOrder != null && CurrentBar > barNumberOfOrder + 3)
				{
					// When entryOrder gets cancelled below in OnOrderUpdate(), it gets replaced with a Market Order via EnterLong()
					CancelOrder(entryOrder);
				}
			}
		}
		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// Checks for all updates to entryOrder.
			if (entryOrder != null && order.Name == "long limit entry")
			{
                // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
                // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
                entryOrder = order;

				// Check if entryOrder is cancelled.
				if (order.OrderState == OrderState.Cancelled)
				{
					// Reset entryOrder back to null
					entryOrder = null;
					
					// Replace entry limit order with a market order.
					marketOrder = EnterLong(1, "market order");
				}
			}
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
			/* We advise monitoring OnExecution() to trigger submission of stop/target orders instead of OnOrderUpdate() since OnExecution() is called after OnOrderUpdate()
			which ensures your strategy has received the execution which is used for internal signal tracking.
			
			This first if-statement is in place to deal only with the long limit entry. */
			if (entryOrder != null && entryOrder == execution.Order)
			{
				// This second if-statement is meant to only let fills and cancellations filter through.
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					// Simple stop and target
					stopOrder = ExitLongStopMarket(0, true, 1, execution.Price - 20 * TickSize, "stop", "long limit entry");
					targetOrder = ExitLongLimit(0, true, 1, execution.Price + 40 * TickSize, "target", "long limit entry");
					
					// Resets the entryOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						entryOrder 	= null;
					}
				}
			}
			
			// This if-statments lets execution details for the market order filter through.
			else if (marketOrder != null && marketOrder == execution.Order)
			{
				// Check only for fills and cancellations.
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					stopOrder = ExitLongStopMarket(0, true, 1, execution.Price - 15 * TickSize, "stop", "market order");
					targetOrder = ExitLongLimit(0, true, 1, execution.Price + 30 * TickSize, "target", "market order");
					
					// Resets the marketOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						marketOrder = null;
					}
				}
			}
		}
	}
}
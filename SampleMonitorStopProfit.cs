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
    public class SampleMonitorStopProfit : Strategy
    {       
		/* Create two lists for storing our stop-loss and profit target orders. An order collection is necessary
		because of the possibilities of a partial fill where two or more stop/profit orders will be submitted for one entry
		condition. The List<Order> will allow us to keep track of each individual order. */
		private List<Order> stopLossOrders;
		private List<Order> profitTargetOrders;
		private bool		doneForSession = false;
        
        protected override void OnStateChange()
        {
			if(State == State.SetDefaults)
			{	
				Calculate 			= Calculate.OnBarClose;
				Name				= "Sample monitor stop profit";
				BarsRequiredToTrade = 20;

				// Allow two simultaneous entries
				EntriesPerDirection = 2;

				
			}
			else if (State == State.Configure)
			{
				profitTargetOrders	= new List<Order>();
				stopLossOrders 		= new List<Order>();
				// Submit stop-loss and profit target orders
				SetStopLoss(CalculationMode.Ticks, 5);
				SetProfitTarget(CalculationMode.Ticks, 10);
			}
        }

        protected override void OnBarUpdate()
        {
			if(State == State.Historical) return;
			// Reset our bool variable that limits our strategy to two entries at the beginning of each session
			if (Bars.IsFirstBarOfSession && IsFirstTickOfBar)
				doneForSession = false;
			
			// If two entries have been made in this session already stop submitting more entry orders
			if (doneForSession)
				return;
			
			// Enter long 1 contract
			if (Close[0] > Open[0])
				EnterLong(1);
			
			// Enter long another 1 contract after the first contract was filled
			if (Position.Quantity == 1)
				EnterLong(1);
			
			// After both orders have been filled prevent the strategy from submitting more orders during this session
			else if (Position.Quantity == 2)
				doneForSession = true;
        }

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// If OnOrderUpdate() is called from a stop loss or profit target order add it to the appropriate collection
			if (order.OrderState == OrderState.Submitted)
			{
				// Add the "Stop loss" orders to the Stop Loss collection
				if (order.Name == "Stop loss")
					stopLossOrders.Add(order);
				
				// Add the "Profit target" orders to the Profit Target collection
				else if (order.Name == "Profit target")
					profitTargetOrders.Add(order);
			}
			
			// Process stop loss orders
			if (stopLossOrders.Contains(order))
			{
				// Check order for terminal state
				if (order.OrderState == OrderState.Cancelled || order.OrderState == OrderState.Filled || order.OrderState == OrderState.Rejected)
				{
					// Print out information about the order
					Print(order);
					
					// Remove from collection
					stopLossOrders.Remove(order);
				}
				else
				{
					// Print out the current stop loss price
					Print("The order name " + order.Name + " stop price is currently " + stopPrice);
				}
			}
			
			// Process profit target orders
			if (profitTargetOrders.Contains(order))
			{
				// Check order for terminal state
				if (order.OrderState == OrderState.Cancelled || order.OrderState == OrderState.Filled || order.OrderState == OrderState.Rejected)
				{
					// Print out information about the order
					Print(order);
					
					// Remove from collection
					profitTargetOrders.Remove(order);
				}
				else
				{
					// Print out the current stop loss price
					Print("The order name " + order.Name + " limit price is currently " + limitPrice);
				}
			}
		}
        #region Properties
        #endregion
    }
}

// 
// Copyright (C) 2019, NinjaTrader LLC <www.ninjatrader.com>.
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
    public class SampleOnOrderUpdate : Strategy
    {
        private Order entryOrder = null; // This variable holds an object representing our entry order
        private Order stopOrder = null; // This variable holds an object representing our stop loss order
        private Order targetOrder = null; // This variable holds an object representing our profit target order
        private int sumFilled = 0; // This variable tracks the quantities of each execution making up the entry order

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Sample Using OnOrderUpdate() and OnExecution() methods to submit protective orders";
                Name = "SampleOnOrderUpdate";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.ByStrategyPosition;
                BarsRequiredToTrade = 20;
            }
            else if (State == State.Realtime)
            {
                // one time only, as we transition from historical
                // convert any old historical order object references
                // to the new live order submitted to the real-time account
                if (entryOrder != null)
                    entryOrder = GetRealtimeOrder(entryOrder);
                if (stopOrder != null)
                    stopOrder = GetRealtimeOrder(stopOrder);
                if (targetOrder != null)
                    targetOrder = GetRealtimeOrder(targetOrder);
            }
        }

        protected override void OnBarUpdate()
        {
            // Submit an entry market order if we currently don't have an entry order open and are past the BarsRequiredToTrade bars amount
            if (entryOrder == null && Position.MarketPosition == MarketPosition.Flat && CurrentBar > BarsRequiredToTrade)
            {
                /* Enter Long. We will assign the resulting Order object to entryOrder1 in OnOrderUpdate() */
                EnterLong(1, "MyEntry");
            }

            /* If we have a long position and the current price is 4 ticks in profit, raise the stop-loss order to breakeven.
			We use (7 * (TickSize / 2)) to denote 4 ticks because of potential precision issues with doubles. Under certain
			conditions (4 * TickSize) could end up being 3.9999 instead of 4 if the TickSize was 1. Using our method of determining
			4 ticks helps cope with the precision issue if it does arise. */
            if (Position.MarketPosition == MarketPosition.Long && Close[0] >= Position.AveragePrice + (7 * (TickSize / 2)))
            {
                // Checks to see if our Stop Order has been submitted already
                if (stopOrder != null && stopOrder.StopPrice < Position.AveragePrice)
                {
                    // Modifies stop-loss to breakeven
                    stopOrder = ExitLongStopMarket(0, true, stopOrder.Quantity, Position.AveragePrice, "MyStop", "MyEntry");
                }
            }
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
        {
            // Handle entry orders here. The entryOrder object allows us to identify that the order that is calling the OnOrderUpdate() method is the entry order.
            // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
            // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
            if (order.Name == "MyEntry")
            {
                entryOrder = order;

                // Reset the entryOrder object to null if order was cancelled without any fill
                if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
                {
                    entryOrder = null;
                    sumFilled = 0;
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            /* We advise monitoring OnExecution to trigger submission of stop/target orders instead of OnOrderUpdate() since OnExecution() is called after OnOrderUpdate()
			which ensures your strategy has received the execution which is used for internal signal tracking. */
            if (entryOrder != null && entryOrder == execution.Order)
            {
                if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
                {
                    // We sum the quantities of each execution making up the entry order
                    sumFilled += execution.Quantity;

                    // Submit exit orders for partial fills
                    if (execution.Order.OrderState == OrderState.PartFilled)
                    {
                        stopOrder = ExitLongStopMarket(0, true, execution.Order.Filled, execution.Order.AverageFillPrice - 4 * TickSize, "MyStop", "MyEntry");
                        targetOrder = ExitLongLimit(0, true, execution.Order.Filled, execution.Order.AverageFillPrice + 8 * TickSize, "MyTarget", "MyEntry");
                    }
                    // Update our exit order quantities once orderstate turns to filled and we have seen execution quantities match order quantities
                    else if (execution.Order.OrderState == OrderState.Filled && sumFilled == execution.Order.Filled)
                    {
                        // Stop-Loss order for OrderState.Filled
                        stopOrder = ExitLongStopMarket(0, true, execution.Order.Filled, execution.Order.AverageFillPrice - 4 * TickSize, "MyStop", "MyEntry");
                        targetOrder = ExitLongLimit(0, true, execution.Order.Filled, execution.Order.AverageFillPrice + 8 * TickSize, "MyTarget", "MyEntry");
                    }

                    // Resets the entryOrder object and the sumFilled counter to null / 0 after the order has been filled
                    if (execution.Order.OrderState != OrderState.PartFilled && sumFilled == execution.Order.Filled)
                    {
                        entryOrder = null;
                        sumFilled = 0;
                    }
                }
            }

            // Reset our stop order and target orders' Order objects after our position is closed. (1st Entry)
            if ((stopOrder != null && stopOrder == execution.Order) || (targetOrder != null && targetOrder == execution.Order))
            {
                if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
                {
                    stopOrder = null;
                    targetOrder = null;
                }
            }
        }
    }
}
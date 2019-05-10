using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AkunaTest
{
    public enum OrderValidity
    {
        GFD,
        IOC  //TODO : cater for this type of orders
    }

    public enum OrderType
    {
        BUY,
        SELL
    }

    public class Order
    {
        public OrderType Type { get; set; }
        public OrderValidity Validity { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public string ID { get; set; }
        public void Update(Order updatedOrder)
        {
            Type = updatedOrder.Type;
            Price = updatedOrder.Price;
            Quantity = updatedOrder.Quantity;
        }
        public override string ToString()
        {
            return $"{ID} {Type.ToString()} {Validity.ToString()} {Price} {Quantity}";
        }

        public string TradeInfo()
        {
            return $"{ID} {Price} {Quantity}";
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }

    public class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
    {
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }

    public class OrderCollection : OrderedDictionary
    {
        public void Add(Order order)
        {
            this.Add(order.ID, order);
        }

        public bool Remove(string id)
        {
            if (Contains(id))
            {
                base.Remove(id);
                return true;
            }
            else
                return false;
        }

        public Order Find(string id)
        {
            return this.Contains(id) ?  (Order)this[id] : null;
        }

        public int GetIndex(string id)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (((Order)this[i]).ID == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public IEnumerable<Order> AsEnumerable()
        {
            return this.Cast<DictionaryEntry>().Select(d=>(Order)d.Value);
        }

        public IEnumerable<Order> SortByBuyPrice()
        {
            return this.AsEnumerable().OrderByDescending(o => o.Type).ThenByDescending(o => o.Price);
        }

        public bool UpdateOrder(Order updatedOrder)
        {
            Order order = this.Find(updatedOrder.ID);

            if (order != null && order.Validity != OrderValidity.IOC)
            {
                order.Update(updatedOrder);

                //bring the order to the begining of the list
                this.Remove(order.ID);
                this.Add(order);

                return true;
            }

            return false;
        }

        public bool CancelOrder(string cancelledOrderId)
        {
            Order order = this.Find(cancelledOrderId);

            if (order != null)
            {
                this.Remove(cancelledOrderId);

                return true;
            }

            return false;
        }

        public void MergeOrders(Order firstOrder, Order secondOrder)
        {
            if (firstOrder.Quantity - secondOrder.Quantity == 0)
            {
                this.Remove(firstOrder.ID);
                this.Remove(secondOrder.ID);
            }
            else if (firstOrder.Quantity - secondOrder.Quantity > 0)
            {
                firstOrder.Quantity -= secondOrder.Quantity;
                this.Remove(secondOrder.ID);
            }
            else if (firstOrder.Quantity - secondOrder.Quantity < 0)
            {
                secondOrder.Quantity -= firstOrder.Quantity;
                this.Remove(firstOrder.ID);
            }
        }
    }

    public class OrderMatchingEngine
    {
        private const string KW_BUY = "BUY";
        private const string KW_SELL = "SELL";
        private const string KW_MODIFY = "MODIFY";
        private const string KW_PRINT = "PRINT";
        private const string KW_CANCEL = "CANCEL";
        private const string KW_TRADE = "TRADE";


        private OrderCollection orders = new OrderCollection(); //we store the orders ordered by updated time : newers first, olders last

        public delegate void OnOutputMethod(string text);
        public event OnOutputMethod OnOutput;

        public delegate void OnTradeHappenedMethod(string Id1, int price1, int quantity1, string order2, int price2, int quantity2);
        public event OnTradeHappenedMethod OnTradeHappened;

        public void Parse(string input)
        {
            var keyword = FindKeyword(input);
            switch (keyword)
            {
                case KW_BUY:
                case KW_SELL:
                    ParseOrderLine(input);
                    var newOrder = ParseOrderLine(input);
                    orders.Add(newOrder);
                    break;

                case KW_MODIFY:
                    Order updatedOrder = ParseModifyLine(input);
                    orders.UpdateOrder(updatedOrder);
                    break;

                case KW_CANCEL:
                    string cancelledOrderId = ParseCancelLine(input);
                    orders.CancelOrder(cancelledOrderId);
                    break;


                case KW_PRINT:
                    PrintPriceBook(orders);
                    break;

                default:
                    break;
            }

            ApplyTrades(orders);
        }

        private void ApplyTrades(OrderCollection orders)
        {
            Order firstOrder, secondOrder;

            while (FindNextTrade(orders, out firstOrder, out secondOrder))
            {
                orders.MergeOrders(firstOrder, secondOrder);

                OnTradeHappened?.Invoke(firstOrder.ID, firstOrder.Price, firstOrder.Quantity,
                                secondOrder.ID, secondOrder.Price, secondOrder.Quantity);

                OnOutput?.Invoke($"{KW_TRADE} {firstOrder.TradeInfo()} {secondOrder.TradeInfo()}");
            }
        }



        private bool FindNextTrade(OrderCollection orders, out Order firstOrder, out Order secondOrder)
        {
            var sortedOrders = orders.SortByBuyPrice().ToList();

            firstOrder = secondOrder = null;

            //var orderList = orders.AsEnumerable().ToList();

            for (int i = sortedOrders.Count()-1 ; i >= 0  ; i--)
            {
                if (sortedOrders[i].Type == OrderType.BUY)
                {
                    for (int j = sortedOrders.Count()-1 ; j>=0  && j!=i; j--)
                    {
                        if (sortedOrders[j].Type == OrderType.SELL &&
                            sortedOrders[j].Price <= sortedOrders[i].Price)
                        {
                            if (orders.GetIndex(sortedOrders[i].ID) < orders.GetIndex(sortedOrders[j].ID))
                            {
                                firstOrder = orders.Find(sortedOrders[i].ID);
                                secondOrder = orders.Find(sortedOrders[j].ID);
                            }
                            else
                            {
                                firstOrder = orders.Find(sortedOrders[j].ID);
                                secondOrder = orders.Find(sortedOrders[i].ID);
                            }

                            return true;
                        }
                    }
                }
                else //if (orderList[i].Type == OrderType.SELL)
                {
                    for (int j = sortedOrders.Count()-1; j>0  && j!=i; j++)
                    {
                        if (sortedOrders[j].Type == OrderType.BUY && sortedOrders[j].Price >= sortedOrders[i].Price)
                        {
                            if (orders.GetIndex(sortedOrders[i].ID) < orders.GetIndex(sortedOrders[j].ID))
                            {
                                firstOrder = orders.Find(sortedOrders[i].ID);
                                secondOrder = orders.Find(sortedOrders[j].ID);
                            }
                            else
                            {
                                firstOrder = orders.Find(sortedOrders[j].ID);
                                secondOrder = orders.Find(sortedOrders[i].ID);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string ParseCancelLine(string input)
        {
            try
            {
                string pattern = KW_CANCEL + @" (?<OrderName>\w+)";
                MatchCollection matches = Regex.Matches(input, pattern);

                if (matches.Count > 0)
                {
                    Match match = matches[0];
                    var orderName = match.Groups["OrderName"].Value;

                    return orderName;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private Order ParseModifyLine(string input)
        {
            try
            {
                string pattern = KW_MODIFY + @" (?<OrderName>\w+) (?<OrderType>\w+) (?<Price>\d+) (?<Quantity>\d+)";
                MatchCollection matches = Regex.Matches(input, pattern);

                if (matches.Count > 0)
                {
                    Match match = matches[0];
                    var orderName = match.Groups["OrderName"].Value;
                    var orderType = match.Groups["OrderType"].Value;
                    var price = match.Groups["Price"].Value;
                    var quantity = match.Groups["Quantity"].Value;

                    return new Order()
                    {
                        Type = (OrderType)Enum.Parse(typeof(OrderType), orderType),
                        Price = int.Parse(price),
                        Quantity = int.Parse(quantity),
                        ID = orderName,
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void AddToOrderSummary(Order order, SortedDictionary<int,int> summary)
        {
            if (summary.ContainsKey(order.Price))
                summary[order.Price] += order.Quantity;
            else
                summary.Add(order.Price, order.Quantity);
        }

        private void PrintPriceBook(OrderCollection orders)
        {
            var sellPrices = new SortedDictionary<int, int>(new DescendingComparer<int>());
            var buyPrices = new SortedDictionary<int, int>(new DescendingComparer<int>());

            foreach (var order in orders.AsEnumerable())
            {
                if (order.Type == OrderType.SELL)
                {
                    AddToOrderSummary(order, sellPrices);
                }
                else if (order.Type == OrderType.BUY)
                {
                    AddToOrderSummary(order, buyPrices);
                }
            }

            OnOutput?.Invoke("SELL:");
            foreach (var item in sellPrices)
            {
                OnOutput?.Invoke($"{item.Key} {item.Value}");
            }

            OnOutput?.Invoke("BUY:");
            foreach (var item in buyPrices)
            {
                OnOutput?.Invoke($"{item.Key} {item.Value}");
            }
        }

        private Order ParseOrderLine(string input)
        {
            try
            {
                string pattern = @"(?<OrderType>\w+) (?<Validity>\w+) (?<Price>\d+) (?<Quantity>\d+) (?<OrderName>\w+)";
                MatchCollection matches = Regex.Matches(input, pattern);

                if(matches.Count > 0 )
                {
                    Match match = matches[0];
                    var orderType = match.Groups["OrderType"].Value;
                    var orderValidity = match.Groups["Validity"].Value;
                    var price = match.Groups["Price"].Value;
                    var quantity = match.Groups["Quantity"].Value;
                    var orderName = match.Groups["OrderName"].Value;

                    return new Order() {
                        Type = (OrderType)Enum.Parse(typeof(OrderType), orderType),
                        Validity = (OrderValidity)Enum.Parse(typeof(OrderValidity), orderValidity),
                        Price = int.Parse(price),
                        Quantity = int.Parse(quantity),
                        ID = orderName,
                    };
                }

                return null;
            }
            catch 
            {
                return null;
            }
        }

        private string FindKeyword(string input)
        {
            var result = string.Empty;
            
            for(var i = 0; i <input.Length && input[i] != ' '; i++)
            {
                result += new string(new char[] { input[i] });
            }

            return result;
        }
    }


    class Program
    {

        static void Main(string[] args)
        {
            string line;

            var engine = new OrderMatchingEngine();

            engine.OnOutput += Engine_OnOutput;

            while ((line = Console.ReadLine()) != null)
            {
                engine.Parse(line);
            }
        }

        private static void Engine_OnOutput(string text)
        {
            Console.WriteLine(text);
        }
    }
}

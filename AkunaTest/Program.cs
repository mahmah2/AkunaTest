using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AkunaTest
{
    public enum OrderValidity
    {
        GFD,
        IOC
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
            return $"{Type.ToString()} {Validity.ToString()} {Price} {Quantity} {ID}";
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

    public static class OrderMatchingExtensionMethods
    {
        public static bool UpdateOrder(this HashSet<Order> orders, Order updatedOrder)
        {
            Order order;

            if (orders.TryGetValue(updatedOrder, out order))
            {
                    order.Update(updatedOrder);

                    //bring the order to the begining of the list
                    orders.Remove(order);
                    orders.Add(order);

                    return true;
            }

            return false;
        }

        public static bool CancelOrder(this HashSet<Order> orders, string cancelledOrderId)
        {
            Order order;

            if (orders.TryGetValue(new Order() { ID = cancelledOrderId } , out order))
            {
                orders.Remove(order);
                return true;
            }

            return false;
        }
    }


    public class OrderMatchingEngine
    {
        private const string KW_BUY = "BUY";
        private const string KW_SELL = "SELL";
        private const string KW_MODIFY = "MODIFY";
        private const string KW_PRINT = "PRINT";
        private const string KW_CANCEL = "CANCEL";


        private HashSet<Order> orders = new HashSet<Order>(); //we store the orders ordered by updated time

        private Action<string> DoOutput;

        public OrderMatchingEngine(Action<string> doOutput)
        {
            DoOutput = doOutput;
        }

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

        private void PrintPriceBook(IEnumerable<Order> orders)
        {
            var sellPrices = new SortedDictionary<int, int>(new DescendingComparer<int>());
            var buyPrices = new SortedDictionary<int, int>(new DescendingComparer<int>());

            foreach (var order in orders)
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

            DoOutput("SELL:");
            foreach (var item in sellPrices)
            {
                DoOutput($"{item.Key} {item.Value}");
            }

            DoOutput("BUY:");
            foreach (var item in buyPrices)
            {
                DoOutput($"{item.Key} {item.Value}");
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

            var engine = new OrderMatchingEngine(s => Console.WriteLine(s));

            while ((line = Console.ReadLine()) != null)
            {
                engine.Parse(line);
            }
        }
    }
}

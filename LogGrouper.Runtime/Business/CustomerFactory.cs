using LogGrouper.Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Runtime.Business
{
    public class CustomerFactory : IDisposable
    {
        Filter _filter;

        public CustomerFactory(Filter filter)
        {
            _filter = filter;
        }

        public void Dispose()
        {
            _filter = null;
        }

        public ICustomer GetCustomer()
        {
            switch (_filter.Storerkey.ToUpper())
            {
                case "FARMAECOM":
                    return new Farmaecom(_filter);
                case "FARMACITY":
                    return new Farmacity(_filter);
                default:
                    return null;
            }
        }
    }
}

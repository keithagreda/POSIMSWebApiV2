using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.Customer
{
    public class CustomerDropDownDto
    {
        public Guid Id { get; set; }
        public string CustomerFullName { get; set; }
    }
}
